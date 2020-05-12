/*  
    Basler USB3 Vision and GigE Vision cameras can send event messages. For example, when a sensor
    exposure has finished, the camera can send an Exposure End event to the computer. The event
    can reach the computer before the image data for the finished exposure has been
    transferred completely. This sample illustrates how to get notified when camera event message data
    has been received.

    The event messages are retrieved automatically and processed by the Camera classes.
    The information contained in event messages is exposed as parameter nodes in the camera node map
    and can be accessed like "normal" camera parameters. These nodes are updated
    when a camera event is received. You can register camera event handler objects that are
    triggered when event data has been received.
    
    The handler object provides access to the changed parameter but not its source (camera). 
    In this sample, we solve the problem with a derived camera class in combination with a handler object as member.

    These mechanisms are demonstrated for the Exposure End event.
    The  Exposure End event carries the following information:
    * EventExposureEndFrameID (USB) / ExposureEndEventFrameID (GigE): Indicates the number of the image frame that has been exposed.
    * EventExposureEndTimestamp(USB) / ExposureEndEventTimestamp (GigE): Indicates the moment when the event has been generated.
    This sample shows how to register event handlers that indicate the arrival of events
    sent by the camera. For demonstration purposes, several different handlers are registered
    for the same event.
*/

using System;
using Basler.Pylon;

namespace Grab_CameraEvents
{
    class EventCamera : Camera
    {
        private static Version Sfnc2_0_0 = new Version(2, 0, 0);

        private IntegerName exposureEndDataName;
        private IntegerName exposureEndFrameID;
        private IntegerName exposureEndTimestamp;

        // This IntegerName can be used for GigE as well as for USB cameras. 
        public IntegerName ExposureEndDataName
        {
            get
            {
                return this.exposureEndDataName;
            }
        }

        // This IntegerName selects the frame ID and can be used for GigE as well as for USB cameras. 
        public IntegerName ExposureEndFrameID
        {
            get
            {
                return this.exposureEndFrameID;
            }
        }

        // This IntegerName selects the timestamp and can be used for GigE as well as for USB cameras. 
        public IntegerName ExposureEndTimestamp
        {
            get
            {
                return this.exposureEndTimestamp;
            }
        }

        public EventCamera()
            : base()
        {
        }


        // Configure camera for event trigger and register exposure end event handler. 
        public bool Configure()
        {
            // In this sample, a software trigger is used to demonstrate synchronous processing of the grab results. 
            // If you want to react to an event as quickly as possible, you have to use Configuration.AcquireContinuous. 
            CameraOpened += Configuration.SoftwareTrigger;

            if (Parameters[PLCameraInstance.GrabCameraEvents].IsWritable)
            {
                Parameters[PLCameraInstance.GrabCameraEvents].SetValue(true);
            }
            else
            {
                throw new Exception("Can not enable GrabCameraEvents.");
            }

            if (base.Open(1000, TimeoutHandling.Return))
            {
                //Check if camera supports waiting for trigger ready
                if (!base.CanWaitForFrameTriggerReady)
                {
                    throw new Exception("This sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
                }

                // Features, e.g., 'ExposureEnd', are named according to the GenICam Standard Feature Naming Convention (SFNC).
                // The SFNC defines a common set of features, their behavior, and the related parameter names.
                // This ensures the interoperability of cameras from different camera vendors.
                // Cameras compliant with the USB3 Vision standard are based on SFNC version 2.0.
                // Basler GigE and FireWire cameras are based on previous SFNC versions.
                // Accordingly, the behavior of these cameras and some parameters names will be different.
                // The SFNC version can be used to handle differences between camera device models.
                if (this.GetSfncVersion() < Sfnc2_0_0)
                {
                    // The naming convention for ExposureEnd differs between SFNC 2.0 and previous versions.
                    exposureEndDataName = PLGigECamera.ExposureEndEventTimestamp;
                    exposureEndFrameID = PLGigECamera.ExposureEndEventFrameID;
                    exposureEndTimestamp = PLGigECamera.ExposureEndEventTimestamp;
                }
                else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
                {
                    exposureEndDataName = PLUsbCamera.EventExposureEnd;
                    exposureEndFrameID = PLUsbCamera.EventExposureEndFrameID;
                    exposureEndTimestamp = PLUsbCamera.EventExposureEndTimestamp;
                }

                // Check if the device supports events. 
                if (Parameters[PLCamera.EventSelector].CanSetValue(PLCamera.EventSelector.ExposureEnd) == false)
                {
                    throw new Exception("The device doesn't support exposure end event.");
                }

                // Add a callback function to receive the changed FrameID value. 
                Parameters[exposureEndDataName].ParameterChanged += OnEventExposureEndData;
                // Enable sending of Exposure End events.
                // Select the event to receive.
                Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.ExposureEnd);
                // Enable it.
                Parameters[PLCamera.EventNotification].SetValue(PLCamera.EventNotification.On);
            }
            return true;
        }

        // Event handler for exposure end. Only very short processing tasks should be performed by this method.
        // Otherwise, the event notification will block the processing of images. 
        public void OnEventExposureEndData(Object sender, ParameterChangedEventArgs e)
        {
            if (Parameters[exposureEndFrameID].IsReadable && Parameters[exposureEndTimestamp].IsReadable)
            {
                Console.WriteLine("OnEventExposureEndData: Camera: {0} EventArgs {1} FrameID {2} TimeStamp {3}"
                        , CameraInfo[CameraInfoKey.ModelName]
                        , e.Parameter.ToString()
                        , Parameters[exposureEndFrameID].ToString()
                        , Parameters[exposureEndTimestamp].ToString());
            }
        }
    }

    class Grab_CameraEvent
    {
        internal static void Main()
        {
            const int c_countOfImagesToGrab = 10;
            int exitCode = 0;

            try
            {
                // Create a camera object and select the first camera device found. 
                using (EventCamera eventCamera = new EventCamera())
                {
                    // Register the ExposureEnd event with the event handler member.
                    eventCamera.Configure();

                    // Register an event handler object with an anonymous method. The object is important if you want to unregister this event. 
                    EventHandler<ParameterChangedEventArgs> handlerTimestamp = (s, e) =>
                    {
                        Console.WriteLine("Anonymous method: TimeStamp {0}", e.Parameter.ToString());
                    };

                    eventCamera.Parameters[eventCamera.ExposureEndTimestamp].ParameterChanged += handlerTimestamp;

                    eventCamera.StreamGrabber.Start(c_countOfImagesToGrab);
                    while (eventCamera.StreamGrabber.IsGrabbing)
                    {
                        if (eventCamera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                        {
                            eventCamera.ExecuteSoftwareTrigger();
                        }
                        // Wait for an image and then retrieve it. A timeout of 5000 ms is used. 
                        IGrabResult grabResult = eventCamera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (grabResult)
                        {
                            // Image grabbed successfully? 
                            if (grabResult.GrabSucceeded)
                            {
                                ImageWindow.DisplayImage(0, grabResult);
                            }
                            else
                            {
                                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                            }
                        }
                    }
                    // If events are not required anymore, you should unregister the event handlers. 
                    eventCamera.Parameters[eventCamera.ExposureEndDataName].ParameterChanged -= eventCamera.OnEventExposureEndData;
                    eventCamera.Parameters[eventCamera.ExposureEndTimestamp].ParameterChanged -= handlerTimestamp;
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
