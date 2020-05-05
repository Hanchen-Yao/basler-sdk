# basler-sdk  
BASLER工业相机的SDK案例。基于这些案例，做一定程度的开发。  

1. Overview  
综述  
pylon Camera软件套件包括一个SDK，它有三个API：  
* 用于c++ (Windows, Linux和macOS)的pylon API，  
* 用于C (Windows和Linux)的pylon API，  
* 用于NET语言的pylon API，如c#和VB.NET(Windows)。  
除了API之外，挂架摄像软件套件还包括一组示例程序和文档。  
本手册描述了SDK示例程序。  

2. DeviceRemovalHandling   
设备移除  
这个示例演示了如何检测相机设备的移除。它还向您展示了如何重新连接到删除的设备。  
注意：如果您在调试模式下构建此示例并使用GigE摄像机设备运行它，则pylon将heartbeat超时设置为5分钟。这样做是为了允许调试和单步执行，而不会因为缺少heartbeat而失去相机连接。但是，使用此设置，应用程序需要5分钟才能注意到一个GigE设备已经断开连接。作为一种解决方案，heartbeat设置为1000毫秒。  
