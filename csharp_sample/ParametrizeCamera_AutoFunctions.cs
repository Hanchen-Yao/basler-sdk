/*
    Note: Before getting started, Basler recommends reading the Programmer's Guide topic
    in the pylon .NET API documentation that gets installed with pylon.

    This sample illustrates how to use the Auto Functions feature of Basler cameras.

    Features, like 'Gain', are named according to the Standard Feature Naming Convention (SFNC).
    The SFNC defines a common set of features, their behavior, and the related parameter names.
    This ensures the interoperability of cameras from different camera vendors. Cameras compliant
    with the USB 3 Vision standard are based on the SFNC version 2.0.
    Basler GigE and Firewire cameras are based on previous SFNC versions.
    Accordingly, the behavior of these cameras and some parameters names will be different.
*/

using System;
using System.Collections.Generic;
using Basler.Pylon;

namespace ParameterizeCamera_AutoFunctions
{
    class ParameterizeCamera_AutoFunctions
    {
        private static Version sfnc2_0_0 = new Version(2, 0, 0);
        private static EnumName regionSelector;
        private static IntegerName regionSelectorWidth, regionSelectorHeight, regionSelectorOffsetX, regionSelectorOffsetY;
        private static String regionSelectorValue1, regionSelectorValue2;
        private static FloatName balanceRatio, exposureTime;
        private static BooleanName autoFunctionAOIROIUseBrightness, autoFunctionAOIROIUseWhiteBalance;

        static void Configure(Camera camera)
        {
            if (camera.GetSfncVersion() < sfnc2_0_0)  // Handling for older cameras
            {
                regionSelector = PLCamera.AutoFunctionAOISelector;
                regionSelectorOffsetX = PLCamera.AutoFunctionAOIOffsetX;
                regionSelectorOffsetY = PLCamera.AutoFunctionAOIOffsetY;
                regionSelectorWidth = PLCamera.AutoFunctionAOIWidth;
                regionSelectorHeight = PLCamera.AutoFunctionAOIHeight;
                regionSelectorValue1 = PLCamera.AutoFunctionAOISelector.AOI1;
                regionSelectorValue2 = PLCamera.AutoFunctionAOISelector.AOI2;
                balanceRatio = PLCamera.BalanceRatioAbs;
                exposureTime = PLCamera.ExposureTimeAbs;
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
                balanceRatio = PLCamera.BalanceRatio;
                exposureTime = PLCamera.ExposureTime;
                autoFunctionAOIROIUseBrightness = PLCamera.AutoFunctionROIUseBrightness;
                autoFunctionAOIROIUseWhiteBalance = PLCamera.AutoFunctionROIUseWhiteBalance;
            }
        }

