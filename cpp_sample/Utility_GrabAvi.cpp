// Utility_GrabAvi.cpp
/*
    Note: Before getting started, Basler recommends reading the "Programmer's Guide" topic
    in the pylon C++ API documentation delivered with pylon.
    If you are upgrading to a higher major version of pylon, Basler also
    strongly recommends reading the "Migrating from Previous Versions" topic in the pylon C++ API documentation.

    This sample illustrates how to create a video file in Audio Video Interleave (AVI) format.
*/

// Include files to use the pylon API.
#include <pylon/PylonIncludes.h>
#include <pylon/AviCompressionOptions.h>
#include <pylon/PylonGUI.h>

// Namespace for using pylon objects.
using namespace Pylon;

// Namespace for using GenApi objects.
using namespace GenApi;

// Namespace for using cout.
using namespace std;

// The maximum number of images to be grabbed.
static const uint32_t c_countOfImagesToGrab = 500;

// When this amount of image data has been written the grabbing is stopped.
static const size_t c_maxImageDataBytesThreshold = 50 * 1024 * 1024;


int main(int argc, char* argv[])
{
    // The exit code of the sample application.
    int exitCode = 0;

    // Before using any pylon methods, the pylon runtime must be initialized. 
    PylonInitialize();

    try
    {
        // Create an AVI writer object.
        CAviWriter aviWriter;

        // The AVI writer supports the output formats PixelType_Mono8,
        // PixelType_BGR8packed, and PixelType_BGRA8packed.
        EPixelType aviPixelType = PixelType_BGR8packed;
        // The frame rate used for playing the video (play back frame rate).
        const int cFramesPerSecond = 20;

        // Create an instant camera object with the camera device found first.
        CInstantCamera camera( CTlFactory::GetInstance().CreateFirstDevice());

        // Print the model name of the camera.
        cout << "Using device " << camera.GetDeviceInfo().GetModelName() << endl;

        // Open the camera.
        camera.Open();

        // Get the required camera settings.
        CIntegerParameter width( camera.GetNodeMap(), "Width");
        CIntegerParameter height( camera.GetNodeMap(), "Height");
        CEnumParameter pixelFormat( camera.GetNodeMap(), "PixelFormat");

        // Optional: Depending on your camera or computer, you may not be able to save   
        // a video without losing frames. Therefore, we limit the resolution:
        width.SetValue( 640, IntegerValueCorrection_Nearest );
        height.SetValue( 480, IntegerValueCorrection_Nearest );

        if ( pixelFormat.IsReadable())
        {
            // If the camera produces Mono8 images use Mono8 for the AVI file.
            if ( pixelFormat.GetValue() == "Mono8")
            {
                aviPixelType = PixelType_Mono8;
            }
        }

        // Optionally set up compression options.
        SAviCompressionOptions* pCompressionOptions = NULL;
        // Uncomment the two code lines below to enable AVI compression.
        // A dialog will be shown for selecting the codec.
        //SAviCompressionOptions compressionOptions( "MSVC", true);
        //pCompressionOptions = &compressionOptions;

        // Open the AVI writer.
        aviWriter.Open(
            "_TestAvi.avi",
            cFramesPerSecond,
            aviPixelType,
            (uint32_t)width.GetValue(),
            (uint32_t)height.GetValue(),
            ImageOrientation_BottomUp, // Some compression codecs will not work with top down oriented images.
            pCompressionOptions);

        // Start the grabbing of c_countOfImagesToGrab images.
        // The camera device is parameterized with a default configuration which
        // sets up free running continuous acquisition.
        camera.StartGrabbing( c_countOfImagesToGrab, GrabStrategy_LatestImages);


        cout << "Please wait. Images are grabbed." << endl;

        // This smart pointer will receive the grab result data.
        CGrabResultPtr ptrGrabResult;

        // Camera.StopGrabbing() is called automatically by the RetrieveResult() method
        // when c_countOfImagesToGrab images have been retrieved.
        while ( camera.IsGrabbing())
        {
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            camera.RetrieveResult( 5000, ptrGrabResult, TimeoutHandling_ThrowException);

            // Display the image. Remove the following line of code to maximize frame rate.
            Pylon::DisplayImage(1, ptrGrabResult);

            // If required, the grabbed image is converted to the correct format and is then added to the AVI file.
            // The orientation of the image taken by the camera is top down.
            // The bottom up orientation is specified to apply when opening the Avi Writer. That is why the image is
            // always converted before it is added to the AVI file.
            // To maximize frame rate try to avoid image conversion (see the CanAddWithoutConversion() method).
            aviWriter.Add( ptrGrabResult);

            // If images are skipped, writing AVI frames takes too much processing time.
            std::cout << "Images Skipped = " << ptrGrabResult->GetNumberOfSkippedImages() << boolalpha
                << "; Image has been converted = " << !aviWriter.CanAddWithoutConversion( ptrGrabResult)
                << std::endl;

            // Check whether the image data size limit has been reached to avoid the AVI File to get too large.
            // The size returned by GetImageDataBytesWritten() does not include the sizes of the AVI file header and AVI file index.
            // See the documentation for GetImageDataBytesWritten() for more information.
            if ( c_maxImageDataBytesThreshold < aviWriter.GetImageDataBytesWritten())
            {
                std::cout << "The image data size limit has been reached." << endl;
                break;
            }
        }
    }
    catch (const GenericException &e)
    {
        // Error handling.
        cerr << "An exception occurred." << endl
        << e.GetDescription() << endl;
        exitCode = 1;
    }

    // Comment the following two lines to disable waiting on exit.
    cerr << endl << "Press enter to exit." << endl;
    while( cin.get() != '\n');

    // Releases all pylon resources. 
    PylonTerminate(); 

    return exitCode;
}
