/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    Demonstrates how to use user configuration sets (user sets) and how to configure the camera
    to start up with the user defined settings of user set 1.

    You can also configure your camera using the pylon Viewer and
    store your custom settings in a user set of your choice.


    ATTENTION:
    Executing this sample will overwrite all current settings in user set 1.
*/

using System;
using Basler.Pylon;

namespace ParameterizeCamera_UserSets
{
    class ParameterizeCamera_UserSets
    {
        static Version sfnc2_0_0 = new Version(2, 0, 0);
        public static EnumName userDefaultSelector;
        public static string userDefaultSelectorUserSet1;

        public static void Configure(Camera camera)
        {
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                userDefaultSelector = PLCamera.UserSetDefaultSelector;
                userDefaultSelectorUserSet1 = PLCamera.UserSetDefaultSelector.UserSet1;
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                userDefaultSelector = PLCamera.UserSetDefault;
                userDefaultSelectorUserSet1 = PLCamera.UserSetDefault.UserSet1;
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
                    // Before accessing camera device parameters, the camera must be opened.
                    camera.Open();

                    Configure(camera);

                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    // Print the device type
                    String deviceType = camera.CameraInfo[CameraInfoKey.DeviceType];
                    Console.WriteLine("Testing {0} Camera Params:", deviceType);

                    // Remember the current default user set selector so we can restore it later when cleaning up.
                    String oldDefaultUserSet = camera.Parameters[userDefaultSelector].GetValue();

                    // Load default settings.
                    Console.WriteLine("Loading Default Params");
                    camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.Default);
                    camera.Parameters[PLCamera.UserSetLoad].Execute();

                    // Set gain and exposure time values.
                    // The camera won't let you set specific values when related auto functions are active.
                    // So we need to disable the related auto functions before setting the values.
                    Console.WriteLine("Turning off Gain Auto and Exposure Auto");
                    camera.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off);
                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        camera.Parameters[PLCamera.GainRaw].SetValue(camera.Parameters[PLCamera.GainRaw].GetMinimum());
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        camera.Parameters[PLCamera.Gain].SetValue(camera.Parameters[PLCamera.Gain].GetMinimum());
                    }
                    camera.Parameters[PLCamera.ExposureAuto].TrySetValue( PLCamera.ExposureAuto.Off);
                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        camera.Parameters[PLCamera.ExposureTimeRaw].SetValue(camera.Parameters[PLCamera.ExposureTimeRaw].GetMinimum());
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        camera.Parameters[PLCamera.ExposureTime].SetValue(camera.Parameters[PLCamera.ExposureTime].GetMinimum());
                    }

                    // Save to user set 1.
                    //
                    // ATTENTION:
                    // This will overwrite all settings previously saved in user set 1.
                    Console.WriteLine("Saving Currently Active Settings to user set 1");
                    camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.Default);
                    camera.Parameters[PLCamera.UserSetSave].Execute();

                    // Load Default Settings.
                    Console.WriteLine("Loading default settings.");
                    camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.Default);
                    camera.Parameters[PLCamera.UserSetLoad].Execute();
                    
                    // Show Default Settings.
                    Console.WriteLine("Default settings");
                    Console.WriteLine("================");
                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        Console.WriteLine("Gain                :{0}", camera.Parameters[PLCamera.GainRaw].GetValue());
                        Console.WriteLine("Exposure Time       :{0}", camera.Parameters[PLCamera.ExposureTimeRaw].GetValue());
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        Console.WriteLine("Gain                :{0}", camera.Parameters[PLCamera.Gain].GetValue());
                        Console.WriteLine("Exposure Time       :{0}", camera.Parameters[PLCamera.ExposureTime].GetValue());
                    }

                    // Show User Set 1 settings.
                    Console.WriteLine("Loading User set 1 Settings.");
                    camera.Parameters[PLCamera.UserSetSelector].SetValue(PLCamera.UserSetSelector.UserSet1);
                    camera.Parameters[PLCamera.UserSetLoad].Execute();
                    Console.WriteLine("User Set 1 Settings");
                    Console.WriteLine("===================");
                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        Console.WriteLine("Gain                :{0}", camera.Parameters[PLCamera.GainRaw].GetValue());
                        Console.WriteLine("Exposure Time       :{0}", camera.Parameters[PLCamera.ExposureTimeRaw].GetValue());
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        Console.WriteLine("Gain                :{0}", camera.Parameters[PLCamera.Gain].GetValue());
                        Console.WriteLine("Exposure Time       :{0}", camera.Parameters[PLCamera.ExposureTime].GetValue());
                    }

                    // Set user set 1 as default user set:
                    // When the camera wakes up it will be configured
                    // with the settings from user set 1.
                    camera.Parameters[userDefaultSelector].SetValue(userDefaultSelectorUserSet1);

                    // Restore the default user set selector.
                    camera.Parameters[userDefaultSelector].SetValue(oldDefaultUserSet);

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
