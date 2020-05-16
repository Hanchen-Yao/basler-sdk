/*
*Pylon+OpenCV实现实时抓取图像和视频
*用OpenCV实现轮廓提取、计算坐标、周长和面积
*作者：hanchen
*时间：2020年5月12日
*/

//定义是否保存图片
#define saveImages 1
//定义是否记录视频
#define recordVideo 0

//加载OpenCV API
#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/video/video.hpp>
#include <opencv2/highgui/highgui_c.h>
#include<opencv2/opencv.hpp>
//加载PYLON API
#include <pylon/PylonIncludes.h>

#include<iostream>
#include<math.h>

#ifdef PYLON_WIN_BUILD
#include <pylon/PylonGUI.h>    
#endif

//命名空间.
using namespace Pylon;
using namespace cv;
using namespace std;
//定义抓取的图像数
static const uint32_t c_countOfImagesToGrab = 20;

void main()
{

    //Pylon自动初始化和终止
    Pylon::PylonAutoInitTerm autoInitTerm;
    try
    {
        //创建相机对象（以最先识别的相机）
        CInstantCamera camera(CTlFactory::GetInstance().CreateFirstDevice());
        // 打印相机的名称
        std::cout << "Using device " << camera.GetDeviceInfo().GetModelName() << endl;
        //获取相机节点映射以获得相机参数
        GenApi::INodeMap& nodemap = camera.GetNodeMap();
        //打开相机
        camera.Open();
        //获取相机成像宽度和高度
        GenApi::CIntegerPtr width = nodemap.GetNode("Width");
        GenApi::CIntegerPtr height = nodemap.GetNode("Height");

        //设置相机最大缓冲区,默认为10
        camera.MaxNumBuffer = 5;
        //新建pylon ImageFormatConverter对象.
        CImageFormatConverter formatConverter;
        //确定输出像素格式
        formatConverter.OutputPixelFormat = PixelType_BGR8packed;
        //创建一个Pylonlmage后续将用来创建OpenCV images
        CPylonImage pylonImage;

        //声明一个整形变量用来计数抓取的图像，以及创建文件名索引
        int grabbedlmages = 0;

        //新建一个OpenCV video creator对象.
        VideoWriter cvVideoCreator;
        //新建一个OpenCV image对象.
        Mat openCvImage;
        //视频文件名
        std::string videoFileName = "openCvVideo.avi";
        //定义视频帧大小
        cv::Size frameSize = Size((int)width->GetValue(), (int)height->GetValue());

        //设置视频编码类型和帧率，有三种选择
        //帧率必须小于等于相机成像帧率
        cvVideoCreator.open(videoFileName, CAP_OPENCV_MJPEG, 10, frameSize, true);
        //cvVideoCreator.open(videoFileName, CV_F0URCC('M','P',,4','2’), 20, frameSize, true);
        //cvVideoCreator.open(videoFileName, CV_FOURCC('M', '3', 'P', 'G'), 20, frameSize, true);

        //开始抓取c_countOfImagesToGrab images.
        //相机默认设置连续抓取模式
        camera.StartGrabbing(c_countOfImagesToGrab, GrabStrategy_LatestImageOnly);

        //抓取结果数据指针
        CGrabResultPtr ptrGrabResult;

        //当c_countOfImagesToGrab images获取恢复成功时，Camera.StopGrabbing() 
        //被RetrieveResult()方法自动调用停止抓取

        while (camera.IsGrabbing())

        {
            //等待接收和恢复图像，超时时间设置为5000 ms.
            camera.RetrieveResult(5000, ptrGrabResult, TimeoutHandling_ThrowException);

            //如果图像抓取成功
            if (ptrGrabResult->GrabSucceeded())
            {
                //获取图像数据
                cout << "SizeX: " << ptrGrabResult->GetWidth() << endl;
                cout << "SizeY: " << ptrGrabResult->GetHeight() << endl;

                //将抓取的缓冲数据转化成pylon image.
                formatConverter.Convert(pylonImage, ptrGrabResult);

                // 将pylon image转成OpenCV image.
                openCvImage = cv::Mat(ptrGrabResult->GetHeight(), ptrGrabResult->GetWidth(), CV_8UC3, (uint8_t*)pylonImage.GetBuffer());



                //对OpenCV image进行处理
                //高斯模糊
                //Mat openCvImage;
                Mat GaussImg;
                GaussianBlur(openCvImage, GaussImg, Size(7, 7), 0, 0);
                //imshow("Gauss Image", GaussImg);
                //imwrite("D:/images/test0510/Gauss Image.jpg", GaussImg);
                cvtColor(GaussImg, GaussImg, CV_BGR2GRAY);

                //二值化操作
                Mat binary;
                threshold(GaussImg, binary, 0, 255, THRESH_BINARY | THRESH_TRIANGLE);
                //imshow("binary Image", binary);
                //imwrite("D:/images/test0510/binary Image.jpg", binary);

                //形态学操作
                Mat morphImg;
                Mat kernel = getStructuringElement(MORPH_RECT, Size(5, 5), Point(-1, -1));
                morphologyEx(binary, morphImg, MORPH_CLOSE, kernel, Point(-1, -1), 1);
                //imshow("morph Image", morphImg);
                //imwrite("D:/images/test0510/morph Image.jpg", morphImg);

                //轮廓发现
                Mat contoursImg = Mat::zeros(openCvImage.size(), CV_8UC3);
                vector<vector<Point>>contours;
                vector<Vec4i>hireachy;
                findContours(morphImg, contours, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE, Point(-1, -1));

                for (size_t i = 0; i < contours.size(); i++)
                {
                    Rect rect = boundingRect(contours[i]);
                    if (rect.width < openCvImage.cols / 2)
                        continue;

                    drawContours(contoursImg, contours, static_cast<int>(i),
                        Scalar(0, 0, 255), 2, 8, hireachy, 0, Point(0, 0));
                    
                    //计算面积与周长
                    float area = contourArea(contours[i]);
                    float length = arcLength(contours[i], true);
                    printf("对象图像面积为:%f\n", area);
                    printf("对象图像周长为:%f\n", length);
                    //输出轮廓坐标
                    std::cout << ("轮廓的坐标为：%f\n", contours[0]) << endl;
                }
                //imwrite("D:/images/test0510/contours Image.jpg", contoursImg);
                




                //如果需要保存图片
                if (saveImages)
                {
                    std::ostringstream s;
                    // 按索引定义文件名存储图片
                    s << "image_" << grabbedlmages << ".jpg";
                    std::string imageName(s.str());
                    //保存OpenCV image.
                    imwrite(imageName, openCvImage);
                    grabbedlmages++;
                }

                //如果需要记录视频
                if (recordVideo)
                {

                    cvVideoCreator.write(openCvImage);
                }

                //新建OpenCV display window
                namedWindow("OpenCV Display Window", CV_WINDOW_NORMAL); // other options: CV_AUTOSIZE, CV_FREERATIO
                //显示及时影像
                imshow("OpenCV Display Window", openCvImage);
                imshow("OpenCV Contours Window", contoursImg);

                // Define a timeout for customer's input in
                // '0' means indefinite, i.e. the next image will be displayed after closing the window.
                // '1' means live stream
                waitKey(1);//这里必须是1。0则需要单击关闭窗口按钮才能采集下一个图像

            }

        }

    }
    catch (GenICam::GenericException& e)
    {
        // Error handling.
        cerr << "An exception occurred." << endl
            << e.GetDescription() << endl;
    }
    return;
}
