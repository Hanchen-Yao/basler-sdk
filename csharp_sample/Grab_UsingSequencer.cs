/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample shows how to grab images using the sequencer feature of a camera.
    Three sequence sets are used for image acquisition. Each sequence set
    uses a different image height.
*/

using System;
using Basler.Pylon;

namespace Grab_UsingSequencer
{
    class Grab_UsingSequencer
    {
        private static Version sfnc2_0_0 = new Version(2, 0, 0);
        // Number of images to be grabbed.
        private static UInt32 countOfImagesToGrab = 10;

        // OnImageGrabbed is used to print the image information like Width, Height etc..
        // Can be used to implement other functionality for image grab event.
        private static void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
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
                // Create a camera object selecting the first camera device found.
                // More constructors are available for selecting a specific camera device.
                using (Camera camera = new Camera())
                {
                    // Change default configuration to enable software triggering.
                    camera.CameraOpened += Configuration.SoftwareTrigger;

                    // Open the camera.
                    camera.Open();

                    // Register image grabbed event to print frame info
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                    // DeviceVendorName, DeviceModelName, and DeviceFirmwareVersion are string parameters.
                    Console.WriteLine("Camera Device Information");
                    Console.WriteLine("=========================");
                    Console.WriteLine("Vendor           : {0}", camera.Parameters[PLCamera.DeviceVendorName].GetValue());
                    Console.WriteLine("Model            : {0}", camera.Parameters[PLCamera.DeviceModelName].GetValue());
                    Console.WriteLine("Firmware version : {0}", camera.Parameters[PLCamera.DeviceFirmwareVersion].GetValue());
                    Console.WriteLine("");
                    Console.WriteLine("Camera Device Settings");
                    Console.WriteLine("======================");

                    // Can the camera device be queried whether it is ready to accept the next frame trigger?
                    if (camera.CanWaitForFrameTriggerReady)
                    {
                        // bool for testing if sequencer is available or not
                        bool sequencerAvailable = false;

                        if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                        {
                            if (camera.Parameters[PLCamera.SequenceEnable].IsWritable)
                            {
                                sequencerAvailable = true; //Sequencer is available that is why it is true.

                                // Disable the sequencer before changing parameters.
                                // The parameters under control of the sequencer are locked
                                // when the sequencer is enabled. For a list of parameters
                                // controlled by the sequencer, see the camera User's Manual.
                                camera.Parameters[PLCamera.SequenceEnable].SetValue(false);

                                // Turn configuration mode on
                                if (camera.Parameters[PLCamera.SequenceConfigurationMode].IsWritable)
                                {
                                    camera.Parameters[PLCamera.SequenceConfigurationMode].SetValue(PLCamera.SequenceConfigurationMode.On);
                                }

                                // Maximize the image area of interest (Image AOI).
                                camera.Parameters[PLCamera.OffsetX].TrySetValue(camera.Parameters[PLCamera.OffsetX].GetMinimum());
                                camera.Parameters[PLCamera.OffsetY].TrySetValue(camera.Parameters[PLCamera.OffsetY].GetMinimum());
                                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

                                // Set the pixel data format.
                                camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);

                                // Set up sequence sets.

                                // Configure how the sequence will advance.
                                // 'Auto' refers to the auto sequence advance mode.
                                // The advance from one sequence set to the next will occur automatically with each image acquired.
                                // After the end of the sequence set cycle was reached a new sequence set cycle will start.
                                camera.Parameters[PLCamera.SequenceAdvanceMode].SetValue(PLCamera.SequenceAdvanceMode.Auto);

                                // Our sequence sets relate to three steps (0..2).
                                // In each step we will increase the height of the Image AOI by one increment.
                                camera.Parameters[PLCamera.SequenceSetTotalNumber].SetValue(3);

                                long increments = (camera.Parameters[PLCamera.Height].GetMaximum() - camera.Parameters[PLCamera.Height].GetMinimum()) / camera.Parameters[PLCamera.Height].GetIncrement();

                                // Set the parameters for step 0; quarter height image.
                                camera.Parameters[PLCamera.SequenceSetIndex].SetValue(0);
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * (increments / 4) + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequenceSetStore].Execute();

                                // Set the parameters for step 1; half height image.
                                camera.Parameters[PLCamera.SequenceSetIndex].SetValue(1);
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * (increments / 2) + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequenceSetStore].Execute();

                                // Set the parameters for step 2; full height image.
                                camera.Parameters[PLCamera.SequenceSetIndex].SetValue(2);
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * (increments) + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequenceSetStore].Execute();

                                // Finish configuration
                                if (camera.Parameters[PLCamera.SequenceConfigurationMode].IsWritable)
                                {
                                    camera.Parameters[PLCamera.SequenceConfigurationMode].SetValue(PLCamera.SequenceConfigurationMode.Off);
                                }

                                // Enable the sequencer feature.
                                // From here on you cannot change the sequencer settings anymore.
                                camera.Parameters[PLCamera.SequenceEnable].SetValue(true);

