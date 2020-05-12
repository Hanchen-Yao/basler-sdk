/*
   This sample illustrates how to work with a device behind a router.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;


namespace Utility_AnnouceRemoteDevice
{
    class Utility_AnnouceRemoteDevice
    {
        internal static void PrintCameraList(String headline)
        {
            List<ICameraInfo> deviceList = CameraFinder.Enumerate();

            Console.WriteLine("Available Devices " + headline);
            Console.WriteLine(String.Format("{0,-32}{1,-14}{2,-17}{3,-17}{4,-15}{5,-8}",
                                            "Friendly Name", "MAC", "IP Address", "Subnet Mask", "Gateway", "Mode"));

            foreach (var device in deviceList)
            {
                // Determine currently active configuration method
                String currentConfig;
                if (IpConfigurator.IsPersistentIpActive(device))
                { 
                    currentConfig = "StaticIP"; 
                }
                else if (IpConfigurator.IsDhcpActive(device))
                { 
                    currentConfig = "DHCP"; 
                }
                else if (IpConfigurator.IsAutoIpActive(device))
                {
                    currentConfig = "AutoIP"; 
                }
                else
                {
                    currentConfig = "Unknown"; 
                }

                Console.WriteLine(String.Format("{0,-32}{1,-14}{2,-17}{3,-17}{4,-15}{5,-8}",
                            device[CameraInfoKey.FriendlyName], device[CameraInfoKey.DeviceMacAddress], device[CameraInfoKey.DeviceIpAddress],
                            device[CameraInfoKey.SubnetMask], device[CameraInfoKey.DefaultGateway], currentConfig));
            }
        }

        internal static void Main()
        {
            // The exit code of the sample application.
            int exitCode = 0;

            // The IP address of a GigE camera device behind a router
            String ipAddress = "10.1.1.1";

            // Keep a pylon object so that devices can be announced safely.
            // Check the documentation for AnnounceRemoteDevice() for details.
            Library lib = new Library();

            try
            {
                // Camera list at start - the camera device behind the router is not visible
                PrintCameraList("(at start)");

                IpConfigurator.AnnounceRemoteDevice(ipAddress);

                // Camera list after announce call - the camera device behind the router is visible
                PrintCameraList("(after AnnounceRemoteDevice)");

                IpConfigurator.RenounceRemoteDevice(ipAddress);

                // Camera list after renounce call - the camera device behind the router is not visible
                PrintCameraList("(after RenounceRemoteDevice)");
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
