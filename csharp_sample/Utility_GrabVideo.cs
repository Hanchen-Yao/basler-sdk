/*
   This sample illustrates how to create a video file in MP4 format.
*/

using System;
using Basler.Pylon;

namespace Grab
{
    class Grab
    {
        const int countOfImagesToGrab = 100;
        const string videoFilename = "Utility_GrabVideo.mp4";

        internal static void Main()
        {
            // The exit code of the sample application.
            int exitCode = 0;

            // Check if VideoWriter is supported and all required DLLs are available.
            if (!VideoWriter.IsSupported)
            {
                Console.WriteLine("VideoWriter is not supported at the moment. Please install the pylon Supplementary Package for MPEG-4 which is available on the Basler website.");
                // Return with error code 1.
                Environment.Exit(1);
            }

            try
            {
                // Create a camera object that selects the first camera device found.
                // More constructors are available for selecting a specific camera device.
                using (Camera camera = new Camera())
                {
                    // Open the connection to the camera device.
                    camera.Open();

                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    // Optional: Depending on your camera or computer, you may not be able to save
                    // a video without losing frames. Therefore, we limit the resolution:
                    camera.Parameters[PLCamera.Width].SetValue(640, IntegerValueCorrection.Nearest);
                    camera.Parameters[PLCamera.Height].SetValue(480, IntegerValueCorrection.Nearest);
                    camera.Parameters[PLCamera.PixelFormat].TrySetValue(PLCamera.PixelFormat.Mono8);

                    // We also increase the number of memory buffers to be used while grabbing.
                    camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(20);

                    // Create and open the VideoWriter.
                    using (VideoWriter writer = new VideoWriter())
                    {
                        // Set a quality of 90 for the video (value range is 1 to 100).
                        writer.Parameters[PLVideoWriter.Quality].SetValue(90);

                        // This will create a compressed video file.
                        writer.Create(videoFilename, 25, camera);

                        // Start grabbing.
                        camera.StreamGrabber.Start(countOfImagesToGrab);

                        Console.WriteLine("Please wait. Images are being grabbed.");

                        while (camera.StreamGrabber.IsGrabbing)
                        {
                            // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                            IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                            using (grabResult)
                            {
                                // Image grabbed successfully?
                                if (grabResult.GrabSucceeded)
                                {
                                    // Write the image to the video file.
                                    writer.Write(grabResult);
                                }
                                else
                                {
                                    Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                                }
                            }
                        }

                        // Stop grabbing.
                        camera.StreamGrabber.Stop();

                        // Close the video file.
                        writer.Close();
                    }

                    // Close the connection to the camera device.
                    camera.Close();
                }
            }
            catch (Exception e)
            {
                // Error handling.
                Console.Error.WriteLine("Exception: {0}", e.Message);
                exitCode = 1;
            }
            finally
            {
                // Comment the following two lines to disable waiting on exit.
                Console.Error.WriteLine("\nPress enter to exit.");
                Console.ReadLine();
            }

            Environment.Exit(exitCode);
        }
    }
}