                                // Start the grabbing of countOfImagesToGrab images.
                                camera.StreamGrabber.Start(countOfImagesToGrab);
                            }
                            else
                            {
                                sequencerAvailable = false; // Sequencer not available
                            }
                        }
                        else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                        {
                            if (camera.Parameters[PLCamera.SequencerMode].IsWritable)
                            {
                                sequencerAvailable = true;

                                // Disable the sequencer before changing parameters.
                                // The parameters under control of the sequencer are locked
                                // when the sequencer is enabled. For a list of parameters
                                // controlled by the sequencer, see the camera User's Manual.
                                camera.Parameters[PLCamera.SequencerMode].SetValue(PLCamera.SequencerMode.Off);

                                // Maximize the image area of interest (Image AOI).
                                camera.Parameters[PLCamera.OffsetX].TrySetValue(camera.Parameters[PLCamera.OffsetX].GetMinimum());
                                camera.Parameters[PLCamera.OffsetY].TrySetValue(camera.Parameters[PLCamera.OffsetY].GetMinimum());
                                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

                                // Set the pixel data format.
                                // This parameter may be locked when the sequencer is enabled.
                                camera.Parameters[PLCamera.PixelFormat].SetValue(PLCamera.PixelFormat.Mono8);

                                // Set up sequence sets and turn sequencer configuration mode on.
                                camera.Parameters[PLCamera.SequencerConfigurationMode].SetValue(PLCamera.SequencerConfigurationMode.On);

                                // Configure how the sequence will advance.

                                // The sequence sets relate to three steps (0..2).
                                // In each step, the height of the Image AOI is doubled.

                                long increments = (camera.Parameters[PLCamera.Height].GetMaximum() - camera.Parameters[PLCamera.Height].GetMinimum()) / camera.Parameters[PLCamera.Height].GetIncrement();

                                long initialSet = camera.Parameters[PLCamera.SequencerSetSelector].GetMinimum();
                                long incSet = camera.Parameters[PLCamera.SequencerSetSelector].GetIncrement();
                                long curSet = initialSet;

                                // Set the parameters for step 0; quarter height image.
                                camera.Parameters[PLCamera.SequencerSetSelector].SetValue(initialSet);
                                {
                                    // valid for all sets
                                    // reset on software signal 1;
                                    camera.Parameters[PLCamera.SequencerPathSelector].SetValue(0);
                                    camera.Parameters[PLCamera.SequencerSetNext].SetValue(initialSet);
                                    camera.Parameters[PLCamera.SequencerTriggerSource].SetValue(PLCamera.SequencerTriggerSource.SoftwareSignal1);
                                    // advance on Frame Start
                                    camera.Parameters[PLCamera.SequencerPathSelector].SetValue(1);
                                    camera.Parameters[PLCamera.SequencerTriggerSource].SetValue(PLCamera.SequencerTriggerSource.FrameStart);
                                }
                                camera.Parameters[PLCamera.SequencerSetNext].SetValue(curSet + incSet);

                                // Set the parameters for step 0; quarter height image.
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * (increments / 4) + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequencerSetSave].Execute();

                                // Set the parameters for step 1; half height image.
                                curSet += incSet;
                                camera.Parameters[PLCamera.SequencerSetSelector].SetValue(curSet);
                                // advance on Frame Start to next set
                                camera.Parameters[PLCamera.SequencerSetNext].SetValue(curSet + incSet);
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * (increments / 2) + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequencerSetSave].Execute();

                                // Set the parameters for step 2; full height image.
                                curSet += incSet;
                                camera.Parameters[PLCamera.SequencerSetSelector].SetValue(curSet);
                                // advance on Frame End to initial set,
                                camera.Parameters[PLCamera.SequencerSetNext].SetValue(initialSet); // terminates sequence definition
                                                                                                   // full height
                                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetIncrement() * increments + camera.Parameters[PLCamera.Height].GetMinimum());
                                camera.Parameters[PLCamera.SequencerSetSave].Execute();

                                // Enable the sequencer feature.
                                // From here on you cannot change the sequencer settings anymore.
                                camera.Parameters[PLCamera.SequencerConfigurationMode].SetValue(PLCamera.SequencerConfigurationMode.Off);
                                camera.Parameters[PLCamera.SequencerMode].SetValue(PLCamera.SequencerMode.On);

                                // Start the grabbing of countOfImagesToGrab images.
                                camera.StreamGrabber.Start(countOfImagesToGrab);
                            }
                            else
                            {
                                sequencerAvailable = false; // Sequencer not available
                            }
                        }

                        if (sequencerAvailable)
                        {
                            IGrabResult result;
                            // Camera.StopGrabbing() is called automatically by the RetrieveResult() method
                            // when countOfImagesToGrab images have been retrieved.
                            while (camera.StreamGrabber.IsGrabbing)
                            {
                                // Execute the software trigger. Wait up to 1000 ms for the camera to be ready for trigger.
                                if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                                {
                                    camera.ExecuteSoftwareTrigger();

                                    // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                                    result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                                    using (result)
                                    {
                                        // Image grabbed successfully?
                                        if (result.GrabSucceeded)
                                        {
                                            // Display the grabbed image.
                                            ImageWindow.DisplayImage(1, result);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Error code:{0} Error description:{1}", result.ErrorCode, result.ErrorDescription);
                                        }
                                    }
                                }

                                // Wait for user input.
                                Console.WriteLine("Press Enter to continue.");
                                while (camera.StreamGrabber.IsGrabbing && Console.ReadKey().Key != ConsoleKey.Enter)
                                    ;
                            }

                            // Disable the sequencer.
                            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                            {
                                camera.Parameters[PLCamera.SequenceEnable].SetValue(false);
                            }
                            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                            {
                                camera.Parameters[PLCamera.SequencerMode].SetValue(PLCamera.SequencerMode.Off);
                            }
                        }
                        else
                        {
                            Console.WriteLine("The sequencer feature is not available for this camera.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("This sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
                    }

                    // Close the camera.
                    camera.Close();
                }
            }
            catch (Exception e)
            {
                // Error handling.
                Console.Error.WriteLine("Exception: {0}", e.Message);
                exitCode = 1;
            }

            // Comment the following two lines to disable waiting on exit.
            Console.Error.WriteLine("\nPress enter to exit.");
            Console.ReadLine();

            Environment.Exit(exitCode);
        }
    }
}
