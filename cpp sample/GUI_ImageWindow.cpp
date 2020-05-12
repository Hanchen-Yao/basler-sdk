// GUI_ImageWindow.cpp
/*
    Note: Before getting started, Basler recommends reading the "Programmer's Guide" topic
    in the pylon C++ API documentation delivered with pylon.
    If you are upgrading to a higher major version of pylon, Basler also
    strongly recommends reading the "Migrating from Previous Versions" topic in the pylon C++ API documentation.

    This sample illustrates how to show images using the
    CPylonImageWindow class. Here, images are grabbed, split into
    multiple tiles and and each tile is shown in a separate image windows.
*/

// Include files to use the pylon API.
#include <pylon/PylonIncludes.h>
#include <pylon/PylonGUI.h>
#include "../include/SampleImageCreator.h"
#include <conio.h>

// Namespace for using pylon objects.
using namespace Pylon;

// Namespace for using cout.
using namespace std;

// Number of images to be grabbed.
static const uint32_t c_countOfImagesToGrab = 1000;

int main(int argc, char* argv[])
{
    // The exit code of the sample application.
    int exitCode = 0;

    // Before using any pylon methods, the pylon runtime must be initialized. 
    PylonInitialize();

    try
    {
        // Define constants.
        static const uint32_t cNumTilesX = 3;
        static const uint32_t cNumTilesY = 2;
        static const uint32_t cWindowBorderSizeX = 25;
        static const uint32_t cWindowBorderSizeY = 125;
        static const uint32_t cScreenStartX = 40;
        static const uint32_t cScreenStartY = 40;
        static const uint32_t cMaxIndex = 31;
        static const size_t   cNumWindows = cNumTilesY * cNumTilesX ;
        static const uint32_t cMaxWidth = 640;
        static const uint32_t cMaxHeight = 480;

        // Create an array of image windows.
        CPylonImageWindow imageWindows[ cNumWindows ];

        // Create an Instant Camera object.
        CInstantCamera camera( CTlFactory::GetInstance().CreateFirstDevice());

        // Print the model name of the camera.
        cout << "Using device " << camera.GetDeviceInfo().GetModelName() << endl;

        // Start the grab. Only display the latest image.
        camera.StartGrabbing( c_countOfImagesToGrab, GrabStrategy_LatestImageOnly);

        // This smart pointer will receive the grab result data.
        CGrabResultPtr ptrGrabResult;

        // Grab images and show the tiles of each image in separate image windows.
        while ( camera.IsGrabbing())
        {
            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
            camera.RetrieveResult( 5000, ptrGrabResult, TimeoutHandling_ThrowException);

            // If the image was grabbed successfully.
            if ( ptrGrabResult->GrabSucceeded())
            {
                // This image object is used for splitting the grabbed image into tiles.
                CPylonImage image;

                // Attach the grab result to a pylon image.
                image.AttachGrabResultBuffer( ptrGrabResult);

                // Compute tile sizes.
                uint32_t imageTileWidth = min( image.GetWidth(), cMaxWidth) / cNumTilesX;
                uint32_t imageTileHeight = min( image.GetHeight(), cMaxHeight) / cNumTilesY;
                imageTileWidth -= imageTileWidth % GetPixelIncrementX( image.GetPixelType());
                imageTileHeight -= imageTileWidth % GetPixelIncrementY( image.GetPixelType());

                uint32_t windowTileWidth = imageTileWidth + cWindowBorderSizeX;
                uint32_t windowTileHeight = imageTileHeight + cWindowBorderSizeY;

                // Create and display the tiles of the grabbed image.
                for ( uint32_t indexTileX = 0; indexTileX < cNumTilesX; ++indexTileX)
                {
                    for ( uint32_t indexTileY = 0; indexTileY < cNumTilesY; ++indexTileY)
                    {
                        size_t arrayIndex = indexTileY * cNumTilesX + indexTileX;
                        bool windowCreated = false;

                        if ( !imageWindows[ arrayIndex ].IsValid())
                        {
                            // Create the image window and position the image window as a tile on the screen.
                            // The Image Window stores the last size and position.
                            // The last Image Window indices are used here to avoid changing
                            // the settings of the windows used for other samples.
                            size_t windowIndex = cMaxIndex - arrayIndex;
                            imageWindows[ arrayIndex ].Create( windowIndex,
                                cScreenStartX + indexTileX * windowTileWidth,
                                cScreenStartY + indexTileY * windowTileHeight,
                                windowTileWidth,
                                windowTileHeight
                                );

                            windowCreated = true;
                        }

                        // Get the image area of interest (Image AOI) that includes the tile. This is a zero copy operation.
                        CPylonImage tile = image.GetAoi( indexTileX * imageTileWidth, indexTileY * imageTileHeight, imageTileWidth, imageTileHeight);

                        // Set the tile image.
                        imageWindows[ arrayIndex ].SetImage( tile);

                        // Show the image.
                        imageWindows[ arrayIndex ].Show();

                        if ( windowCreated)
                        {
                            // Wait a little to show how the windows appear on the screen.
                            ::Sleep( 200);
                        }
                    }
                }
            }
            else
            {
                throw RUNTIME_EXCEPTION( "Error image grab failed: %hs", ptrGrabResult->GetErrorDescription().c_str());
            }
        }

        // Destroy the windows.
        for ( size_t arrayIndex = 0; arrayIndex < cNumWindows; ++arrayIndex)
        {
            // Close() closes and destroys the window.
            imageWindows[ arrayIndex ].Close();

            // Wait a little to show how the windows are removed from the screen.
            ::Sleep( 200);
        }
    }
    catch (const GenericException &e)
    {
        // Error handling.
        cerr << "An exception occurred." << endl
        << e.GetDescription() << endl;
        exitCode = 1;

        cerr << endl << "Press enter to exit." << endl;
        while( cin.get() != '\n');
    }

    // Releases all pylon resources. 
    PylonTerminate(); 

    return exitCode;
}
