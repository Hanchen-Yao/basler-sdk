/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample shows how to use the Exposure End event to speed up the image acquisition.
    For example, when a sensor exposure is finished, the camera can send an Exposure End event to the PC.
    The PC can receive the event before the image data of the finished exposure has been completely transferred.
    This can be used in order to avoid an unnecessary delay, for example when an imaged
    object is moved further before the related image data transfer is complete.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;
using System.Diagnostics;

namespace Grab_UsingExposureEndEvent
{
    // Used for logging received events without outputting the information on the screen
    // because outputting will change the timing.
    // This class is used for demonstration purposes only.
    internal class LogItem
    {
        private string eventType;
        private long frameNumber;
        private double time;

        public string EventType
        {
            get
            {
                return this.eventType;
            }
        }
        public long FrameNumber
        {
            get
            {
                return this.frameNumber;
            }
        }
        public double Time
        {
            get
            {
                return time;
            }
        }

        //Stores the values inside private variables
        public LogItem(string type, long frameNr)
        {
            eventType = type;
            frameNumber = frameNr;
            time = Stopwatch.GetTimestamp();
        }
    };

    class Grab_UsingExposureEndEvent
    {
        private static Version sfnc2_0_0 = new Version(2, 0, 0);

        private static long nextExpectedFrameNumberImage;
        private static long nextExpectedFrameNumberExposureEnd;
        private static long nextFrameNumberForMove;

        private static string eventNotificationOn;
        private static IntegerName exposureEndEventFrameId;

        // Number of images to be grabbed.
        public static long countOfImagesToGrab = 50;
        // Create list of LogItem object
        public static List<LogItem> logItems = new List<LogItem>();


        private static void Configure(Camera camera)
        {
            // Camera event processing must be activated first, the default is off.
            if (camera.Parameters[PLCameraInstance.GrabCameraEvents].IsWritable)
            {
                camera.Parameters[PLCameraInstance.GrabCameraEvents].SetValue(true);
            }
            else
            {
                throw new Exception("Can not enable GrabCameraEvents.");
            }

            // Open the camera for setting parameters.
            camera.Open();

            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                nextExpectedFrameNumberImage = 1;
                nextExpectedFrameNumberExposureEnd = 1;
                nextFrameNumberForMove = 1;

                // The naming convention for ExposureEnd differs between SFNC 2.0 and previous versions.
                exposureEndEventFrameId = PLCamera.ExposureEndEventFrameID;
                eventNotificationOn = PLCamera.EventNotification.GenICamEvent;
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                nextExpectedFrameNumberImage = 0;
                nextExpectedFrameNumberExposureEnd = 0;
                nextFrameNumberForMove = 0;

                exposureEndEventFrameId = PLCamera.EventExposureEndFrameID;
                eventNotificationOn = PLCamera.EventNotification.On;
            }

            // Register the event handlers
            camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                camera.Parameters["ExposureEndEventData"].ParameterChanged += delegate (Object sender, ParameterChangedEventArgs e) { OnCameraEventExposureEndData(sender, e, camera); };
                camera.Parameters["FrameStartOvertriggerEventData"].ParameterChanged += delegate (Object sender, ParameterChangedEventArgs e) { OnCameraEventFrameStartOvertriggerData(sender, e); };
                camera.Parameters["EventOverrunEventData"].ParameterChanged += delegate (Object sender, ParameterChangedEventArgs e) { OnCameraEventOverrunEventData( sender, e ); };
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                camera.Parameters["EventExposureEndData"].ParameterChanged += delegate (Object sender, ParameterChangedEventArgs e) { OnCameraEventExposureEndData( sender, e, camera ); };
                camera.Parameters["EventFrameStartOvertriggerData"].ParameterChanged += delegate (Object sender, ParameterChangedEventArgs e) { OnCameraEventFrameStartOvertriggerData(sender, e); };
            }

            // The network packet signaling an event of a GigE camera device can get lost on the network.
            // The following commented parameters can be used to control the handling of lost events.
            //camera.Parameters[ParametersPLGigEEventGrabber.Timeout]
            //camera.Parameters[PLGigEEventGrabber.RetryCount]

            // Check if the device supports events.
            if (!camera.Parameters[PLCamera.EventSelector].IsWritable)
            {
                throw new Exception("The device doesn't support events.");
            }

