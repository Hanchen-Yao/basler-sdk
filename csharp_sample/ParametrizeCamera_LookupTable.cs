/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample program demonstrates the use of the Luminance Lookup Table feature.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;

namespace ParameterizeCamera_LookupTable
{
    class ParameterizeCamera_LookupTable
    {
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
                    Console.WriteLine("Opening camera...");
                    // Open the camera.
                    camera.Open();
                    Console.WriteLine("Done\n");

                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);
                    //Check the device type
                    String deviceType = camera.CameraInfo[CameraInfoKey.DeviceType];
                    Console.WriteLine("Testing {0} Camera Params:", deviceType);
                    Console.WriteLine("==============================");

                    Console.WriteLine("Writing LUT....");

                    // Select the lookup table using the LUTSelector.
                    camera.Parameters[PLCamera.LUTSelector].SetValue(PLCamera.LUTSelector.Luminance);

                    // Some cameras have 10 bit and others have 12 bit lookup tables, so determine
                    // the type of the lookup table for the current device.
                    int nValues = (int) camera.Parameters[PLCamera.LUTIndex].GetMaximum() + 1;
                    int inc = 0;
                    if (nValues == 4096) // 12 bit LUT.
                    {
                        inc = 8;
                    }
                    else if (nValues == 1024) // 10 bit LUT.
                    {
                        inc = 2;
                    }
                    else
                    {
                        throw new Exception("Type of LUT is not supported by this sample.");
                    }

                    // Use LUTIndex and LUTValue parameter to access the lookup table values.
                    // The following lookup table causes an inversion of the sensor values.
                    for (int i = 0; i < nValues; i += inc)
                    {
                        camera.Parameters[PLCamera.LUTIndex].SetValue(i);
                        camera.Parameters[PLCamera.LUTValue].SetValue(nValues - 1 - i);
                    }
                    Console.WriteLine("DONE");

                    // Enable the lookup table.
                    camera.Parameters[PLCamera.LUTEnable].SetValue(true);
                    // Grab and process images here.
                    // ...

                    // Disable the lookup table.
                    camera.Parameters[PLCamera.LUTEnable].SetValue(false);
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
