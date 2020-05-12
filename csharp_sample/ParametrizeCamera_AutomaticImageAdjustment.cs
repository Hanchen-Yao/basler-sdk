/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample illustrates how to copy the 'Nice Image' button of the Basler PylonViewer.

*/

using System;
using Basler.Pylon;

namespace ParametrizeCamera_AutomaticImageAdjustment
{
    class ParametrizeCamera_AutomaticImageAdjustment
    {

        private static Version sfnc2_0_0 = new Version(2, 0, 0);
        private static EnumName regionSelector;
        private static BooleanName autoFunctionAOIROIUseWhiteBalance, autoFunctionAOIROIUseBrightness;
        private static IntegerName regionSelectorWidth, regionSelectorHeight, regionSelectorOffsetX, regionSelectorOffsetY;
        private static String regionSelectorValue1, regionSelectorValue2;


        public static void Configure(Camera camera)
        {
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                regionSelector = PLCamera.AutoFunctionAOISelector;
                regionSelectorOffsetX = PLCamera.AutoFunctionAOIOffsetX;
                regionSelectorOffsetY = PLCamera.AutoFunctionAOIOffsetY;
                regionSelectorWidth = PLCamera.AutoFunctionAOIWidth;
                regionSelectorHeight = PLCamera.AutoFunctionAOIHeight;
                regionSelectorValue1 = PLCamera.AutoFunctionAOISelector.AOI1;
                regionSelectorValue2 = PLCamera.AutoFunctionAOISelector.AOI2;
                autoFunctionAOIROIUseBrightness = PLCamera.AutoFunctionAOIUsageIntensity;
                autoFunctionAOIROIUseWhiteBalance = PLCamera.AutoFunctionAOIUsageWhiteBalance;
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                regionSelector = PLCamera.AutoFunctionROISelector;
                regionSelectorOffsetX = PLCamera.AutoFunctionROIOffsetX;
                regionSelectorOffsetY = PLCamera.AutoFunctionROIOffsetY;
                regionSelectorWidth = PLCamera.AutoFunctionROIWidth;
                regionSelectorHeight = PLCamera.AutoFunctionROIHeight;
                regionSelectorValue1 = PLCamera.AutoFunctionROISelector.ROI1;
                regionSelectorValue2 = PLCamera.AutoFunctionROISelector.ROI2;
                autoFunctionAOIROIUseBrightness = PLCamera.AutoFunctionROIUseBrightness;
                autoFunctionAOIROIUseWhiteBalance = PLCamera.AutoFunctionROIUseWhiteBalance;
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

                    // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                    //camera.CameraOpened += Configuration.AcquireContinuous;

                    // Open the connection to the camera device.
                    camera.Open();

                    Configure(camera);

                    // Set the pixel format to one from a list of ones compatible with this example
                    string[] pixelFormats = new string[]
                    {
                        PLCamera.PixelFormat.YUV422_YUYV_Packed,
                        PLCamera.PixelFormat.YCbCr422_8,
                        PLCamera.PixelFormat.BayerBG8,
                        PLCamera.PixelFormat.BayerRG8,
                        PLCamera.PixelFormat.BayerGR8,
                        PLCamera.PixelFormat.BayerGB8,
                        PLCamera.PixelFormat.Mono8
                    };
                    camera.Parameters[PLCamera.PixelFormat].SetValue(pixelFormats);

                    // Disable test image generator if available
                    camera.Parameters[PLCamera.TestImageSelector].TrySetValue(PLCamera.TestImageSelector.Off);
                    camera.Parameters[PLCamera.TestPattern].TrySetValue(PLCamera.TestPattern.Off);

                    // Set the Auto Function ROI for luminance and white balance statistics.
                    // We want to use ROI2 for gathering the statistics.
                    if (camera.Parameters[regionSelector].IsWritable)
                    {
                        camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                        camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(true);// ROI 1 is used for brightness control
                        camera.Parameters[autoFunctionAOIROIUseWhiteBalance].SetValue(true);// ROI 1 is used for white balance control
                    }
                    camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                    camera.Parameters[regionSelectorOffsetX].SetValue(camera.Parameters[PLCamera.OffsetX].GetMinimum());
                    camera.Parameters[regionSelectorOffsetY].SetValue(camera.Parameters[PLCamera.OffsetY].GetMinimum());
                    camera.Parameters[regionSelectorWidth].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
                    camera.Parameters[regionSelectorHeight].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        camera.Parameters[PLCamera.ProcessedRawEnable].TrySetValue(true);
                        camera.Parameters[PLCamera.GammaEnable].TrySetValue(true);
                        camera.Parameters[PLCamera.GammaSelector].TrySetValue(PLCamera.GammaSelector.sRGB);
                        camera.Parameters[PLCamera.AutoTargetValue].TrySetValue(80);
                        camera.Parameters[PLCamera.AutoFunctionProfile].TrySetValue(PLCamera.AutoFunctionProfile.GainMinimum);
                        camera.Parameters[PLCamera.AutoGainRawLowerLimit].TrySetToMinimum();
                        camera.Parameters[PLCamera.AutoGainRawUpperLimit].TrySetToMaximum();
                        camera.Parameters[PLCamera.AutoExposureTimeAbsLowerLimit].TrySetToMinimum();
                        camera.Parameters[PLCamera.AutoExposureTimeAbsUpperLimit].TrySetToMaximum();
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        camera.Parameters[PLCamera.AutoTargetBrightness].TrySetValue(0.3);
                        camera.Parameters[PLCamera.AutoFunctionProfile].TrySetValue(PLCamera.AutoFunctionProfile.MinimizeGain);
                        camera.Parameters[PLCamera.AutoGainLowerLimit].TrySetToMinimum();
                        camera.Parameters[PLCamera.AutoGainUpperLimit].TrySetToMaximum();
                        double maxExposure = camera.Parameters[PLCamera.AutoExposureTimeUpperLimit].GetMaximum();
                        // Reduce upper limit to one second for this example
                        if (maxExposure > 1000000)
                        {
                            maxExposure = 1000000;
                        }
                        camera.Parameters[PLCamera.AutoExposureTimeUpperLimit].TrySetValue(maxExposure);
                    }

                    // Set all auto functions to once in this example
                    camera.Parameters[PLCamera.GainSelector].TrySetValue(PLCamera.GainSelector.All);
                    camera.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Once);
                    camera.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Once);
                    camera.Parameters[PLCamera.BalanceWhiteAuto].TrySetValue(PLCamera.BalanceWhiteAuto.Once);
                    if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                    {
                        camera.Parameters[PLCamera.LightSourceSelector].TrySetValue(PLCamera.LightSourceSelector.Daylight);
                    }
                    else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                    {
                        camera.Parameters[PLCamera.LightSourcePreset].TrySetValue(PLCamera.LightSourcePreset.Daylight5000K);
                    }

                    camera.StreamGrabber.Start();
                    for (int n = 0; n < 20; n++)            // For demonstration purposes, we will grab "only" 20 images.
                    {
                        IGrabResult result = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (result)
                        {
                            // Image grabbed successfully? 
                            if (result.GrabSucceeded)
                            {
                                ImageWindow.DisplayImage(1, result);
                            }
                        }

                        //For demonstration purposes only. Wait until the image is shown.
                        System.Threading.Thread.Sleep(100);
                    }

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
