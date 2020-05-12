/*
   This sample illustrates how to create a video file in Audio Video Interleave (AVI) format.
*/

using System;
using Basler.Pylon;

namespace Grab
{
    class Grab
    {
        const int countOfImagesToGrab = 100;
        const string videoFilename = "Utility_GrabAvi.avi";

        internal static void Main()
        {
            // The exit code of the sample application.
            int exitCode = 0;

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

                    // Optional: Depending on your camera, computer, and codec choice, you may not be able
                    // to save a video without losing frames. Therefore, we limit the resolution:
                    camera.Parameters[PLCamera.Width].SetValue(640, IntegerValueCorrection.Nearest);
                    camera.Parameters[PLCamera.Height].SetValue(480, IntegerValueCorrection.Nearest);
                    camera.Parameters[PLCamera.PixelFormat].TrySetValue(PLCamera.PixelFormat.Mono8);

                    // We also increase the number of memory buffers to be used while grabbing.
                    camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(20);

                    // Create and open the AviVideoWriter.
                    using (AviVideoWriter writer = new AviVideoWriter())
                    {
                        // This will create an uncompressed file.
                        // If you want to use a specific codec, you should call an overload where you can
                        // pass the four-character code of the codec you want to use or pass preset compression options
                        // using the <c>compressionOptions</c> parameter.
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
                                    // Write the image to the .avi file.
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

                        // Close the .avi file.
                        writer.Close();
                    }

                    // Close the connection to the camera device.
                    camera.Close();
                }
            }
            catch (Exception e)
            {
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
