/*
   This sample demonstrates how to use a user-provided buffer factory.
   Using a buffer factory is optional and intended for advanced use cases only.
   A buffer factory is only necessary if you want to grab into externally supplied buffers.
*/

using System;
using System.Runtime.InteropServices;
using Basler.Pylon;

namespace Grab_UsingBufferFactory
{

    // A user-provided buffer factory.
    class MyBufferFactory : IBufferFactory
    {
        public MyBufferFactory()
        {
        }

        // This function will be called by pylon.NET when it needs to allocate a buffer to store image data.
        // The bufferSize parameter specifies the size in bytes of the buffer to allocate. On return you must
        // set the output parameters createdPinnedBuffer and createdPinnedObject. Optionally you can set
        // bufferUserData. The bufferUserData can later be used to identify the buffer.
        // In case the allocation fails you must throw an exception to indicate the failure.
        // Note: This function may be called from different threads.

        public void AllocateBuffer( long bufferSize, ref object createdPinnedObject, ref IntPtr createdPinnedBuffer, ref object bufferUserData )
        {
            // Allocate buffer for pixel data.
            // If you already have a buffer allocated by your image processing library you can use this instead.
            // In this case you must modify the delete code (see below) accordingly.
            long elementSize = sizeof( ushort );
            long arraySize = (bufferSize + (elementSize - 1)) / elementSize; //Round up if size does not align

            // The pinned object will receive the actual allocated object (in our case the array).
            // This information can be retrieved from a grab result by calling
            // grabResult.PixelData;
            createdPinnedObject = new ushort [(int)( arraySize )]; // ATTENTION: in .NET array indexes are always int!!!

            // The pinned buffer will receive the pointer to the pinned memory location
            // that will be used for image data aquisition internally.
            GCHandle handle = GCHandle.Alloc( createdPinnedObject, GCHandleType.Pinned );
            createdPinnedBuffer = handle.AddrOfPinnedObject();

            // Here we store the GCHandle in the buffer user data to be able to free the
            // buffer in FreeBuffer.
            bufferUserData = handle;

            Console.WriteLine( "Created buffer {0}.", createdPinnedBuffer );
        }

        // Frees a buffer allocated by a previous call to AllocateBuffer.
        // Warning: This method can be called by different threads.
        public void FreeBuffer( object createdPinnedObject, IntPtr createdPinnedBuffer, object bufferUserData )
        {
            if (null == bufferUserData)
            {
                return;
            }

            // We used the buffer user data to store the buffer handle.
            // Now we use this buffer handle to free/unpin.
            GCHandle handle = (GCHandle)bufferUserData;
            if (null == handle)
            {
                return;
            }

            Console.WriteLine( "Freed buffer {0}.", handle.AddrOfPinnedObject() );

            handle.Free();
        }
    }

    class Grab_UsingBufferFactory
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
                    // Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);

                    // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                    camera.CameraOpened += Configuration.AcquireContinuous;

                    // Open the connection to the camera device.
                    camera.Open();

                    // Set buffer factory before starting the stream grabber because allocation
                    // happens there.
                    MyBufferFactory myFactory = new MyBufferFactory();
                    camera.StreamGrabber.BufferFactory = myFactory;

                    // Start grabbing.
                    camera.StreamGrabber.Start();

                    // Grab a number of images.
                    for (int i = 0; i < 10; ++i)
                    {
                        // Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                        IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
                        using (grabResult)
                        {
                            // Image grabbed successfully?
                            if (grabResult.GrabSucceeded)
                            {
                                // Access the image data.
                                Console.WriteLine("SizeX: {0}", grabResult.Width);
                                Console.WriteLine("SizeY: {0}", grabResult.Height);

                                // Normally we would have a byte array in the pixel data.
                                // However we are using the buffer factory here which allocates
                                // ushort arrays.
                                ushort[] buffer = grabResult.PixelData as ushort[];
                                Console.WriteLine("First value of pixel data: {0}", buffer[0]);
                                Console.WriteLine("");

                                // Display the grabbed image.
                                ImageWindow.DisplayImage( 0, grabResult );
                            }
                            else
                            {
                                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription);
                            }
                        }
                    }

                    // Stop grabbing.
                    camera.StreamGrabber.Stop();

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
