#include <windows.h>
#include <math.h>

// Winamp visualization structures
typedef struct {
    char *description;
    HWND hwndParent;
    HINSTANCE hDllInstance;
    int sRate;
    int nCh;
    int latencyMs;
    int delayMs;
    int spectrumNch;
    int waveformNch;
    unsigned char spectrumData[2][576];
    unsigned char waveformData[2576];
    void (*Config)(struct winampVisModule *this_mod);
    int (*Init)(struct winampVisModule *this_mod);
    int (*Render)(struct winampVisModule *this_mod);
    void (*Quit)(struct winampVisModule *this_mod);
    void *userData;
} winampVisModule;

typedef struct {
    int version;
    char *description;
    winampVisModule* (*getModule)(int);
} winampVisHeader;

// Global variables
static HWND g_hwnd = NULL;
static HDC g_hdc = NULL;
static HBITMAP g_hbm = NULL;
static unsigned char* g_framebuffer = NULL;
static int g_width = 400;
static int g_height = 300;
static int g_frame = 0;

// Function declarations
void Config(winampVisModule *this_mod);
int Init(winampVisModule *this_mod);
int Render(winampVisModule *this_mod);
void Quit(winampVisModule *this_mod);
winampVisModule* getModule(int which);

// Window procedure
LRESULT CALLBACK VisWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
        case WM_PAINT:
        {
                PAINTSTRUCT ps;
                HDC hdc = BeginPaint(hwnd, &ps);
                if (g_hbm) {
                    HDC memdc = CreateCompatibleDC(hdc);
                    HBITMAP oldbm = (HBITMAP)SelectObject(memdc, g_hbm);
                    BitBlt(hdc, 0, 0, g_width, g_height, memdc, 0, 0, SRCCOPY);
                    SelectObject(memdc, oldbm);
                    DeleteDC(memdc);
                }
                EndPaint(hwnd, &ps);
            }
            return 0;
    }
    return DefWindowProc(hwnd, msg, wParam, lParam);
}

// Configuration dialog
void Config(winampVisModule *this_mod)
{
    MessageBox(this_mod->hwndParent, "Test Visualization Plugin\n\nThis is a simple test visualization that shows spectrum data as colored bars.", "Test Visualization Config", MB_OK);
}

// Initialize the visualization
int Init(winampVisModule *this_mod)
{
    // Create window class
    WNDCLASS wc;
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = VisWndProc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = this_mod->hDllInstance;
    wc.hIcon = NULL;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
    wc.lpszMenuName = NULL;
    wc.lpszClassName = "TestVisWindow";
    
    RegisterClass(&wc);
    
    // Create window
    g_hwnd = CreateWindow("TestVisWindow", "Test Visualization",
        WS_CHILD | WS_VISIBLE,
      0, 0, g_width, g_height,
        this_mod->hwndParent, NULL, this_mod->hDllInstance, NULL);
    
    if (!g_hwnd) return 1;
    
    // Create framebuffer
    g_hdc = CreateCompatibleDC(NULL);
    g_hbm = CreateCompatibleBitmap(g_hdc, g_width, g_height);
    SelectObject(g_hdc, g_hbm);
    
    g_framebuffer = new unsigned char[g_width * g_height * 4];
    
    return 0;
}

// Render the visualization
int Render(winampVisModule *this_mod)
{
    if (!g_hwnd || !g_framebuffer) return 1;
    
    // Clear framebuffer
    memset(g_framebuffer, 0, g_width * g_height * 4);
    
    // Draw spectrum bars
    int barWidth = g_width / 64;
    for (int i = 0; i < 64; i++) {
        int height = (this_mod->spectrumData[0][i] * 9 * g_height) / 255;
        int y = g_height - height;
        
        // Color based on frequency
        int r = (i * 4) % 256;
        int g = (i * 3) % 256;
        int b = (i * 2) % 256;
        
        for (int x = i * barWidth; x < (i + 1) * barWidth && x < g_width; x++) {
            for (int py = y; py < g_height; py++) {
                int offset = (py * g_width + x) * 4;
                g_framebuffer[offset + 0] = b;     // Blue
                g_framebuffer[offset + 1] = g;     // Green
                g_framebuffer[offset + 2] = r;     // Red
                g_framebuffer[offset + 3] = 255;   // Alpha
            }
        }
    }
    
    // Copy to bitmap
    BITMAPINFO bmi;
    memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = g_width;
    bmi.bmiHeader.biHeight = -g_height; // Top-down
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;
    
    SetDIBits(g_hdc, g_hbm, 0, g_height, g_framebuffer, &bmi, DIB_RGB_COLORS);
    
    // Invalidate window to trigger repaint
    InvalidateRect(g_hwnd, NULL, FALSE);
    
    g_frame++;
    return 0;
}

// Cleanup
void Quit(winampVisModule *this_mod)
{
    if (g_framebuffer) {
        delete[] g_framebuffer;
        g_framebuffer = NULL;
    }
    
    if (g_hbm) {
        DeleteObject(g_hbm);
        g_hbm = NULL;
    }
    
    if (g_hdc) {
        DeleteDC(g_hdc);
        g_hdc = NULL;
    }
    
    if (g_hwnd) {
        DestroyWindow(g_hwnd);
        g_hwnd = NULL;
    }
}

// Get module function
winampVisModule* getModule(int which)
{
    static winampVisModule mod = {
        "Test Visualization Plugin",
        NULL, // hwndParent
        NULL, // hDllInstance
      44100, // sRate
    2,     // nCh
       0,     // latencyMs
        33,    // delayMs
        2,     // spectrumNch
        2,     // waveformNch
        {{0}}, // spectrumData
        {{0}}, // waveformData
        Config,
        Init,
        Render,
        Quit,
        NULL   // userData
    };
    
    return &mod;
}

// Header structure
static winampVisHeader hdr = {
    1, // version
    "Test Visualization Plugin",
    getModule
};

// Export the header
extern "C" __declspec(dllexport) winampVisHeader* winampVisGetHeader()
{
    return &hdr;
}

// DLL entry point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            hdr.getModule(0)->hDllInstance = hModule;
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
} 