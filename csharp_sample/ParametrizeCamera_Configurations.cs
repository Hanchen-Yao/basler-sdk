/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon C# API documentation that gets installed with pylon.
    If you are upgrading to a higher major version of pylon, Basler also
    strongly recommends reading the Migration topic in the pylon C# API documentation.

    This sample shows how to use configuration event handlers by applying the standard
    configurations and registering sample configuration event handlers.

    If the configuration event handler is registered the registered methods are called
    when the state of the camera objects changes, e.g. when the camera object is opened
    or closed. In pylon.NET a configuration event handler is a method that parametrizes
    the camera.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;

namespace ParameterizeCamera_Configurations
{
    class ParameterizeCamera_Configurations
    {
        // Number of images to be grabbed.
        static int countOfImagesToGrab = 3;


        public static void PixelFormatAndAoiConfiguration( object sender, EventArgs e )
        {
            ICamera camera = sender as ICamera;
            camera.Parameters [PLCamera.OffsetX].TrySetToMinimum();
            camera.Parameters [PLCamera.OffsetY].TrySetToMinimum();

            camera.Parameters [PLCamera.Width].TrySetToMaximum();
            camera.Parameters [PLCamera.Height].TrySetToMaximum();

            camera.Parameters [PLCamera.PixelFormat].TrySetValue( PLCamera.PixelFormat.Mono8 );
        }


        // Shown here for demonstration purposes only to illustrate the effect of this configuration.
        static void AcquireContinuous( object sender, EventArgs e)
        {
            // Disable all trigger types.
            DisableAllTriggers( sender, e );

            // Disable compression.
            DisableCompression( sender, e );

            // Set acquisition mode to Continuous.
            ICamera camera = sender as ICamera;
            camera.Parameters [(EnumName)"AcquisitionMode"].SetValue( "Continuous" );
        }


        // Shown here for demonstration purposes only to illustrate the effect of this configuration.
        static void AcquireSingleFrame( object sender, EventArgs e)
        {
            // Disable all trigger types.
            DisableAllTriggers( sender, e );

            // Disable compression.
            DisableCompression( sender, e );

            // Set acquisition mode to SingleFrame.
            ICamera camera = sender as ICamera;
            camera.Parameters [(EnumName)"AcquisitionMode"].SetValue( "SingleFrame" );
        }


        // Shown here for demonstration purposes only to illustrate the effect of this configuration.
        public static void SoftwareTrigger( object sender, EventArgs e )
        {
            ICamera camera = sender as ICamera;
            // Get required Enumerations.
            IEnumParameter triggerSelector = camera.Parameters [PLCamera.TriggerSelector];
            IEnumParameter triggerMode = camera.Parameters [PLCamera.TriggerMode];
            IEnumParameter triggerSource = camera.Parameters [PLCamera.TriggerSource];


            // Check the available camera trigger mode(s) to select the appropriate one: acquisition start trigger mode
            // (used by older cameras, i.e. for cameras supporting only the legacy image acquisition control mode;
            // do not confuse with acquisition start command) or frame start trigger mode
            // (used by newer cameras, i.e. for cameras using the standard image acquisition control mode;
            // equivalent to the acquisition start trigger mode in the legacy image acquisition control mode).
            string triggerName = "FrameStart";
            if (!triggerSelector.CanSetValue( triggerName ))
            {
                triggerName = "AcquisitionStart";
                if (!triggerSelector.CanSetValue( triggerName ))
                {
                    throw new NotSupportedException( "Could not select trigger. Neither FrameStart nor AcquisitionStart is available." );
                }
            }

            try
            {
                foreach (string trigger in triggerSelector)
                {
                    triggerSelector.SetValue( trigger );

                    if (triggerName == trigger)
                    {
                        // Activate trigger.
                        triggerMode.SetValue( PLCamera.TriggerMode.On );

                        // Set the trigger source to software.
                        triggerSource.SetValue( PLCamera.TriggerSource.Software );
                    }
                    else
                    {
                        // Turn trigger mode off.
                        triggerMode.SetValue( PLCamera.TriggerMode.Off );
                    }
                }
            }
            finally
            {
                // Set selector for software trigger.
                triggerSelector.SetValue( triggerName );
            }
            // Set acquisition mode to Continuous
            camera.Parameters [PLCamera.AcquisitionMode].SetValue( PLCamera.AcquisitionMode.Continuous );
        }



