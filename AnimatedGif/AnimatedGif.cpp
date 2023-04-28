// AnimatedGif.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "AnimatedGif.h"

// Utility inline functions

template <typename T>
inline void SafeRelease(T*& pI)
{
    if (NULL != pI)
    {
        pI->Release();
        pI = NULL;
    }
}

inline LONG RectWidth(RECT rc)
{
    return rc.right - rc.left;
}

inline LONG RectHeight(RECT rc)
{
    return rc.bottom - rc.top;
}

class AnimatedGif
{
public:
    IWICImagingFactory* m_pIWICFactory = NULL;
    IWICBitmapDecoder* m_pDecoder = NULL;

    D2D1_COLOR_F m_backgroundColor = { 0 };
    UINT    m_uTotalLoopCount = 0;  // The number of loops for which the animation will be played
    UINT    m_uLoopNumber = 0;      // The current animation loop number (e.g. 1 when the animation is first played)
    UINT    m_uNextFrameIndex = 0;
    UINT    m_cxGifImage = 0;
    UINT    m_cyGifImage = 0;
    UINT    m_cxGifImagePixel = 0;  // Width of the displayed image in pixel calculated using pixel aspect ratio
    UINT    m_cyGifImagePixel = 0;  // Height of the displayed image in pixel calculated using pixel aspect ratio
    BOOL    m_fHasLoop = FALSE;         // Whether the gif has a loop
    UINT    m_cFrames = 0;
    UINT    m_uFrameDisposal = DM_NONE;  // No previous frame, use disposal none
    UINT    m_uFrameDelay = 0;
    D2D1_RECT_F m_framePosition = { 0 };

    const FLOAT DEFAULT_DPI = 96.f;   // Default DPI that maps image resolution directly to screen resoltuion

    AnimatedGif() {
    }

    ~AnimatedGif() {
        SafeRelease(m_pIWICFactory);
        SafeRelease(m_pDecoder);
    }

    int GetBackgroundColor(IWICMetadataQueryReader* pMetadataQueryReader)
    {
        DWORD dwBGColor;
        BYTE backgroundIndex = 0;
        WICColor rgColors[256];
        UINT cColorsCopied = 0;
        PROPVARIANT propVariant;
        PropVariantInit(&propVariant);
        IWICPalette* pWicPalette = NULL;

        // If we have a global palette, get the palette and background color
        HRESULT hr = pMetadataQueryReader->GetMetadataByName(
            L"/logscrdesc/GlobalColorTableFlag",
            &propVariant);
        if (SUCCEEDED(hr))
        {
            hr = (propVariant.vt != VT_BOOL || !propVariant.boolVal) ? E_FAIL : S_OK;
            PropVariantClear(&propVariant);
        }

        if (SUCCEEDED(hr))
        {
            // Background color index
            hr = pMetadataQueryReader->GetMetadataByName(
                L"/logscrdesc/BackgroundColorIndex",
                &propVariant);
            if (SUCCEEDED(hr))
            {
                hr = (propVariant.vt != VT_UI1) ? E_FAIL : S_OK;
                if (SUCCEEDED(hr))
                {
                    backgroundIndex = propVariant.bVal;
                }
                PropVariantClear(&propVariant);
            }
        }

        // Get the color from the palette
        if (SUCCEEDED(hr))
        {
            hr = m_pIWICFactory->CreatePalette(&pWicPalette);
        }

        if (SUCCEEDED(hr))
        {
            // Get the global palette
            hr = m_pDecoder->CopyPalette(pWicPalette);
        }

        if (SUCCEEDED(hr))
        {
            hr = pWicPalette->GetColors(
                ARRAYSIZE(rgColors),
                rgColors,
                &cColorsCopied);
        }

        if (SUCCEEDED(hr))
        {
            // Check whether background color is outside range 
            hr = (backgroundIndex >= cColorsCopied) ? E_FAIL : S_OK;
        }

        if (SUCCEEDED(hr))
        {
            // Get the color in ARGB format
            dwBGColor = rgColors[backgroundIndex];

            // The background color is in ARGB format, and we want to 
            // extract the alpha value and convert it in FLOAT
            FLOAT alpha = (dwBGColor >> 24) / 255.f;
            m_backgroundColor = D2D1::ColorF(dwBGColor, alpha);
        }

        SafeRelease(pWicPalette);
        return hr;
    }


