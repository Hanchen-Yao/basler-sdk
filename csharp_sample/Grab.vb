'This sample illustrates how to grab images and process images asynchronously.
'This means that while the application is processing a buffer,
'the acquisition of the next buffer is done in parallel.
'The sample uses a pool of buffers. The buffers are automatically allocated. Once a buffer is filled
'and ready for processing, the buffer is retrieved from the stream grabber as part of a grab
'result. The grab result is processed and the buffer is passed back to the stream grabber by
'disposing the grab result. The buffer is reused and refilled.
'A buffer retrieved from the stream grabber as a grab result is not overwritten in the background
'as long as the grab result is not disposed.


Imports Basler.Pylon

' Bring extension methods of the pylon API into scope.
Imports Basler.Pylon.IIntegerParameterExtensions
Imports Basler.Pylon.IBooleanParameterExtensions
Imports Basler.Pylon.ICommandParameterExtensions
Imports Basler.Pylon.IEnumParameterExtensions
Imports Basler.Pylon.IFloatParameterExtensions
Imports Basler.Pylon.IImageExtensions
Imports Basler.Pylon.IStringParameterExtensions

Module Grab
    Public Class Grab

        Shared Sub Main()

            ' The exit code of the sample application.
            Dim exitCode As Integer = 0

            Try

                ' Create a camera object that selects the first camera device found.
                ' More constructors are available for selecting a specific camera device.
                Using camera As New Camera()
                    ' Print the model name of the camera.
                    Console.WriteLine("Using camera {0}.", camera.CameraInfo(CameraInfoKey.ModelName))

                    ' Set the acquisition mode to free running continuous acquisition when the camera is opened.
                    AddHandler camera.CameraOpened, AddressOf Configuration.AcquireContinuous

                    ' Open the connection to the camera device.
                    camera.Open()

                    ' The parameter MaxNumBuffer can be used to control the amount of buffers
                    ' allocated for grabbing. The default value of this parameter is 10.
                    camera.Parameters(PLCameraInstance.MaxNumBuffer).SetValue(5)

                    ' Start grabbing.
                    camera.StreamGrabber.Start()

                    ' Grab a number of images.
                    Dim i As Integer = 0
                    For i = 1 To 10
                        ' Wait for an image and then retrieve it. A timeout of 5000 ms is used.
                        Dim grabResult As IGrabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException)
                        Using grabResult
                            ' Image grabbed successfully?
                            If grabResult.GrabSucceeded Then
                                ' Access the image data.
                                Console.WriteLine("SizeX: {0}", grabResult.Width)
                                Console.WriteLine("SizeY: {0}", grabResult.Height)
                                Dim buffer As Byte() = TryCast(grabResult.PixelData, Byte())
                                Console.WriteLine("Gray value of first pixel: {0}", buffer(0))
                                Console.WriteLine("")

                                ' Display the grabbed image.
                                ImageWindow.DisplayImage(0, grabResult)
                            Else
                                Console.WriteLine("Error: {0} {1}", grabResult.ErrorCode, grabResult.ErrorDescription)
                            End If
                        End Using
                    Next

                    ' Stop grabbing.
                    camera.StreamGrabber.Stop()

                    ' Close the connection to the camera device.
                    camera.Close()
                End Using

            Catch e As Exception
                Console.Error.WriteLine("Exception: {0}", e.Message)
                exitCode = 1

            Finally
                ' Comment the following two lines to disable waiting on exit.
                Console.Error.WriteLine("Press enter to exit.")
                Console.ReadLine()

            End Try

            Environment.Exit(exitCode)

        End Sub

    End Class
End Module