        // Shown here for demonstration purposes only to illustrate the effect of this configuration.
        static void DisableAllTriggers( object sender, EventArgs e )
        {
            ICamera camera = sender as ICamera;
            // Disable all trigger types.
            //------------------------------------------------------------------------------

            // Get required enumerations.
            IEnumParameter triggerSelector = camera.Parameters [PLCamera.TriggerSelector];
            IEnumParameter triggerMode = camera.Parameters [PLCamera.TriggerMode];

            // Remember previous selector value.
            string oldSelectorValue = triggerSelector.IsReadable ? triggerSelector.GetValue() : null;

            try
            {
                // Turn trigger mode off for all trigger selector entries.
                foreach (string trigger in triggerSelector)
                {
                    triggerSelector.SetValue( trigger );
                    triggerMode.SetValue( PLCamera.TriggerMode.Off );
                }
            }
            finally
            {
                // Restore previous selector.
                if (oldSelectorValue != null)
                {
                    triggerSelector.SetValue( oldSelectorValue );
                }
            }
            // Set acquisition mode to Continuous.
            camera.Parameters [PLCamera.AcquisitionMode].SetValue( PLCamera.AcquisitionMode.SingleFrame );
        }

        // Shown here for demonstration purposes only to illustrate the effect of this configuration.
        static void DisableCompression( object sender, EventArgs e )
        {
            ICamera camera = sender as ICamera;

            // Disable compression mode.
            //------------------------------------------------------------------------------

            // Get required enumeration.
            IEnumParameter compressionMode = camera.Parameters [(EnumName)"ImageCompressionMode"];

            if (compressionMode.IsWritable)
            {
                // Turn off compression mode.
                compressionMode.SetValue( "Off" );
            }
        }

        //It is used as a CImageEventPrinter like in C++
        //OnImageGrabbed is used to print the image information like Width, Height etc..
        //Can be used to implement other functionality for image grab event.
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

                    IGrabResult result;
                    
                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    // Print the device type
                    String deviceType = camera.CameraInfo[CameraInfoKey.DeviceType];
                    Console.WriteLine("Testing {0} Camera Params:", deviceType);
                    Console.WriteLine("==============================");

                    //Register handler for acquired images
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                    Console.WriteLine("Grab using continuous acquisition:");

                    // Register the standard configuration event handler for setting up the camera for continuous acquisition.
                    camera.CameraOpened += Configuration.AcquireContinuous;

                    // The camera's Open() method calls the configuration handler's method that
                    // applies the required parameter modifications.
                    camera.Open();

                    // Grab some images for demonstration.
                    camera.StreamGrabber.Start(countOfImagesToGrab);
                    while (camera.StreamGrabber.IsGrabbing)
                    {
                        result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    }

                    // Close the camera
                    camera.Close();

                    //-------------------------------------------------------------

                    Console.WriteLine("Grab using software trigger mode:");

                    // Register the standard configuration event handler for setting up the camera for software
                    // triggering.
                    camera.CameraOpened += Configuration.SoftwareTrigger;

                    // The camera's Open() method calls the configuration handler's method that
                    // applies the required parameter modifications.
                    camera.Open();

                    // Check if camera supports waiting for trigger ready
                    if (camera.CanWaitForFrameTriggerReady)
                    {
                        // StartGrabbing() calls the camera's Open() automatically if the camera is not open yet.
                        // The Open method calls the configuration handler's OnOpened() method that
                        // sets the required parameters for enabling software triggering.
                        // Grab some images for demonstration.

                        camera.StreamGrabber.Start(countOfImagesToGrab);
                        while (camera.StreamGrabber.IsGrabbing)
                        {
                            if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                            {
                                camera.ExecuteSoftwareTrigger();
                            }
                            result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        }
                    }
                    else
                    {
                        Console.WriteLine("The software trigger sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
                    }

                    //Close the camera
                    camera.Close();

                    //-------------------------------------------------------------

                    Console.WriteLine("Grab using single frame acquisition:");

                    // Register the standard configuration event handler for setting up the camera for
                    // single frame acquisition.
                    camera.CameraOpened += Configuration.AcquireSingleFrame;

                    // The camera's Open() method calls the configuration handler's method that
                    // applies the required parameter modifications.
                    camera.Open();

                    //Start multiple single grabs as configured.
                    result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                    result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                    result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                    result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);

                    //Close the camera
                    camera.Close();

                    //-------------------------------------------------------------

                    Console.WriteLine("Grab using multiple configuration objects:");

                    // Register the standard configuration event handler for setting up the camera for
                    // single frame acquisition and a custom event handler for pixel format and AOI configuration.
                    camera.CameraOpened += Configuration.AcquireSingleFrame;
                    camera.CameraOpened += PixelFormatAndAoiConfiguration;
                    
                    // The camera's Open() method calls the configuration handler's method that
                    // applies the required parameter modifications.
                    camera.Open();

                    result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);

                    //Close the camera
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
