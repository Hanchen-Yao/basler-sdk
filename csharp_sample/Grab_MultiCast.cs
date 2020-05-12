/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample demonstrates how to open a camera in multicast mode
    and how to receive a multicast stream.

    Two instances of this application must be started simultaneously on different computers.
    The first application started on PC A acts as the controlling application and has full access to the GigE camera.
    The second instance started on PC B opens the camera in monitor mode.
    This instance is not able to control the camera but can receive multicast streams.

    To get the sample running, start this application first on PC A in control mode.
    After PC A has begun to receive frames, start the second instance of this
    application on PC B in monitor mode.
*/

using System;
using Basler.Pylon;

namespace Grab_MultiCast
{
    class GrabMultiCast
    {
        const UInt32 countOfImagesToGrab = 100;
        
        // OnImageGrabbed is used to print the image information like Width, Height etc.
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
        
      
        // This method is called if one or more images have been skipped during
        // image acquisition.
        static void OnImageSkipped(Object sender, ImageGrabbedEventArgs e)
        {
            Console.WriteLine("OnImageSkipped Event");
            Console.WriteLine("Number Of skipped images {0}", e.GrabResult.SkippedImageCount);
        }
        
        internal static void Main()
        {
            // The exit code of the sample application.
            int exitCode = 0;

            try
            {
                // Create a camera object that selects the first camera device found.
                // More constructors are available for selecting a specific camera device.
                // For multicast only look for GigE cameras here.
                using (Camera camera = new Camera(DeviceType.GigE, CameraSelectionStrategy.FirstFound))
                {
                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);
                    String deviceType = camera.CameraInfo[CameraInfoKey.DeviceType];

                    Console.WriteLine("==========");
                    Console.WriteLine("{0} Camera", deviceType);
                    Console.WriteLine("==========");
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                    camera.StreamGrabber.ImageGrabbed += OnImageSkipped;
                    // Get the Key from the user for selecting the mode
                    
                    Console.Write( "Start multicast sample in (c)ontrol or in (m)onitor mode? (c/m) " );
                    ConsoleKeyInfo keyPressed = Console.ReadKey();
                    switch (keyPressed.KeyChar)
                    {
                        // The default configuration must be removed when monitor mode is selected
                        // because the monitoring application is not allowed to modify any parameter settings.
                        case 'm':
                        case 'M':
                            // Monitor mode selected.
                            Console.WriteLine("\nIn Monitor mode");

                            // Set MonitorModeActive to true to act as monitor
                            camera.Parameters [PLCameraInstance.MonitorModeActive].SetValue( true );// Set monitor mode

                            // Open the camera.
                            camera.Open();

                            // Select transmission type. If the camera is already controlled by another application
                            // and configured for multicast, the active camera configuration can be used
                            // (IP Address and Port will be set automatically).
                            camera.Parameters[PLGigEStream.TransmissionType].TrySetValue(PLGigEStream.TransmissionType.UseCameraConfig);

                            // Alternatively, the stream grabber could be explicitly set to "multicast"...
                            // In this case, the IP Address and the IP port must also be set.
                            //
                            //camera.Parameters[PLGigEStream.TransmissionType].SetValue(PLGigEStream.TransmissionType.Multicast);
                            //camera.Parameters[PLGigEStream.DestinationAddr].SetValue("239.0.0.1");
                            //camera.Parameters[PLGigEStream.DestinationPort].SetValue(49152);

                            if ( (camera.Parameters[PLGigEStream.DestinationAddr].GetValue() != "0.0.0.0") &&
                                 (camera.Parameters[PLGigEStream.DestinationPort].GetValue() != 0))
                            {
                                camera.StreamGrabber.Start(countOfImagesToGrab);
                            }
                            else
                            {
                                throw new Exception("Failed to open stream grabber (monitor mode): The acquisition is not yet started by the controlling application. Start the controlling application before starting the monitor application.");
                            }
                            break;
                                
                        case 'c':
                        case 'C':
                            // Controlling mode selected.
                            Console.WriteLine("\nIn Control mode");

                            // Open the camera.
                            camera.Open();

                            // Set transmission type to "multicast"...
                            // In this case, the IP Address and the IP port must also be set.
                            camera.Parameters[PLGigEStream.TransmissionType].SetValue(PLGigEStream.TransmissionType.Multicast);
                            //camera.Parameters[PLGigEStream.DestinationAddr].SetValue("239.0.0.1");
                            //camera.Parameters[PLGigEStream.DestinationPort].SetValue(49152);

                            // Maximize the image area of interest (Image AOI).
                            camera.Parameters[PLGigECamera.OffsetX].TrySetValue(camera.Parameters[PLGigECamera.OffsetX].GetMinimum());
                            camera.Parameters[PLGigECamera.OffsetY].TrySetValue(camera.Parameters[PLGigECamera.OffsetY].GetMinimum());
                            camera.Parameters[PLGigECamera.Width].SetValue(camera.Parameters[PLGigECamera.Width].GetMaximum());
                            camera.Parameters[PLGigECamera.Height].SetValue(camera.Parameters[PLGigECamera.Height].GetMaximum());

                            // Set the pixel data format.
                            camera.Parameters[PLGigECamera.PixelFormat].SetValue(PLGigECamera.PixelFormat.Mono8);

                            camera.StreamGrabber.Start();
                            break;

                        default:
                            throw new NotSupportedException("Invalid mode selected.");
                    }

                    IGrabResult grabResult;

                    // Camera.StopGrabbing() is called automatically by the RetrieveResult() method
                    // when countOfImagesToGrab images have been retrieved in monitor mode
                    // or when a key is pressed and the camera object is destroyed.
                    Console.WriteLine("Press any key to quit FrameGrabber...");
                        
                    while (!Console.KeyAvailable && camera.StreamGrabber.IsGrabbing)
                    {
                        grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (grabResult)
                        {
                            // Image grabbed successfully? 
                            if (grabResult.GrabSucceeded)
                            {
                                // Display the image
                                ImageWindow.DisplayImage(1, grabResult);

                                // The grab result could now be processed here.
                            }
                            else
                            {
                                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                            }
                        }
                    }
                        
                    camera.Close();
                }
            }
            catch (Exception e)
            {
                // Error handling
                Console.Error.WriteLine("\nException: {0}", e.Message);
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
