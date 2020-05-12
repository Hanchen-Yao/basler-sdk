'For camera configuration and for accessing other parameters, the pylon API
'uses the technologies defined by the GenICam standard hosted by the
'European Machine Vision Association (EMVA). The GenICam specification
'(http://www.GenICam.org) defines a format for camera description files.
'These files describe the configuration interface of GenICam compliant cameras.
'The description files are written in XML (eXtensible Markup Language) and
'describe camera registers, their interdependencies, and all other
'information needed to access high-level features. This includes features such as Gain,
'Exposure Time, or Pixel Format. The features are accessed by means of low level
'register read and write operations.

'The elements of a camera description file are represented as parameter objects.
'For example, a parameter object can represent a single camera
'register, a camera parameter such as Gain, or a set of available parameter
'values.



Imports Basler.Pylon

' Bring extension methods of the pylon API into scope.
Imports Basler.Pylon.IIntegerParameterExtensions
Imports Basler.Pylon.IBooleanParameterExtensions
Imports Basler.Pylon.ICommandParameterExtensions
Imports Basler.Pylon.IEnumParameterExtensions
Imports Basler.Pylon.IFloatParameterExtensions
Imports Basler.Pylon.IImageExtensions
Imports Basler.Pylon.IStringParameterExtensions


Public Class ParameterizeCamera

    Shared Sfnc2_0_0 As New Version(2, 0, 0)

    Shared Sub Main()
        ' The exit code of the sample application.
        Dim exitCode As Integer = 0

        Try
            ' Create a camera object that selects the first camera device found.
            ' More constructors are available for selecting a specific camera device.
            Using camera As New Camera()
                '************************************************************************
                '* Accessing camera parameters                                          *
                '************************************************************************


                ' Before accessing camera device parameters the camera must be opened.
                camera.Open()

                ' Parameters are accessed using parameter lists. Parameter lists contain a set of parameter names
                ' analogous to enumerations of a programming language. Here the parameter list PLCamera is used.
                ' PLCamera contains a list of parameter names of all camera device types. Additional device-specific
                ' parameter lists are available, e.g. PLUsbCamera for USB camera devices.


                ' DeviceVendorName, DeviceModelName, and DeviceFirmwareVersion are string parameters.
                Console.WriteLine("Camera Device Information")
                Console.WriteLine("=========================")
                Console.WriteLine("Vendor           : {0}", camera.Parameters(PLCamera.DeviceVendorName).GetValue())
                Console.WriteLine("Model            : {0}", camera.Parameters(PLCamera.DeviceModelName).GetValue())
                Console.WriteLine("Firmware version : {0}", camera.Parameters(PLCamera.DeviceFirmwareVersion).GetValue())
                Console.WriteLine("")
                Console.WriteLine("Camera Device Settings")
                Console.WriteLine("======================")


                ' Setting the AOI. OffsetX, OffsetY, Width, and Height are integer parameters.
                ' On some cameras, the offsets are read-only. If they are writable, set the offsets to min.
                camera.Parameters(PLCamera.OffsetX).TrySetToMinimum()
                camera.Parameters(PLCamera.OffsetY).TrySetToMinimum()
                ' Some parameters have restrictions. You can use GetIncrement/GetMinimum/GetMaximum to make sure you set a valid value.
                ' Here, we let pylon correct the value if needed.
                camera.Parameters(PLCamera.Width).SetValue(202, IntegerValueCorrection.Nearest)
                camera.Parameters(PLCamera.Height).SetValue(101, IntegerValueCorrection.Nearest)

                Console.WriteLine("OffsetX          : {0}", camera.Parameters(PLCamera.OffsetX).GetValue())
                Console.WriteLine("OffsetY          : {0}", camera.Parameters(PLCamera.OffsetY).GetValue())
                Console.WriteLine("Width            : {0}", camera.Parameters(PLCamera.Width).GetValue())
                Console.WriteLine("Height           : {0}", camera.Parameters(PLCamera.Height).GetValue())


                ' Set an enum parameter.
                Dim oldPixelFormat As String = camera.Parameters(PLCamera.PixelFormat).GetValue()
                ' Remember the current pixel format.
                Console.WriteLine("Old PixelFormat  : {0} ({1})", camera.Parameters(PLCamera.PixelFormat).GetValue(), oldPixelFormat)

                ' Set pixel format to Mono8 if available.
                If camera.Parameters(PLCamera.PixelFormat).TrySetValue(PLCamera.PixelFormat.Mono8) Then
                    Console.WriteLine("New PixelFormat  : {0} ({1})", camera.Parameters(PLCamera.PixelFormat).GetValue(), oldPixelFormat)
                End If

                ' Some camera models may have auto functions enabled. To set the gain value to a specific value,
                ' the Gain Auto function must be disabled first (if gain auto is available).
                camera.Parameters(PLCamera.GainAuto).TrySetValue(PLCamera.GainAuto.Off) ' Set GainAuto to Off if it is writable.


                ' Features, e.g. 'Gain', are named according to the GenICam Standard Feature Naming Convention (SFNC).
                ' The SFNC defines a common set of features, their behavior, and the related parameter names.
                ' This ensures the interoperability of cameras from different camera vendors.
                ' Cameras compliant with the USB3 Vision standard are based on the SFNC version 2.0.
                ' Basler GigE and Firewire cameras are based on previous SFNC versions.
                ' Accordingly, the behavior of these cameras and some parameters names will be different.
                ' The SFNC version can be used to handle differences between camera device models.
                If camera.GetSfncVersion() < Sfnc2_0_0 Then
                    ' In previous SFNC versions, GainRaw is an integer parameter.
                    camera.Parameters(PLCamera.GainRaw).SetValuePercentOfRange(50)
                    ' GammaEnable is a boolean parameter.
                    camera.Parameters(PLCamera.GammaEnable).TrySetValue(True)
                Else
                    ' For SFNC 2.0 cameras, e.g. USB3 Vision cameras
                    ' In SFNC 2.0, Gain is a float parameter.
                    ' For USB cameras, Gamma is always enabled.
                    camera.Parameters(PLUsbCamera.Gain).SetValuePercentOfRange(50)
                End If


                '************************************************************************
                '* Parameter access status                                              *
                '************************************************************************


                ' Each parameter is either readable or writable or both.
                ' Depending on the camera's state, a parameter may temporarily not be readable or writable.
                ' For example, a parameter related to external triggering may not be available when the camera is in free run mode.
                ' Additionally, parameters can be read-only by default.
                Console.WriteLine("OffsetX readable        : {0}", camera.Parameters(PLCamera.OffsetX).IsReadable)
                Console.WriteLine("TriggerSoftware writable: {0}", camera.Parameters(PLCamera.TriggerSoftware).IsWritable)


                '************************************************************************
                '* Empty parameters                                                     *
                '************************************************************************


                ' Camera models have different parameter sets available. For example, GammaEnable is not part of USB camera device
                ' parameters. If a requested parameter does not exist, an empty parameter object will be returned to simplify handling.
                ' Therefore, an additional existence check is not necessary.
                ' An empty parameter is never readable or writable.
                Console.WriteLine("GammaEnable writable    : {0}", camera.Parameters(PLCamera.GammaEnable).IsWritable)
                Console.WriteLine("GammaEnable readable    : {0}", camera.Parameters(PLCamera.GammaEnable).IsReadable)
                Console.WriteLine("GammaEnable empty       : {0}", camera.Parameters(PLCamera.GammaEnable).IsEmpty)


                '************************************************************************
                '* Try or GetValueOrDefaultmethods                                      *
                '************************************************************************


                ' Several parameters provide Try or GetValueOrDefault methods. These methods are provided because
                ' a parameter may not always be available, either because the camera device model does not support the parameter
                ' or because the parameter is temporarily disabled (due to other parameter settings).
                ' If the GammaEnable parameter is writable, enable it.
                camera.Parameters(PLCamera.GammaEnable).TrySetValue(True)
                ' Toggle CenterX to change the availability of OffsetX.
                ' If CenterX is readable, get the value. Otherwise, return false.
                Dim centerXValue As Boolean = camera.Parameters(PLCamera.CenterX).GetValueOrDefault(False)
                Console.WriteLine("CenterX                 : {0}", centerXValue)
                Console.WriteLine("OffsetX writable        : {0}", camera.Parameters(PLCamera.OffsetX).IsWritable)
                ' Toggle CenterX if CenterX is writable.
                camera.Parameters(PLCamera.CenterX).TrySetValue(Not centerXValue)
                Console.WriteLine("CenterX                 : {0}", camera.Parameters(PLCamera.CenterX).GetValueOrDefault(False))
                Console.WriteLine("OffsetX writable        : {0}", camera.Parameters(PLCamera.OffsetX).IsWritable)
                ' Restore the value of CenterX if CenterX is writable.
                camera.Parameters(PLCamera.CenterX).TrySetValue(centerXValue)
                ' Important: The Try and the GetValueOrDefault methods are usually related to the access status (IsWritable or IsReadable) of a parameter.
                ' For more information, check the summary of the methods.

                ' There are additional methods available that provide support for setting valid values.
                ' Set the width and correct the value to the nearest valid increment.
                camera.Parameters(PLCamera.Width).SetValue(202, IntegerValueCorrection.Nearest)
                ' Set the width and correct the value to the nearest valid increment if width is readable and writable
                camera.Parameters(PLCamera.Width).TrySetValue(202, IntegerValueCorrection.Nearest)
                ' One of the following pixel formats should be available:
                Dim pixelFormats As String() = New String() {PLCamera.PixelFormat.BayerBG8, PLCamera.PixelFormat.BayerRG8, PLCamera.PixelFormat.BayerGR8, PLCamera.PixelFormat.BayerGB8, PLCamera.PixelFormat.Mono8}
                'Set the first valid pixel format in the list.
                camera.Parameters(PLCamera.PixelFormat).SetValue(pixelFormats)
                'Set the first valid pixel format in the list if PixelFormat is writable.
                camera.Parameters(PLCamera.PixelFormat).TrySetValue(pixelFormats)
                Console.WriteLine("New PixelFormat  : {0}", camera.Parameters(PLCamera.PixelFormat).GetValue())


                '************************************************************************
                '* Optional: Accessing camera parameters without using a parameter list *
                '************************************************************************


                ' Accessing parameters without using a parameter list can be necessary in rare cases,
                ' e.g. if you want to set newly added camera parameters that are not added to a parameter list yet.
                ' It is recommended to use parameter lists if possible to avoid using the wrong parameter type and
                ' to avoid spelling errors.

                ' When accessing parameters, the name and the type must usually be known beforehand.
                ' The following syntax can be used to access any camera device parameter.
                ' Adjust the parameter name ("BrandNewFeature") and the parameter type (IntegerName, EnumName, FloatName, etc.)
                ' according to the parameter that you want to access.
                camera.Parameters(CType("BrandNewFeature", IntegerName)).TrySetToMaximum()

                ' This is another alternative to access a parameter without using a parameter list
                ' shown for completeness only
                Dim brandNewFeature As IIntegerParameter = TryCast(camera.Parameters("BrandNewFeature"), IIntegerParameter)
                ' brandNewFeature will be Nothing if it is not present because it cannot be casted to IIntegerParameter
                If brandNewFeature IsNot Nothing Then
                    brandNewFeature.TrySetToMaximum()
                End If

                ' TrySetToMaximum is called for demonstration purposes only.
                ' Enumeration values are plain strings.
                ' Similar to the example above, the pixel format is set to Mono8, this time without using a parameter list.
                If camera.Parameters(CType("PixelFormat", EnumName)).TrySetValue("Mono8") Then
                    Console.WriteLine("New PixelFormat  : {0}", camera.Parameters(CType("PixelFormat", EnumName)).GetValue())
                End If

                ' Restore the old pixel format.
                camera.Parameters(PLCamera.PixelFormat).SetValue(oldPixelFormat)

                ' Close the camera.
                camera.Close()
            End Using
        Catch e As Exception
            Console.[Error].WriteLine("Exception: {0}", e.Message)
            exitCode = 1
        End Try

        ' Comment the following two lines to disable waiting on exit.
        Console.[Error].WriteLine(vbLf & "Press enter to exit.")
        Console.ReadLine()

        Environment.[Exit](exitCode)
    End Sub
End Class