            // Enable the sending of Exposure End events.
            // Select the event to be received.
            camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.ExposureEnd);
            camera.Parameters[PLCamera.EventNotification].SetValue(eventNotificationOn);

            // Enable the sending of Event Overrun events.
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.EventOverrun);
                camera.Parameters[PLCamera.EventNotification].SetValue(eventNotificationOn);
            }

            // Enable the sending of Frame Start Overtrigger events.
            if (camera.Parameters[PLCamera.EventSelector].CanSetValue(PLCamera.EventSelector.FrameStartOvertrigger))
            {
                camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.FrameStartOvertrigger);
                camera.Parameters[PLCamera.EventNotification].SetValue(eventNotificationOn);
            }
        }


        private static void Disable(Camera camera)
        {
            // Disable the sending of Exposure End events.
            camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.ExposureEnd);
            camera.Parameters[PLCamera.EventNotification].SetValue(PLCamera.EventNotification.Off);

            // Disable the sending of Event Overrun events.
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.EventOverrun);
                camera.Parameters[PLCamera.EventNotification].SetValue(PLCamera.EventNotification.Off);
            }

            // Disable the sending of Frame Start Overtrigger events.
            if (camera.Parameters[PLCamera.EventSelector].CanSetValue(PLCamera.EventSelector.FrameStartOvertrigger))
            {
                camera.Parameters[PLCamera.EventSelector].SetValue(PLCamera.EventSelector.FrameStartOvertrigger);
                camera.Parameters[PLCamera.EventNotification].SetValue(PLCamera.EventNotification.Off);
            }
        }


        private static long IncrementFrameNumber(long frameNr)
        {
            return GetIncrementedFrameNumber(frameNr);
        }


        private static long GetIncrementedFrameNumber(long frameNr)
        {
            frameNr++;
            if (frameNr == 0)
            {
                frameNr++;
            }
            return frameNr;
        }


        private static void MoveImagedItemOrSensorHead()
        {
            // The imaged item or the sensor head can be moved now...
            // The camera may not be ready for a trigger at this point yet because the sensor is still being read out.
            // See the documentation of the CInstantCamera::WaitForFrameTriggerReady() method for more information.
            logItems.Add(new LogItem("Move", nextFrameNumberForMove));
            nextFrameNumberForMove = IncrementFrameNumber(nextFrameNumberForMove);
        }


        private static void OnCameraEventExposureEndData(Object sender, ParameterChangedEventArgs e, Camera camera)
        {
            // An image has been received. Block ID is equal to frame number for GigE camera devices.
            long frameNumber = 0;
            if (camera.Parameters[exposureEndEventFrameId].IsReadable)
            {
                frameNumber = camera.Parameters[exposureEndEventFrameId].GetValue();
            }
            logItems.Add(new LogItem("ExposureEndEvent", frameNumber));

            if (GetIncrementedFrameNumber(frameNumber) != nextExpectedFrameNumberExposureEnd)
            {
                // Check whether the imaged item or the sensor head can be moved.
                // This will be the case if the Exposure End has been lost or if the Exposure End is received later than the image.
                if (frameNumber == nextFrameNumberForMove)
                {
                    MoveImagedItemOrSensorHead();
                }

                // Check for missing images.
                if (frameNumber != nextExpectedFrameNumberExposureEnd)
                {
                    throw new Exception("An Exposure End event has been lost. Expected frame number is " + nextExpectedFrameNumberExposureEnd + " but got frame number" + frameNumber);
                }
                nextExpectedFrameNumberExposureEnd = IncrementFrameNumber(nextExpectedFrameNumberExposureEnd);
            }
        }


        private static void OnCameraEventFrameStartOvertriggerData(Object sender, ParameterChangedEventArgs e)
        {
            logItems.Add(new LogItem("FrameStartOvertrigger", 0));
            //Handle This Error...
        }


        private static void OnCameraEventOverrunEventData(Object sender, ParameterChangedEventArgs e)
        {
            // The camera was unable to send all its events to the PC.
            // Events have been dropped by the camera.
            logItems.Add(new LogItem("EventOverrunEvent", 0));
        }


        //It is used as a CImageEventPrinter like in C++
        //OnImageGrabbed is used to print the image information like Width, Height etc..
        //Can be used to implement other functionality for image grab event.
        private static void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            //Frame number
            long fn = e.GrabResult.BlockID;
            logItems.Add(new LogItem("ImageReceived", fn));
            if (fn == nextFrameNumberForMove)
            {
                MoveImagedItemOrSensorHead();
            }
            if (fn != nextExpectedFrameNumberImage)
            {
                throw new Exception("An image has been lost. Expected frame number is" + nextExpectedFrameNumberExposureEnd + " but got frame number " + fn);
            }
            nextExpectedFrameNumberImage = IncrementFrameNumber(nextExpectedFrameNumberImage);
        }

        //This will print all the LogItems to console
        private static void PrintLog()
        {
            Console.WriteLine("Warning, the printed time values can be wrong on older PC hardware.");
            Console.WriteLine("Time [ms]    Event                 FrameNumber");
            Console.WriteLine("----------   ----------------      --------------");
            int logSize = logItems.Capacity;
            int i = 0;
            var ticks = new List<double>();
            foreach (LogItem item in logItems)
            {
                ticks.Add(item.Time);
            }
            foreach (LogItem item in logItems)
            {
                double time_ms = 0;
                double oldTicks = 0, newTicks = 0;
                if (i > 0)
                {
                    newTicks = ticks[i];
                    oldTicks = ticks[i - 1];
                    time_ms = (((newTicks - oldTicks)) * 1000 / Stopwatch.Frequency);
                }
                i++;
                //{0,10:0.0000} Formatting the size of time_ms to 10 spaces and precision of 4
                Console.WriteLine(String.Format("{0,10:0.0000}", time_ms) + " {0,18}       {1}", item.EventType, item.FrameNumber);
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
                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    // Configure Camera
                    Configure(camera);

                    // Start the grabbing of countOfImagesToGrab images.
                    // The camera device is parameterized with a default configuration which
                    // sets up free-running continuous acquisition.
                    camera.StreamGrabber.Start(countOfImagesToGrab);

                    IGrabResult result;
                    while (camera.StreamGrabber.IsGrabbing)
                    {
                        // Retrieve grab results and notify the camera event and image event handlers.
                        result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (result)
                        {
                             // Nothing to do here with the grab result, the grab results are handled by the registered event handlers.
                        }
                    }

                    // Disable Events
                    Disable(camera);

                    // Print the recorded log showing the timing of events and images.
                    PrintLog();

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
