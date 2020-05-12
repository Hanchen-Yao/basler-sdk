/*
    This sample illustrates how to enable the Compression Beyond feature in Basler cameras and
    how to decompress images using the ImageDecompressor class.
*/


using System;
using Basler.Pylon;

namespace Utility_ImageDecompressor
{
    class Utility_ImageDecompressor
    {
        static void PrintCompressionInfo( CompressionInfo compressionInfo)
        {
            Console.WriteLine( "\n\nCompression info:" );
            Console.WriteLine( "HasCompressedImage: {0}", compressionInfo.HasCompressedImage );
            Console.WriteLine( "CompressionStatus: {0} ({0:D})", compressionInfo.CompressionStatus );
            Console.WriteLine( "Lossy: {0}", compressionInfo.Lossy );
            Console.WriteLine( "Width: {0}", compressionInfo.Width );
            Console.WriteLine( "Height: {0}", compressionInfo.Height );
            Console.WriteLine( "PixelType: {0} ({0:D}) ", compressionInfo.PixelType);
            Console.WriteLine( "DecompressedImageSize: {0}", compressionInfo.DecompressedImageSize );
            Console.WriteLine( "DecompressedPayloadSize: {0}", compressionInfo.DecompressedPayloadSize );
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
                    Console.WriteLine( "Using camera {0}.", camera.CameraInfo [CameraInfoKey.ModelName] );

                    // Set the acquisition mode to single frame acquisition when the camera is opened.
                    camera.CameraOpened += Configuration.AcquireSingleFrame;

                    // Open the connection to the camera device.
                    camera.Open();

                    // Remember the original compression mode.
                    string oldCompressionMode = camera.Parameters [PLCamera.ImageCompressionMode].GetValue();

                    // Set the compression mode to BaslerCompressionBeyond if available.
                    camera.Parameters [PLCamera.ImageCompressionMode].SetValue( PLCamera.ImageCompressionMode.BaslerCompressionBeyond );
                    // After enabling the compression, we can read the compression rate option.
                    string oldCompressionRateOption = camera.Parameters [PLCamera.ImageCompressionRateOption].GetValue();
                    // Configure lossless compression.
                    camera.Parameters [PLCamera.ImageCompressionRateOption].SetValue( PLCamera.ImageCompressionRateOption.Lossless );

                    // Create the decompressor and initialize it with the camera.
                    using (ImageDecompressor decompressor = new ImageDecompressor( camera ))
                    {
                        // Wait max. 5000ms for a new image.
                        IGrabResult grabResult = camera.StreamGrabber.GrabOne( 5000 );
                        using (grabResult)
                        {
                            if (grabResult.GrabSucceeded)
                            {
                                // Fetch compression info and check whether the image was compressed by the camera.
                                CompressionInfo compressionInfo = new CompressionInfo();
                                if (ImageDecompressor.GetCompressionInfo( ref compressionInfo, grabResult ))
                                {
                                    // Print content of CompressionInfo.
                                    PrintCompressionInfo( compressionInfo );

                                    // Check if we have a valid compressed image                                    
                                    if (compressionInfo.CompressionStatus == CompressionStatus.Ok)
                                    {
                                        // Show compression ratio.
                                        Console.WriteLine( "\nTransferred compressed payload: {0}", grabResult.PayloadSize );
                                        Console.WriteLine( "Compression ratio: {0:N2}%", (Single)grabResult.PayloadSize / (Single)compressionInfo.DecompressedPayloadSize * 100.0 );

                                        // Create buffer for storing the decompressed image.
                                        var myBuffer = new Byte [compressionInfo.DecompressedImageSize];

                                        // Decompress the image.
                                        decompressor.DecompressImage( myBuffer, grabResult );
                                            
                                        // Show the image.
                                        ImageWindow.DisplayImage( 1, myBuffer, compressionInfo.PixelType, compressionInfo.Width, compressionInfo.Height, 0, ImageOrientation.TopDown );
                                    }
                                    else
                                    {
                                        Console.WriteLine( "There was an error while the camera was compressing the image." );
                                    }
                                }
                            }
                            else
                            {
                                // Somehow image grabbing failed.
                                Console.WriteLine( "Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription );
                            }
                        }

                        Console.WriteLine( "\n\n--- Switching to Fix Ratio compression ---" );

                        // Take another picture with lossy compression (if available).
                        if (camera.Parameters [PLCamera.ImageCompressionRateOption].TrySetValue( PLCamera.ImageCompressionRateOption.FixRatio ))
                        {
                            // After changing the compression parameters, the decompressor MUST be reconfigured.
                            decompressor.SetCompressionDescriptor( camera );

                            // Wait max. 5000ms for a new image.
                            grabResult = camera.StreamGrabber.GrabOne( 5000 );
                            using (grabResult)
                            {
                                if (grabResult.GrabSucceeded)
                                {
                                    // Fetch compression info and check whether the image was compressed by the camera.
                                    CompressionInfo compressionInfo = new CompressionInfo();
                                    if (ImageDecompressor.GetCompressionInfo( ref compressionInfo, grabResult ))
                                    {
                                        // Print content of CompressionInfo.
                                        PrintCompressionInfo( compressionInfo );

                                        // Check if we have a valid compressed image
                                        if (compressionInfo.CompressionStatus == CompressionStatus.Ok)
                                        {
                                            // Show compression ratio.
                                            Console.WriteLine( "\nTransferred compressed payload: {0}", grabResult.PayloadSize );
                                            Console.WriteLine( "Compression ratio: {0:N2}%", (Single)grabResult.PayloadSize / (Single)compressionInfo.DecompressedPayloadSize * 100.0 );

                                            // Create buffer for storing the decompressed image.
                                            var myBuffer = new Byte [compressionInfo.DecompressedImageSize];

                                            // Decompress the image.
                                            decompressor.DecompressImage( myBuffer, grabResult );

                                            // Show the image.
                                            ImageWindow.DisplayImage( 2, myBuffer, compressionInfo.PixelType, compressionInfo.Width, compressionInfo.Height, 0, ImageOrientation.TopDown );
                                        }
                                        else
                                        {
                                            Console.WriteLine( "There was an error while the camera was compressing the image." );
                                        }
                                    }
                                }
                                else
                                {
                                    // Somehow image grabbing failed.
                                    Console.WriteLine( "Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription );
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine( "With this setting the camera does not support the \"FixRatio\" Image Compression Rate Option." );
                        }
                    }
                    // restore the old camera settings
                    camera.Parameters [PLCamera.ImageCompressionRateOption].SetValue( oldCompressionRateOption );
                    camera.Parameters [PLCamera.ImageCompressionMode].SetValue( oldCompressionMode );

                    camera.Close();
                }
            }
            catch (InvalidOperationException e)
            {
                Console.Error.WriteLine( "Exception: Camera does not support Compression. {0}", e.Message );
                exitCode = 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine( "Exception: {0}", e.Message );
                exitCode = 1;
            }
            finally
            {
                // Comment the following two lines to disable waiting on exit.
                Console.Error.WriteLine( "\nPress enter to exit." );
                Console.ReadLine();
            }

            Environment.Exit( exitCode );
        }
    }
}
