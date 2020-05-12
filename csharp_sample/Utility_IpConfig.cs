/*
    This sample demonstrates how to configure the IP address of a camera device.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;


namespace Utility_IpConfig
{
    class Utility_IpConfig
    {
        internal static void Main(string[] args)
        {
            // The exit code of the sample application.
            int exitCode = 0;

            try
            {
                List<ICameraInfo> deviceList = IpConfigurator.EnumerateAllDevices();

                if (args.Length < 2)
                {
                    // Print usage information.
                    Console.WriteLine("Usage: Utility_IpConfig <MAC> <IP> [MASK] [GATEWAY]");
                    Console.WriteLine("       <MAC> is the MAC address without separators, e.g., 0030531596CF");
                    Console.WriteLine("       <IP> is one of the following:");
                    Console.WriteLine("            - AUTO to use Auto-IP (LLA).");
                    Console.WriteLine("            - DHCP to use DHCP.");
                    Console.WriteLine("            - Everything else is interpreted as a new IP address in dotted notation, e.g., 192.168.1.1");
                    Console.WriteLine("       [MASK] is the network mask in dotted notation. This is optional. 255.255.255.0 is used as default.");
                    Console.WriteLine("       [GATEWAY] is the gateway address in dotted notation. This is optional. 0.0.0.0 is used as default.");
                    Console.WriteLine("Please note that this is a sample and no sanity checks are made.");
                    Console.WriteLine("");
                    Console.WriteLine(String.Format("{0,-103}{1,-15}", "Available Devices", "   supports "));
                    Console.WriteLine(String.Format("{0,-32}{1,-14}{2,-17}{3,-17}{4,-13}{5,-9}{6,-5}{7,-6}{8,-5}",
                                                "Friendly Name", "MAC", "IP Address", "Subnet Mask", "Gateway", "Mode", "IP?", "DHCP?", "LLA?"));

                    foreach (var device in deviceList)
                    {
                        // Determine currently active configuration method.
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

                        Console.WriteLine(String.Format("{0,-32}{1,-14}{2,-17}{3,-17}{4,-13}{5,-9}{6,-5}{7,-6}{8,-5}",
                                device[CameraInfoKey.FriendlyName], device[CameraInfoKey.DeviceMacAddress], device[CameraInfoKey.DeviceIpAddress],
                                device[CameraInfoKey.SubnetMask], device[CameraInfoKey.DefaultGateway], currentConfig,
                                IpConfigurator.IsPersistentIpSupported(device), IpConfigurator.IsDhcpSupported(device), IpConfigurator.IsAutoIpSupported(device)));
                    }
                    exitCode = 1;
                }
                else
                {
                    // Read arguments. Note that sanity checks are skipped for clarity.
                    String macAddress = args[0];
                    String ipAddress = args[1];
                    String subnetMask = "255.255.255.0";
                    if (args.Length >= 3)
                    {
                        subnetMask = args[2];
                    }
                    String defaultGateway = "0.0.0.0";
                    if (args.Length >= 4)
                    {
                        defaultGateway = args[3];
                    }

                    // Check if configuration mode is AUTO, DHCP, or IP address.
                    bool isAuto = args[1].Equals("AUTO");
                    bool isDhcp = args[1].Equals("DHCP");
                    IpConfigurationMethod configurationMethod = IpConfigurationMethod.StaticIP;
                    if (isAuto)
                    {
                        configurationMethod = IpConfigurationMethod.AutoIP;
                    }
                    else if (isDhcp)
                    {
                        configurationMethod = IpConfigurationMethod.DHCP; 
                    }

                    // Find the camera's user-defined name.
                    String userDefinedName = "";
                    foreach (var device in deviceList)
                    {
                        if (macAddress == device[CameraInfoKey.DeviceMacAddress])
                        {
                            userDefinedName = device[CameraInfoKey.UserDefinedName];
                        }
                    }

                    // Set new IP configuration.
                    bool setOk = false;
                    if (configurationMethod == IpConfigurationMethod.StaticIP)
                    {
                        setOk = IpConfigurator.ChangeIpConfiguration(macAddress, configurationMethod, ipAddress, subnetMask, defaultGateway);
                    }
                    else
                    {
                        setOk = IpConfigurator.ChangeIpConfiguration(macAddress, configurationMethod);
                    }

                    if(setOk)
                    {
                        Console.WriteLine("Successfully changed IP configuration via broadcast for device {0} to {1}.", macAddress, ipAddress);
                    }
                    else
                    {
                        Console.WriteLine("Failed to change IP configuration via broadcast for device {0}.", macAddress);
                        Console.WriteLine("This is not an error. The device may not support broadcast IP configuration.");
                    }
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
