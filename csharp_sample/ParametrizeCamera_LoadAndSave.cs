/*
    This sample application demonstrates how to save or load the features of a camera
    to or from a file.
*/

using System;
using Basler.Pylon;

namespace ParameterizeCamera_LoadAndSave
{
class ParameterizeCamera_LoadAndSave
{
    internal static void Main()
    {
        // The exit code of the sample application.
        int exitCode = 0;

        // The name of the pylon feature stream file.
        const string filename = "CameraParameters.pfs";

        try
        {
            // Create a camera object that selects the first camera device found.
            // More constructors are available for selecting a specific camera device.
            using (Camera camera = new Camera())
            {
                // Print the model name of the camera.
                Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                // Before accessing camera device parameters, the camera must be opened.
                camera.Open();

                Console.WriteLine("Saving camera device parameters to file {0} ...", filename);
                // Save the content of the camera device parameters in the file.
                camera.Parameters.Save(filename, ParameterPath.CameraDevice);

                Console.WriteLine("Reading file {0} back to camera device parameters ...", filename);
                // Just for demonstration, read the content of the file back to the camera device parameters.
                camera.Parameters.Load(filename, ParameterPath.CameraDevice);

                // Close the camera.
                camera.Close();
            }
        }
        catch (Exception e)
        {
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