        // Check if camera is a color camera
        static bool IsColorCamera(Camera camera)
        {
            bool result = false;
            IEnumerable<String> enteries;
            enteries = camera.Parameters[PLCamera.PixelFormat].GetAllValues();
            foreach (String ent in enteries)
            {
                if (camera.Parameters [PLCamera.PixelFormat].CanSetValue( ent ))
                {
                    if (ent.Contains( "Bayer" ))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        static void AutoGainOnce(Camera camera)
        {
            // Check whether the gain auto function is available.
            if (!camera.Parameters[PLCamera.GainAuto].IsWritable)
            {
                Console.WriteLine("The camera does not support Gain Auto.");
                return;
            }

            // Maximize the grabbed image area of interest (Image AOI).
            camera.Parameters[PLCamera.OffsetX].TrySetValue( camera.Parameters[PLCamera.OffsetX].GetMinimum());
            camera.Parameters[PLCamera.OffsetX].TrySetValue( camera.Parameters[PLCamera.OffsetY].GetMinimum());
            camera.Parameters[PLCamera.Width].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[PLCamera.Height].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // Set the Auto Function ROI for luminance statistics.
            // We want to use ROI1 for gathering the statistics.
            if (camera.Parameters[autoFunctionAOIROIUseBrightness].IsWritable)
            {
                camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(true);// ROI 1 is used for brightness control
                camera.Parameters[regionSelector].SetValue(regionSelectorValue2);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(false);// ROI 2 is not used for brightness control
            }
            camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
            camera.Parameters[regionSelectorOffsetX].SetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters[regionSelectorOffsetY].SetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[regionSelectorWidth].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[regionSelectorHeight].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // We are going to try GainAuto = Once.
            Console.WriteLine( "Trying 'GainAuto = Once'." );
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                // Set the target value for luminance control. The value is always expressed
                // as an 8 bit value regardless of the current pixel data output format,
                // i.e., 0 -> black, 255 -> white.
                camera.Parameters[PLCamera.AutoTargetValue].SetValue(80);

                Console.WriteLine("Initial Gain = {0}", camera.Parameters[PLCamera.GainRaw].GetValue());
                // Set the gain ranges for luminance control.
                camera.Parameters[PLCamera.AutoGainRawLowerLimit].SetValue( camera.Parameters[PLCamera.GainRaw].GetMinimum());
                camera.Parameters[PLCamera.AutoGainRawUpperLimit].SetValue( camera.Parameters[PLCamera.GainRaw].GetMaximum());
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                // Set the target value for luminance control.
                // A value of 0.3 means that the target brightness is 30 % of the maximum brightness of the raw pixel value read out from the sensor.
                // A value of 0.4 means 40 % and so forth.
                camera.Parameters[PLCamera.AutoTargetBrightness].SetValue(0.3);

                Console.WriteLine("Initial Gain = {0}", camera.Parameters[PLCamera.Gain].GetValue());
                // Set the gain ranges for luminance control.
                camera.Parameters[PLCamera.AutoGainLowerLimit].SetValue( camera.Parameters[PLCamera.Gain].GetMinimum());
                camera.Parameters[PLCamera.AutoGainUpperLimit].SetValue( camera.Parameters[PLCamera.Gain].GetMaximum());
            }
            camera.Parameters[PLCamera.GainAuto].SetValue(PLCamera.GainAuto.Once);

            // When the "once" mode of operation is selected,
            // the parameter values are automatically adjusted until the related image property
            // reaches the target value. After the automatic parameter value adjustment is complete, the auto
            // function will automatically be set to "off" and the new parameter value will be applied to the
            // subsequently grabbed images.
            int n = 0;
            while (camera.Parameters[PLCamera.GainAuto].GetValue() != PLCamera.GainAuto.Off)
            {
                IGrabResult result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                using (result)
                {
                    // Image grabbed successfully? 
                    if (result.GrabSucceeded)
                    {
                        ImageWindow.DisplayImage(1, result);
                    }
                }
                n++;
                //For demonstration purposes only. Wait until the image is shown.
                System.Threading.Thread.Sleep(100);

                //Make sure the loop is exited.
                if (n > 100)
                {
                    throw new TimeoutException("The adjustment of auto gain did not finish.");
                }
            }

            Console.WriteLine("GainAuto went back to 'Off' after {0} frames", n);
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                Console.WriteLine("Final Gain = {0}", camera.Parameters[PLCamera.GainRaw].GetValue());
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                Console.WriteLine("Final Gain = {0}", camera.Parameters[PLCamera.Gain].GetValue());
            }
        }


        static void AutoGainContinuous(Camera camera)
        {
            // Check whether the Gain Auto feature is available.
            if (!camera.Parameters[PLCamera.GainAuto].IsWritable)
            {
                Console.WriteLine("The camera does not support Gain Auto.");
                return;
            }

            // Maximize the grabbed image area of interest (Image AOI).
            camera.Parameters [PLCamera.OffsetX].TrySetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters [PLCamera.OffsetX].TrySetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[PLCamera.Width].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[PLCamera.Height].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // Set the Auto Function ROI for luminance statistics.
            // We want to use ROI1 for gathering the statistics.
            if (camera.Parameters[autoFunctionAOIROIUseBrightness].IsWritable)
            {
                camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(true);// ROI 1 is used for brightness control
                camera.Parameters[regionSelector].SetValue(regionSelectorValue2);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(false);// ROI 2 is not used for brightness control
            }
            camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
            camera.Parameters[regionSelectorOffsetX].SetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters[regionSelectorOffsetY].SetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[regionSelectorWidth].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[regionSelectorHeight].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // We are trying GainAuto = Continuous.
            Console.WriteLine( "Trying 'GainAuto' = Continuous" );
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                // Set the target value for luminance control. The value is always expressed
                // as an 8 bit value regardless of the current pixel data output format,
                // i.e., 0 -> black, 255 -> white.
                camera.Parameters[PLCamera.AutoTargetValue].SetValue(80);

                Console.WriteLine("Initial Gain = {0}", camera.Parameters[PLCamera.GainRaw].GetValue());
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                // Set the target value for luminance control.
                // A value of 0.3 means that the target brightness is 30 % of the maximum brightness of the raw pixel value read out from the sensor.
                // A value of 0.4 means 40 % and so forth.
                camera.Parameters[PLCamera.AutoTargetBrightness].SetValue(0.3);

                Console.WriteLine("Initial Gain = {0}", camera.Parameters[PLCamera.Gain].GetValue());
            }
            camera.Parameters[PLCamera.GainAuto].SetValue(PLCamera.GainAuto.Continuous);

            // When "continuous" mode is selected, the parameter value is adjusted repeatedly while images are acquired.
            // Depending on the current frame rate, the automatic adjustments will usually be carried out for
            // every or every other image unless the camera's microcontroller is kept busy by other tasks.
            // The repeated automatic adjustment will proceed until the "once" mode of operation is used or
            // until the auto function is set to "off", in which case the parameter value resulting from the latest
            // automatic adjustment will operate unless the value is manually adjusted.
            for (int n = 0; n < 20; n++)            // For demonstration purposes, we will grab "only" 20 images.
            {
                IGrabResult result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
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
            camera.Parameters[PLCamera.GainAuto].SetValue(PLCamera.GainAuto.Off); // Switch off GainAuto.

            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                Console.WriteLine("Final Gain = {0}", camera.Parameters[PLCamera.GainRaw].GetValue());
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                Console.WriteLine("Final Gain = {0}", camera.Parameters[PLCamera.Gain].GetValue());
            }

        }


