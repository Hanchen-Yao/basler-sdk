# basler-sdk  
BASLER工业相机的SDK案例。基于这些案例，做一定程度的开发。  

1. Overview  
综述  
pylon Camera软件套件包括一个SDK，它有三个API：  
* 用于C++ (Windows, Linux和macOS)的pylon API，  
* 用于C (Windows和Linux)的pylon API，  
* 用于NET语言的pylon API，如c#和VB.NET(Windows)。  
除了API之外，挂架摄像软件套件还包括一组示例程序和文档。本手册描述了SDK示例程序。  

2. DeviceRemovalHandling.cpp   
设备移除  
这个示例演示了如何检测相机设备的移除。它还向您展示了如何重新连接到删除的设备。  
注意：如果您在调试模式下构建此示例并使用GigE摄像机设备运行它，则pylon将heartbeat超时设置为5分钟。这样做是为了允许调试和单步执行，而不会因为缺少heartbeat而失去相机连接。但是，使用此设置，应用程序需要5分钟才能注意到一个GigE设备已经断开连接。作为一种解决方案，heartbeat设置为1000毫秒。  

3. Grab.cpp  
抓取图像  
这个示例演示了如何使用CInstantCamera类获取和处理图像。  
图像是异步获取和处理的。在应用程序处理缓冲区的同时，将获取下一个缓冲区。  
CInstantCamera类使用一个缓冲池来从摄像机设备检索图像数据。一旦缓冲区被填满并准备就绪，就可以从相机对象中检索缓冲区进行处理。在抓取结果中收集缓冲区和其他图像数据。抓取结果在检索后由智能指针持有。当显式释放或销毁智能指针对象时，缓冲区将自动重用。  

4. Grab_CameraEvents.cpp  
抓取_相机事件  
Basler USB3 Vision和GigE Vision摄像机可以发送事件消息。例如，当传感器曝光完成时，摄像机可以向计算机发送曝光结束事件。在完成曝光后的图像数据完全传输之前，计算机就可以接收到该事件。这个示例演示了如何在接收到相机事件消息数据时得到通知。  
事件消息由InstantCamera类自动检索和处理。事件消息携带的信息作为相机节点映射中的参数节点公开，可以像标准相机参数一样访问。当接收到摄像机事件时，将更新这些节点。可以注册在接收到事件数据时触发的相机事件处理程序对象。  
这些机制说明了曝光结束和超限事件。  
曝光结束事件包含以下信息：  
* ExposureEndEventFrameID：已曝光图像的数量。  
* ExposureEndEventTimestamp：事件生成的时间。  
* ExposureEndEventStreamChannelIndex:用于传输图像的图像数据流的数量。在Basler相机上，这个参数总是设置为0。  
事件溢出事件（The Event Overrun event）由摄像机发送，作为事件正在被删除的警告。该通知不包含关于删除了多少事件或哪些事件的具体信息。如果事件以较高的频率生成，并且没有足够的带宽发送事件，则可能会删除事件。  
这个示例还向您展示了如何注册事件处理程序，该事件处理程序指示摄像机发送的事件的到达。出于演示目的，为同一个事件注册了不同的处理程序。  
注意:不同的相机系列实现不同版本的标准功能命名约定(Standard Feature Naming  Convention，SFNC)。这就是为什么使用的参数的名称和类型可以不同。  
