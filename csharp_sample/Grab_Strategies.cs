/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.
    
    This sample shows the use of the Instant Camera grab strategies.
*/

using System;
using Basler.Pylon;

namespace Grab_Strategies
{
    class Grab_Strategies
    {
        // OnImageGrabbed is used to print the image information like Width, Height etc..
        // Can be used to implement other functionality for image grab event.
        static void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            if (e.GrabResult.GrabSucceeded)
            {
                Console.WriteLine("SizeX:{0}", e.GrabResult.Width);
                Console.WriteLine("SizeY:{0}", e.GrabResult.Height);
                byte[] pixelData = (byte[])e.GrabResult.PixelData;
                Console.WriteLine("Gray value of first pixel:{0}", pixelData[0]);
            }
            else
            {
                Console.WriteLine("Error Code: {0} Error Description: {1}", e.GrabResult.ErrorCode, e.GrabResult.ErrorDescription);
            }
        }


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
                    // Camera event processing must be activated first, the default is off.
                    camera.Parameters[PLCameraInstance.GrabCameraEvents].SetValue(true);

                    // Change default configuration to enable software triggering.
                    camera.CameraOpened += Configuration.SoftwareTrigger;
                    // Register image grabbed event to print frame info
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    IGrabResult result;
                    int nBuffersInQueue = 0;

                    // Open the connection to the camera device.
                    camera.Open();

                    // The MaxNumBuffer parameter can be used to control the count of buffers
                    // allocated for grabbing. The default value of this parameter is 10.
                    camera.Parameters[PLStream.MaxNumBuffer].SetValue(15);

                    // Can the camera device be queried whether it is ready to accept the next frame trigger?
                    if (camera.CanWaitForFrameTriggerReady)
                    {
                        Console.WriteLine("Grab using the GrabStrategy.OneByOne default strategy:");

                        // The GrabStrategy.OneByOne strategy is used. The images are processed
                        // in the order of their arrival.
                        camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByUser);

                        // In the background, the grab engine thread retrieves the
                        // image data and queues the buffers into the internal output queue.

                        // Issue software triggers. For each call, wait up to 1000 ms until the camera is ready for triggering the next image.
                        for (int i = 0; i < 3; i++)
                        {
                            if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                            {
                                camera.ExecuteSoftwareTrigger();
                            }
                        }

                        // For demonstration purposes, wait for the last image to appear in the output queue.
                        System.Threading.Thread.Sleep(3 * 1000);

                        // Check that grab results are waiting.
                        if (camera.StreamGrabber.GrabResultWaitHandle.WaitOne(0))
                        {
                            Console.WriteLine("Grab results wait in the output queue.");
                        }

                        // All triggered images are still waiting in the output queue
                        // and are now retrieved.
                        // The grabbing continues in the background, e.g. when using hardware trigger mode,
                        // as long as the grab engine does not run out of buffers.
                        for (; ; )
                        {
                            result = camera.StreamGrabber.RetrieveResult(0, TimeoutHandling.Return);
                            if (result != null)
                            {
                                using (result)
                                {
                                    nBuffersInQueue++;
                                }
                            }
                            else
                                break;
                        }

                        Console.WriteLine("Retrieved {0} grab results from output queue.", nBuffersInQueue);

                        //Stop the grabbing.
                        camera.StreamGrabber.Stop();

                        Console.WriteLine("Grab using strategy GrabStrategy.LatestImages");

                        // The GrabStrategy_LatestImages strategy is used. The images are processed
                        // in the order of their arrival, but only a number of the images received last
                        // are kept in the output queue.

                        // The size of the output queue can be adjusted.
                        // When using this strategy the OutputQueueSize parameter can be changed during grabbing.
                        camera.Parameters[PLCameraInstance.OutputQueueSize].SetValue(2);

                        camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByUser);

                        // Execute the software trigger, wait actively until the camera accepts the next frame trigger or until the timeout occurs.
                        for (int i = 0; i < 3; ++i)
                        {
                            if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                            {
                                camera.ExecuteSoftwareTrigger();
                            }
                        }

                        // Wait for all images.
                        System.Threading.Thread.Sleep(3 * 1000);

                        // Check whether the grab results are waiting.
                        if (camera.StreamGrabber.GrabResultWaitHandle.WaitOne(0))
                        {
                            Console.WriteLine( "Grab results wait in the output queue." );
                        }

                        // Only the images received last are waiting in the internal output queue
                        // and are now retrieved.
                        // The grabbing continues in the background, e.g. when using the hardware trigger mode.
                        nBuffersInQueue = 0;
                        for (; ; )
                        {
                            result = camera.StreamGrabber.RetrieveResult(0, TimeoutHandling.Return);
                            if (result != null)
                            {
                                using (result)
                                {
                                    if (result.SkippedImageCount > 0)
                                    {
                                        Console.WriteLine( "Skipped {0} images.", result.SkippedImageCount );
                                    }
                                    nBuffersInQueue++;
                                }
                            }
                            else
                                break;
                        }

                        Console.WriteLine("Retrieved {0} grab result from output queue.", nBuffersInQueue);

                        // When setting the output queue size to 1 this strategy is equivalent to the GrabStrategy_LatestImageOnly grab strategy from C++.
                        camera.Parameters [PLCameraInstance.OutputQueueSize].SetValue( 1 );

                        // When setting the output queue size to CInstantCamera::MaxNumBuffer this strategy is equivalent to GrabStrategy.OneByOne.
                        camera.Parameters[PLCameraInstance.OutputQueueSize].SetValue(camera.Parameters[PLStream.MaxNumBuffer].GetValue());

                        //Stop the grabbing.
                        camera.StreamGrabber.Stop();
                    }
                    else
                    {
                        // See the documentation of Camera.CanWaitForFrameTriggerReady for more information.
                        Console.WriteLine("This sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
                    }
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