        static void AutoExposureOnce(Camera camera)
        {
            // Check whether auto exposure is available
            if (!camera.Parameters[PLCamera.ExposureAuto].IsWritable)
            {
                Console.WriteLine("The camera doesnot support Exposure Auto.");
                return;
            }

            // Maximize the grabbed image area of interest (Image AOI).
            camera.Parameters[PLCamera.OffsetX].TrySetValue( camera.Parameters[PLCamera.OffsetX].GetMinimum());
            camera.Parameters[PLCamera.OffsetY].TrySetValue( camera.Parameters[PLCamera.OffsetY].GetMinimum());
            camera.Parameters[PLCamera.Width].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[PLCamera.Height].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // Set the Auto Function ROI for luminance statistics.
            // We want to use ROI1 for gathering the statistics.
            if (camera.Parameters[autoFunctionAOIROIUseBrightness].IsWritable)
            {
                camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(true);// ROI 1 is used for brightness control
                camera.Parameters[regionSelector].SetValue(regionSelectorValue2);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(false);// ROI 2 is not used for brightness control
            }
            camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
            camera.Parameters[regionSelectorOffsetX].SetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters[regionSelectorOffsetY].SetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[regionSelectorWidth].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[regionSelectorHeight].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // Try ExposureAuto = Once.
            Console.WriteLine( "Trying 'ExposureAuto = Once'." );
            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                // Set the target value for luminance control. The value is always expressed
                // as an 8 bit value regardless of the current pixel data output format,
                // i.e., 0 -> black, 255 -> white.
                camera.Parameters[PLCamera.AutoTargetValue].SetValue(80);

                Console.WriteLine("Initial Exposure time = {0} us", camera.Parameters[PLCamera.ExposureTimeAbs].GetValue());

                // Set the exposure time ranges for luminance control.
                camera.Parameters[PLCamera.AutoExposureTimeAbsLowerLimit].SetValue( camera.Parameters[PLCamera.AutoExposureTimeAbsLowerLimit].GetMinimum());
                camera.Parameters[PLCamera.AutoExposureTimeAbsUpperLimit].SetValue( camera.Parameters[PLCamera.AutoExposureTimeAbsLowerLimit].GetMaximum());
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                // Set the target value for luminance control.
                // A value of 0.3 means that the target brightness is 30 % of the maximum brightness of the raw pixel value read out from the sensor.
                // A value of 0.4 means 40 % and so forth.
                camera.Parameters[PLCamera.AutoTargetBrightness].SetValue(0.3);

                Console.WriteLine("Initial Exposure time = {0} us", camera.Parameters[PLCamera.ExposureTime].GetValue());

                // Set the exposure time ranges for luminance control.
                camera.Parameters[PLCamera.AutoExposureTimeLowerLimit].SetValue( camera.Parameters[PLCamera.AutoExposureTimeLowerLimit].GetMinimum());
                camera.Parameters[PLCamera.AutoExposureTimeUpperLimit].SetValue( camera.Parameters[PLCamera.AutoExposureTimeLowerLimit].GetMaximum());
            }
            camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Once);

