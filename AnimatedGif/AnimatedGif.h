// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the ANIMATEDGIF_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// ANIMATEDGIF_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef ANIMATEDGIF_EXPORTS
#define ANIMATEDGIF_API __declspec(dllexport)
#else
#define ANIMATEDGIF_API __declspec(dllimport)
#endif


// Reads the animated gif metadata and returns it in a JSON string format.
extern "C" {

    enum DISPOSAL_METHODS
    {
        DM_UNDEFINED = 0,
        DM_NONE = 1,
        DM_BACKGROUND = 2,
        DM_PREVIOUS = 3
    };

    struct ColorF {
        float r;
        float g;
        float b;
        float a;
    };

    struct RectF
    {
        FLOAT left;
        FLOAT top;
        FLOAT right;
        FLOAT bottom;

    };

    struct AnimatedGifMetadata
    {
    public:
        ColorF backgroundColor;
        int numFrames;
        UINT totalLoopCount;
        UINT cxGifImage = 0;
        UINT cyGifImage = 0;
        UINT cxGifImagePixel = 0;  // Width of the displayed image in pixel calculated using pixel aspect ratio
        UINT cyGifImagePixel = 0;  // Height of the displayed image in pixel calculated using pixel aspect ratio
        BOOL hasLoop = FALSE;      // Whether the gif has a loop
        void* handle = NULL;
    };

    struct AnimatedGifFrameMetadata
    {
        UINT   frameDelay = 0;
        RectF  framePosition = { 0 };
        DISPOSAL_METHODS   frameDisposal = DM_NONE;
    };

    ANIMATEDGIF_API int OpenAnimatedGif(WCHAR* pszFileName, /* out */ AnimatedGifMetadata* gif);
    ANIMATEDGIF_API int ReadFrameMetadata(AnimatedGifMetadata* gif, UINT frameIndex, /* out */ AnimatedGifFrameMetadata* frame);
    ANIMATEDGIF_API void ReleaseAnimatedGif(AnimatedGifMetadata* gif);
}