    int GetGlobalMetadata()
    {
        PROPVARIANT propValue;
        PropVariantInit(&propValue);
        IWICMetadataQueryReader* pMetadataQueryReader = NULL;

        // Get the frame count
        HRESULT hr = m_pDecoder->GetFrameCount(&m_cFrames);
        if (SUCCEEDED(hr))
        {
            // Create a MetadataQueryReader from the decoder
            hr = m_pDecoder->GetMetadataQueryReader(
                &pMetadataQueryReader);
        }

        if (SUCCEEDED(hr))
        {
            // Get background color
            if (FAILED(GetBackgroundColor(pMetadataQueryReader)))
            {
                // Default to transparent if failed to get the color
                m_backgroundColor = D2D1::ColorF(0, 0.f);
            }
        }

        // Get global frame size
        if (SUCCEEDED(hr))
        {
            // Get width
            hr = pMetadataQueryReader->GetMetadataByName(
                L"/logscrdesc/Width",
                &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_cxGifImage = propValue.uiVal;
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            // Get height
            hr = pMetadataQueryReader->GetMetadataByName(
                L"/logscrdesc/Height",
                &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_cyGifImage = propValue.uiVal;
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            // Get pixel aspect ratio
            hr = pMetadataQueryReader->GetMetadataByName(
                L"/logscrdesc/PixelAspectRatio",
                &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI1 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    UINT uPixelAspRatio = propValue.bVal;

                    if (uPixelAspRatio != 0)
                    {
                        // Need to calculate the ratio. The value in uPixelAspRatio 
                        // allows specifying widest pixel 4:1 to the tallest pixel of 
                        // 1:4 in increments of 1/64th
                        FLOAT pixelAspRatio = (uPixelAspRatio + 15.f) / 64.f;

                        // Calculate the image width and height in pixel based on the
                        // pixel aspect ratio. Only shrink the image.
                        if (pixelAspRatio > 1.f)
                        {
                            m_cxGifImagePixel = m_cxGifImage;
                            m_cyGifImagePixel = static_cast<UINT>(m_cyGifImage / pixelAspRatio);
                        }
                        else
                        {
                            m_cxGifImagePixel = static_cast<UINT>(m_cxGifImage * pixelAspRatio);
                            m_cyGifImagePixel = m_cyGifImage;
                        }
                    }
                    else
                    {
                        // The value is 0, so its ratio is 1
                        m_cxGifImagePixel = m_cxGifImage;
                        m_cyGifImagePixel = m_cyGifImage;
                    }
                }
                PropVariantClear(&propValue);
            }
        }

        // Get looping information
        if (SUCCEEDED(hr))
        {
            // First check to see if the application block in the Application Extension
            // contains "NETSCAPE2.0" and "ANIMEXTS1.0", which indicates the gif animation
            // has looping information associated with it.
            // 
            // If we fail to get the looping information, loop the animation infinitely.
            if (SUCCEEDED(pMetadataQueryReader->GetMetadataByName(
                L"/appext/application",
                &propValue)) &&
                propValue.vt == (VT_UI1 | VT_VECTOR) &&
                propValue.caub.cElems == 11 &&  // Length of the application block
                (!memcmp(propValue.caub.pElems, "NETSCAPE2.0", propValue.caub.cElems) ||
                    !memcmp(propValue.caub.pElems, "ANIMEXTS1.0", propValue.caub.cElems)))
            {
                PropVariantClear(&propValue);

                hr = pMetadataQueryReader->GetMetadataByName(L"/appext/data", &propValue);
                if (SUCCEEDED(hr))
                {
                    //  The data is in the following format:
                    //  byte 0: extsize (must be > 1)
                    //  byte 1: loopType (1 == animated gif)
                    //  byte 2: loop count (least significant byte)
                    //  byte 3: loop count (most significant byte)
                    //  byte 4: set to zero
                    if (propValue.vt == (VT_UI1 | VT_VECTOR) &&
                        propValue.caub.cElems >= 4 &&
                        propValue.caub.pElems[0] > 0 &&
                        propValue.caub.pElems[1] == 1)
                    {
                        m_uTotalLoopCount = MAKEWORD(propValue.caub.pElems[2],
                            propValue.caub.pElems[3]);

                        // If the total loop count is not zero, we then have a loop count
                        // If it is 0, then we repeat infinitely
                        if (m_uTotalLoopCount != 0)
                        {
                            m_fHasLoop = TRUE;
                        }
                    }
                }
            }
        }

        PropVariantClear(&propValue);
        SafeRelease(pMetadataQueryReader);
        return hr;
    }

    HRESULT GetFrameMetadata(UINT uFrameIndex)
    {
        IWICBitmapFrameDecode* pWicFrame = NULL;
        IWICMetadataQueryReader* pFrameMetadataQueryReader = NULL;

        PROPVARIANT propValue;
        PropVariantInit(&propValue);

        // Retrieve the current frame
        HRESULT hr = m_pDecoder->GetFrame(uFrameIndex, &pWicFrame);
        
        if (SUCCEEDED(hr))
        {
            // Get Metadata Query Reader from the frame
            hr = pWicFrame->GetMetadataQueryReader(&pFrameMetadataQueryReader);
        }

        // Get the Metadata for the current frame
        if (SUCCEEDED(hr))
        {
            hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Left", &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_framePosition.left = static_cast<FLOAT>(propValue.uiVal);
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Top", &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_framePosition.top = static_cast<FLOAT>(propValue.uiVal);
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Width", &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_framePosition.right = static_cast<FLOAT>(propValue.uiVal)
                        + m_framePosition.left;
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Height", &propValue);
            if (SUCCEEDED(hr))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    m_framePosition.bottom = static_cast<FLOAT>(propValue.uiVal)
                        + m_framePosition.top;
                }
                PropVariantClear(&propValue);
            }
        }