            // When the "once" mode of operation is selected,
            // the parameter values are automatically adjusted until the related image property
            // reaches the target value. After the automatic parameter value adjustment is complete, the auto
            // function will automatically be set to "off", and the new parameter value will be applied to the
            // subsequently grabbed images.
            int n = 0;
            while (camera.Parameters[PLCamera.ExposureAuto].GetValue() != PLCamera.ExposureAuto.Off)
            {
                IGrabResult result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                using (result)
                {
                    // Image grabbed successfully? 
                    if (result.GrabSucceeded)
                    {
                        ImageWindow.DisplayImage(1, result);
                    }
                }
                n++;

                //For demonstration purposes only. Wait until the image is shown.
                System.Threading.Thread.Sleep(100);

                //Make sure the loop is exited.
                if (n > 100)
                {
                    throw new TimeoutException("The adjustment of auto exposure did not finish.");
                }
            }
            Console.WriteLine("ExposureAuto went back to 'Off' after {0} frames", n);
            Console.WriteLine("Final Exposure Time = {0} us", camera.Parameters[exposureTime].GetValue());
        }


        static void AutoExposureContinuous(Camera camera)
        {
            // Check whether the Exposure Auto feature is available.
            if (!camera.Parameters[PLCamera.ExposureAuto].IsWritable)
            {
                Console.WriteLine("The camera does not support Exposure Auto.");
                return;
            }

            // Maximize the grabbed image area of interest (Image AOI).
            camera.Parameters[PLCamera.OffsetX].TrySetValue( camera.Parameters[PLCamera.OffsetX].GetMinimum());
            camera.Parameters[PLCamera.OffsetY].TrySetValue( camera.Parameters[PLCamera.OffsetY].GetMinimum());
            camera.Parameters[PLCamera.Width].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[PLCamera.Height].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            // Set the Auto Function ROI for luminance statistics.
            // We want to use ROI1 for gathering the statistics.
            if (camera.Parameters[autoFunctionAOIROIUseBrightness].IsWritable)
            {
                camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(true);// ROI 1 is used for brightness control
                camera.Parameters[regionSelector].SetValue(regionSelectorValue2);
                camera.Parameters[autoFunctionAOIROIUseBrightness].SetValue(false);// ROI 2 is not used for brightness control
            }
            camera.Parameters[regionSelector].SetValue(regionSelectorValue1);
            camera.Parameters[regionSelectorOffsetX].SetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters[regionSelectorOffsetY].SetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[regionSelectorWidth].SetValue( camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[regionSelectorHeight].SetValue( camera.Parameters[PLCamera.Height].GetMaximum());

            if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
            {
                // Set the target value for luminance control. The value is always expressed
                // as an 8 bit value regardless of the current pixel data output format,
                // i.e., 0 -> black, 255 -> white.
                camera.Parameters[PLCamera.AutoTargetValue].SetValue(80);
            }
            else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
            {
                // Set the target value for luminance control.
                // A value of 0.3 means that the target brightness is 30 % of the maximum brightness of the raw pixel value read out from the sensor.
                // A value of 0.4 means 40 % and so forth.
                camera.Parameters[PLCamera.AutoTargetBrightness].SetValue(0.3);
            }

            // Try ExposureAuto = Continuous.
            Console.WriteLine("Trying 'ExposureAuto = Continuous'.");
            Console.WriteLine("Initial Exposure time = {0} us", camera.Parameters[exposureTime].GetValue());
            camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Continuous);

            // When "continuous" mode is selected, the parameter value is adjusted repeatedly while images are acquired.
            // Depending on the current frame rate, the automatic adjustments will usually be carried out for
            // every or every other image, unless the camera's microcontroller is kept busy by other tasks.
            // The repeated automatic adjustment will proceed until the "once" mode of operation is used or
            // until the auto function is set to "off", in which case the parameter value resulting from the latest
            // automatic adjustment will operate unless the value is manually adjusted.
            for (int n = 0; n < 20; n++)    // For demonstration purposes, we will use only 20 images.
            {
                IGrabResult result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
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
            camera.Parameters[PLCamera.ExposureAuto].SetValue(PLCamera.ExposureAuto.Off); // Switch off Exposure Auto.

            Console.WriteLine("Final Exposure Time = {0} us", camera.Parameters[exposureTime].GetValue());
        }


        static void AutoWhiteBalance(Camera camera)
        {
            // Check whether the Balance White Auto feature is available.
            if (!camera.Parameters[PLCamera.BalanceWhiteAuto].IsWritable)
            {
                Console.WriteLine("The Camera does not support balance white auto.");
                return;
            }

            // Maximize the grabbed area of interest (Image AOI).
            camera.Parameters[PLCamera.OffsetX].TrySetValue(camera.Parameters[PLCamera.OffsetX].GetMinimum());
            camera.Parameters[PLCamera.OffsetY].TrySetValue(camera.Parameters[PLCamera.OffsetY].GetMinimum());
            camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

            // Set the Auto Function ROI for white balace statistics.
            // We want to use ROI2 for gathering the statistics.
            if (camera.Parameters [regionSelector].IsWritable)
            {
                camera.Parameters [regionSelector].SetValue( regionSelectorValue1 );
                camera.Parameters [autoFunctionAOIROIUseWhiteBalance].SetValue( false );// ROI 1 is not used for white balance control
                camera.Parameters [regionSelector].SetValue( regionSelectorValue2 );
                camera.Parameters [autoFunctionAOIROIUseWhiteBalance].SetValue( true );// ROI 2 is used for white balance control
            }
            camera.Parameters[regionSelector].SetValue(regionSelectorValue2);
            camera.Parameters[regionSelectorOffsetX].SetValue( camera.Parameters [PLCamera.OffsetX].GetMinimum() );
            camera.Parameters[regionSelectorOffsetY].SetValue( camera.Parameters [PLCamera.OffsetY].GetMinimum() );
            camera.Parameters[regionSelectorWidth].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
            camera.Parameters[regionSelectorHeight].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());

            Console.WriteLine("Trying 'BalanceWhiteAuto = Once'.");
            Console.WriteLine("Initial balance ratio:");
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Red);
            Console.Write("R = {0}  ", camera.Parameters[balanceRatio].GetValue());
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Green);
            Console.Write("G = {0}  ", camera.Parameters[balanceRatio].GetValue());
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Blue);
            Console.Write("B = {0}  ", camera.Parameters[balanceRatio].GetValue());
            camera.Parameters[PLCamera.BalanceWhiteAuto].SetValue(PLCamera.BalanceWhiteAuto.Once);

            // When the "once" mode of operation is selected,
            // the parameter values are automatically adjusted until the related image property
            // reaches the target value. After the automatic parameter value adjustment is complete, the auto
            // function will automatically be set to "off" and the new parameter value will be applied to the
            // subsequently grabbed images.
            int n = 0;
            while (camera.Parameters[PLCamera.BalanceWhiteAuto].GetValue() != PLCamera.BalanceWhiteAuto.Off)
            {
                IGrabResult result = camera.StreamGrabber.GrabOne(5000, TimeoutHandling.ThrowException);
                using (result)
                {
                    // Image grabbed successfully? 
                    if (result.GrabSucceeded)
                    {
                        ImageWindow.DisplayImage(1, result);
                    }
                }
                n++;

                //For demonstration purposes only. Wait until the image is shown.
                System.Threading.Thread.Sleep(100);

                //Make sure the loop is exited.
                if (n > 100)
                {
                    throw new TimeoutException("The adjustment of auto white balance did not finish.");
                }
            }
            Console.WriteLine("BalanceWhiteAuto went back to 'Off' after {0} Frames", n);
            Console.WriteLine("Final balance ratio: ");
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Red);
            Console.Write("R = {0}  ", camera.Parameters[balanceRatio].GetValue());
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Green);
            Console.Write("G = {0}  ", camera.Parameters[balanceRatio].GetValue());
            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Blue);
            Console.Write("B = {0}  ", camera.Parameters[balanceRatio].GetValue());
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

                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    Configure(camera);

                    // Check the device type
                    String deviceType = camera.CameraInfo[CameraInfoKey.DeviceType];
                    Console.WriteLine("Testing {0} Camera Params:", deviceType);
                    Console.WriteLine("==============================");

                    // Disable test image generator if available
                    camera.Parameters[PLCamera.TestImageSelector].TrySetValue(PLCamera.TestImageSelector.Off);
                    camera.Parameters[PLCamera.TestPattern].TrySetValue(PLCamera.TestPattern.Off);

                    // Only area scan cameras support auto functions.
                    if (camera.Parameters[PLCamera.DeviceScanType].GetValue() == PLCamera.DeviceScanType.Areascan)
                    {
                        // All area scan cameras support luminance control.

                        // Carry out luminance control by using the "once" gain auto function.
                        // For demonstration purposes only, set the gain to an initial value.
                        if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                        {
                            camera.Parameters [PLCamera.GainRaw].SetValue( camera.Parameters [PLCamera.GainRaw].GetMaximum() );
                        }
                        else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                        {
                            camera.Parameters [PLCamera.Gain].SetValue( camera.Parameters [PLCamera.Gain].GetMaximum() );
                        }
                        AutoGainOnce( camera );

                        Console.WriteLine("Press Enter to continue.");
                        Console.ReadLine();

                        // Carry out luminance control by using the "continuous" gain auto function.
                        // For demonstration purposes only, set the gain to an initial value.
                        if (camera.GetSfncVersion() < sfnc2_0_0) // Handling for older cameras
                        {
                            camera.Parameters[PLCamera.GainRaw].SetValue(camera.Parameters[PLCamera.GainRaw].GetMaximum());
                        }
                        else // Handling for newer cameras (using SFNC 2.0, e.g. USB3 Vision cameras)
                        {
                            camera.Parameters[PLCamera.Gain].SetValue(camera.Parameters[PLCamera.Gain].GetMaximum());
                        }
                        AutoGainContinuous(camera);

                        Console.WriteLine("Press Enter to continue.");
                        Console.ReadLine();

                        // For demonstration purposes only, set the exposure time to an initial value.
                        camera.Parameters[exposureTime].SetValue(camera.Parameters[exposureTime].GetMinimum());

                        // Carry out luminance control by using the "once" exposure auto function.
                        AutoExposureOnce(camera);
                        Console.WriteLine("Press Enter to continue.");
                        Console.ReadLine();

                        // For demonstration purposes only, set the exposure time to an initial value.
                        camera.Parameters[exposureTime].SetValue(camera.Parameters[exposureTime].GetMinimum());

                        // Carry out luminance control by using the "once" exposure auto function.
                        AutoExposureContinuous(camera);

                        // Only color cameras support the balance white auto function.
                        if (IsColorCamera(camera))
                        {
                            Console.WriteLine("Press Enter to continue.");
                            Console.ReadLine();

                            // For demonstration purposes only, set the initial balance ratio values:
                            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Red);
                            camera.Parameters[balanceRatio].SetToMaximum();
                            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Green);
                            camera.Parameters[balanceRatio].TrySetValuePercentOfRange(50.0);
                            camera.Parameters[PLCamera.BalanceRatioSelector].SetValue(PLCamera.BalanceRatioSelector.Blue);
                            camera.Parameters[balanceRatio].SetToMinimum();

                            // Carry out white balance using the balance white auto function.
                            AutoWhiteBalance(camera);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Only area scan cameras support auto functions.");
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

            // Comment the following two lines to disable waiting on exit.
            Console.Error.WriteLine("\nPress enter to exit.");
            Console.ReadLine();

            Environment.Exit(exitCode);
        }
    }
}
