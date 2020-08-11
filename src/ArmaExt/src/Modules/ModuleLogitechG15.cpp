#include "ModuleLogitechG15.hpp"
#include <filesystem>
#include <Windows.h>

#include "Game/GameManager.hpp"
#include "RadioModule.hpp"


#define LOGI_LCD_TYPE_MONO    (0x00000001)

#define LOGI_LCD_MONO_BUTTON_0 (0x00000001)
#define LOGI_LCD_MONO_BUTTON_1 (0x00000002)
#define LOGI_LCD_MONO_BUTTON_2 (0x00000004)
#define LOGI_LCD_MONO_BUTTON_3 (0x00000008)

const int LOGI_LCD_MONO_WIDTH = 160;
const int LOGI_LCD_MONO_HEIGHT = 43;


static bool (*LogiLcdInit)(wchar_t* friendlyName, int lcdType);
static bool (*LogiLcdIsConnected)(int lcdType);
static bool (*LogiLcdIsButtonPressed)(int button);
static void (*LogiLcdUpdate)();
static void (*LogiLcdShutdown)();

// Monochrome LCD functions
static bool (*LogiLcdMonoSetBackground)(BYTE monoBitmap[]);
static bool (*LogiLcdMonoSetText)(int lineNumber, wchar_t* text);






void ModuleLogitechG15::Run() {
    bool isTransmitting = false;
    while (shouldRun) {
        LogiLcdUpdate();


        LogiLcdMonoSetText(3, L"Radio 1");

        if (LogiLcdIsButtonPressed(LOGI_LCD_MONO_BUTTON_0) != isTransmitting) {
            isTransmitting = LogiLcdIsButtonPressed(LOGI_LCD_MONO_BUTTON_0);
            //GRadioModule.DoRadioTransmit(isTransmitting);
        }


        std::this_thread::sleep_for(16ms);
    }
}


void ModuleLogitechG15::ModuleInit() {

    char path[MAX_PATH];
    HMODULE hm = NULL;

    if (GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
        GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        (LPCSTR)&RVExtension, &hm) == 0)
    {
        int ret = GetLastError();
        fprintf(stderr, "GetModuleHandle failed, error = %d\n", ret);
        // Return or however you want to handle an error.
    }
    if (GetModuleFileName(hm, path, sizeof(path)) == 0)
    {
        int ret = GetLastError();
        fprintf(stderr, "GetModuleFileName failed, error = %d\n", ret);
        // Return or however you want to handle an error.
    }




    std::string dllPath = (std::filesystem::path(path).parent_path() / "LogitechLcd.dll").lexically_normal().string();

    auto logiDLL = LoadLibraryExA(dllPath.c_str(), nullptr, 0);

    if (!logiDLL) return;

    LogiLcdInit = reinterpret_cast<decltype(LogiLcdInit)>(GetProcAddress(logiDLL, "LogiLcdInit"));
    LogiLcdIsConnected = reinterpret_cast<decltype(LogiLcdIsConnected)>(GetProcAddress(logiDLL, "LogiLcdIsConnected"));
    LogiLcdIsButtonPressed = reinterpret_cast<decltype(LogiLcdIsButtonPressed)>(GetProcAddress(logiDLL, "LogiLcdIsButtonPressed"));
    LogiLcdUpdate = reinterpret_cast<decltype(LogiLcdUpdate)>(GetProcAddress(logiDLL, "LogiLcdUpdate"));
    LogiLcdShutdown = reinterpret_cast<decltype(LogiLcdShutdown)>(GetProcAddress(logiDLL, "LogiLcdShutdown"));
    LogiLcdMonoSetBackground = reinterpret_cast<decltype(LogiLcdMonoSetBackground)>(GetProcAddress(logiDLL, "LogiLcdMonoSetBackground"));
    LogiLcdMonoSetText = reinterpret_cast<decltype(LogiLcdMonoSetText)>(GetProcAddress(logiDLL, "LogiLcdMonoSetText"));

    LogiLcdInit(L"TacControl", LOGI_LCD_TYPE_MONO);

    if (!LogiLcdIsConnected(LOGI_LCD_TYPE_MONO)) return;


    Thread::ModuleInit();

}
