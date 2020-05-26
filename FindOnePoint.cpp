#include <iostream>
#include <fstream>
#include <string>
#include <windows.h>
#include <gdiplus.h>
#pragma comment(lib, "gdiplus.lib")

using namespace std;
using namespace Gdiplus;

int main() {
    GdiplusStartupInput gdiplusstartupinput;
    ULONG_PTR gdiplustoken;
    GdiplusStartup(&gdiplustoken, &gdiplusstartupinput, NULL);

    wstring infilename(L"2.jpg");
    string outfilename("color.txt");

    Bitmap* bmp = new Bitmap(infilename.c_str());
    UINT height = bmp->GetHeight();
    UINT width = bmp->GetWidth();

    Color color;

    for (UINT y = 0; y < height; y++)
        for (UINT x = 0; x < width; x++) {
            bmp->GetPixel(x, y, &color);
            if ((int)color.GetRed() > 200
                && (int)color.GetGreen() > 200
                && (int)color.GetBlue() > 200) {
                cout << x << "," << y << endl;
                //goto EXIT; 
            }
        }
//EXIT:
    delete bmp;
    GdiplusShutdown(gdiplustoken);
    return 0;
}