        if (SUCCEEDED(hr))
        {
            // Get delay from the optional Graphic Control Extension
            if (SUCCEEDED(pFrameMetadataQueryReader->GetMetadataByName(
                L"/grctlext/Delay",
                &propValue)))
            {
                hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
                if (SUCCEEDED(hr))
                {
                    // Convert the delay retrieved in 10 ms units to a delay in 1 ms units
                    hr = UIntMult(propValue.uiVal, 10, &m_uFrameDelay);
                }
                PropVariantClear(&propValue);
            }
            else
            {
                // Failed to get delay from graphic control extension. Possibly a
                // single frame image (non-animated gif)
                m_uFrameDelay = 0;
            }
        }

        if (SUCCEEDED(hr))
        {
            if (SUCCEEDED(pFrameMetadataQueryReader->GetMetadataByName(
                L"/grctlext/Disposal",
                &propValue)))
            {
                hr = (propValue.vt == VT_UI1) ? S_OK : E_FAIL;
                if (SUCCEEDED(hr))
                {
                    m_uFrameDisposal = propValue.bVal;
                }
            }
            else
            {
                // Failed to get the disposal method, use default. Possibly a 
                // non-animated gif.
                m_uFrameDisposal = DM_UNDEFINED;
            }
        }

        PropVariantClear(&propValue);

        SafeRelease(pWicFrame);
        SafeRelease(pFrameMetadataQueryReader);

        return hr;
    }

    int Open(WCHAR* pszFileName)
    {
        RECT rcClient = {};
        RECT rcWindow = {};

        int hr = CoCreateInstance(
            CLSID_WICImagingFactory,
            NULL,
            CLSCTX_INPROC_SERVER,
            IID_PPV_ARGS(&m_pIWICFactory));

        if (SUCCEEDED(hr))
        {
            hr = m_pIWICFactory->CreateDecoderFromFilename(
                pszFileName,
                NULL,
                GENERIC_READ,
                WICDecodeMetadataCacheOnLoad,
                &m_pDecoder);
            if (SUCCEEDED(hr))
            {
                hr = GetGlobalMetadata();
            }
        }

        return hr;
    }

};

extern "C" {
    ANIMATEDGIF_API int OpenAnimatedGif(WCHAR* pszFileName, /* out */ AnimatedGifMetadata* gif)
    {
        AnimatedGif* a = new AnimatedGif();
        int hr = a->Open(pszFileName);
        if (SUCCEEDED(hr))
        {
            gif->backgroundColor = { a->m_backgroundColor.r, a->m_backgroundColor.g, a->m_backgroundColor.b, a->m_backgroundColor.a };
            gif->numFrames = a->m_cFrames;
            gif->cxGifImage = a->m_cxGifImage;
            gif->cyGifImage = a->m_cyGifImage;
            gif->cxGifImagePixel = a->m_cxGifImagePixel;
            gif->cyGifImagePixel = a->m_cyGifImagePixel;
            gif->hasLoop = a->m_fHasLoop;
            gif->handle = a;
        }

        return hr;
    }

    ANIMATEDGIF_API int ReadFrameMetadata(AnimatedGifMetadata* gif, UINT frameIndex, /* out */ AnimatedGifFrameMetadata* frame)
    {
        AnimatedGif* a = (AnimatedGif*)(gif->handle);
        int hr = 0;
        if (a == NULL)
        {
            hr = E_FAIL;
        }
        else {
            hr = a->GetFrameMetadata(frameIndex);
            if (SUCCEEDED(hr))
            {
                frame->frameDelay = a->m_uFrameDelay;
                frame->framePosition = { a->m_framePosition.left, a->m_framePosition.top, a->m_framePosition.right, a->m_framePosition.bottom };
                frame->frameDisposal = (DISPOSAL_METHODS)(a->m_uFrameDisposal);
            }
        }
        return hr;
    }

    ANIMATEDGIF_API void ReleaseAnimatedGif(AnimatedGifMetadata* gif)
    {
        AnimatedGif* a = (AnimatedGif*)(gif->handle);
        gif->handle = NULL;
        delete a;
    }
}