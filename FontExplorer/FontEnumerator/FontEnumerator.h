#pragma once
using namespace Platform;
using namespace Windows::Foundation::Collections;

namespace FontExplorer
{
    public ref class FontEnumerator sealed
    {
    public:

        static IVector<String^>^ GetFonts();
    };
}