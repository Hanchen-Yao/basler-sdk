/*
    This sample shows how to issue a GigE Vision action command to multiple cameras.
    By using an action command, multiple cameras can be triggered at the same time
    as opposed to software triggering where each camera has to be triggered individually.

    To make the execution and configuration of multiple cameras easier, this sample uses the ActionCommandTrigger class.
*/

using System;
using Basler.Pylon;
using System.Collections.Generic;

namespace Grab_UsingActionCommand
{
    class Grab_UsingActionCommand
    {
        /////////////////////////////////////////////////////////////////
        // Limits the amount of cameras used for grabbing.
        // It is important to manage the available bandwidth when grabbing with multiple
        // cameras. This applies, for instance, if two GigE cameras are connected to the
        // same network adapter via a switch. 
        // To avoid potential bandwidth problems, it's possible to optimize the 
        // transport layer using the pylon Viewer Bandwidth Manager.
        const int c_maxCamerasToUse = 2;


        internal static void Main()
        {
            int exitCode = 0;
            List<Camera> cameras = new List<Camera>();

            try
            {
                // Ask the camera finder for a list of all GigE camera devices. 
                // Note that this sample only works with GigE camera devices. 
                List<ICameraInfo> allDeviceInfos = CameraFinder.Enumerate(DeviceType.GigE);

                if (allDeviceInfos.Count == 0)
                {
                    throw new ApplicationException("No GigE cameras present.");
                }

                // Open all cameras to fulfill preconditions for Configure(ICamera()) 
                allDeviceInfos.ForEach(cameraInfo => cameras.Add(new Camera(cameraInfo)));
                cameras.ForEach(camera => camera.Open());

                // Prepare all cameras for action commands 
                ActionCommandTrigger actionCommandTrigger = new ActionCommandTrigger();

                // Configure all cameras to wait for the action command. If a camera doesn't support action commands, an exception will be thrown. 
                actionCommandTrigger.Configure(cameras.ToArray());

                // Starts grabbing on all cameras. 
                // The cameras won't transmit any image data because they are configured to wait for an action command. 
                cameras.ForEach(camera => camera.StreamGrabber.Start());

                // Now we issue the action command to all devices without any DeviceKey, GroupKey, or GroupMask 
                // because Configure(ICamera()) had already set these parameters. 
                actionCommandTrigger.Issue();

                // Retrieve images from all cameras. 
                foreach (Camera camera in cameras)
                {
                    // Camera will return grab results in the order they arrive. 
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                    using (grabResult)
                    {
                        // Image grabbed successfully? 
                        if (grabResult.GrabSucceeded)
                        {
                            // Print the model name and the IP address of the camera. 
                            Console.WriteLine("Image grabbed successfully for: {0} ({1})",
                                camera.CameraInfo.GetValueOrDefault(CameraInfoKey.FriendlyName, null),
                                camera.CameraInfo.GetValueOrDefault(CameraInfoKey.DeviceIpAddress, null));
                        }
                        else
                        {
                            // If a buffer hasn't been grabbed completely, the network bandwidth is possibly insufficient for transferring 
                            // multiple images simultaneously. See note above c_maxCamerasToUse. 
                            Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }
                }
                // To avoid overtriggering, you should call cameras[0].WaitForFrameTriggerReady 
                // (see Grab_UsingGrabLoopThread sample for details). 
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception: {0}", e.Message);
                exitCode = 1;
            }
            finally
            {
                // Stop stream grabber and close all cameras. 
                cameras.ForEach(camera => { camera.StreamGrabber.Stop(); camera.Close(); camera.Dispose(); });
                // Comment the following two lines to disable waiting on exit. 
                Console.Error.WriteLine("\nPress enter to exit.");
                Console.ReadLine();
            }

            Environment.Exit(exitCode);
        }
    }
}
