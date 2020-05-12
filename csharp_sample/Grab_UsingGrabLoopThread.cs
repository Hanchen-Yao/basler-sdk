/*
    This sample illustrates how to grab and process images using the grab loop thread
    provided by the Instant Camera class.
*/

using System;
using System.Threading;
using Basler.Pylon;

namespace Grab_UsingGrabLoopThread
{
class Grab_UsingGrabLoopThread
{
    // Example of an image event handler.
    static void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
    {
        // The grab result is automatically disposed when the event call back returns.
        // The grab result can be cloned using IGrabResult.Clone if you want to keep a copy of it (not shown in this sample).
        IGrabResult grabResult = e.GrabResult;
        // Image grabbed successfully?
        if (grabResult.GrabSucceeded)
        {
            // Access the image data.
            Console.WriteLine("SizeX: {0}", grabResult.Width);
            Console.WriteLine("SizeY: {0}", grabResult.Height);
            byte[] buffer = grabResult.PixelData as byte[];
            Console.WriteLine("Gray value of first pixel: {0}", buffer[0]);
            Console.WriteLine("");

            // Display the grabbed image.
            ImageWindow.DisplayImage(0, grabResult);
            ImagePersistence.Save(ImageFileFormat.Bmp, "test.bmp", grabResult);
        }
        else
        {
            Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
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
            using (Camera camera = new Camera(CameraSelectionStrategy.FirstFound))
            {
                // Print the model name of the camera.
                Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                // Set the acquisition mode to software triggered continuous acquisition when the camera is opened.
                camera.CameraOpened += Configuration.SoftwareTrigger;

                //Open the connection to the camera device.
                camera.Open();

                //Check if camera supports waiting for trigger ready
                if (camera.CanWaitForFrameTriggerReady)
                {

                    // Set a handler for processing the images.
                    camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                    // Start grabbing using the grab loop thread. This is done by setting the grabLoopType parameter
                    // to GrabLoop.ProvidedByStreamGrabber. The grab results are delivered to the image event handler OnImageGrabbed.
                    // The default grab strategy (GrabStrategy_OneByOne) is used.
                    camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);

                    // Wait for user input to trigger the camera or exit the loop.
                    // Software triggering is used to trigger the camera device.
                    Char key;
                    do
                    {
                        Console.WriteLine("Press 't' to trigger the camera or 'e' to exit.");

                        key = Console.ReadKey(true).KeyChar;
                        if ((key == 't' || key == 'T'))
                        {
                            // Execute the software trigger. Wait up to 1000 ms until the camera is ready for trigger.
                            if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                            {
                                camera.ExecuteSoftwareTrigger();
                            }
                        }
                    }
                    while ((key != 'e') && (key != 'E'));

                    // Stop grabbing.
                    camera.StreamGrabber.Stop();
                }
                else
                {
                    Console.WriteLine("This sample can only be used with cameras that can be queried whether they are ready to accept the next frame trigger.");
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
