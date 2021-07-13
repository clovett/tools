// regcheck.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <Windows.h>
#include <iostream>
#include <vector>
#include <map>

enum RegistryValueType {
    None = REG_NONE,
    String = REG_SZ,
    ExpandString = REG_EXPAND_SZ,
    Binary = REG_BINARY,
    DWord = REG_DWORD,
    DWordLittleEndian = REG_DWORD_LITTLE_ENDIAN,
    DWordBigEndian = REG_DWORD_BIG_ENDIAN,
    Link = REG_LINK,
    MultiString = REG_MULTI_SZ,
    ResourceList = REG_RESOURCE_LIST,
    FullDescriptor = REG_FULL_RESOURCE_DESCRIPTOR,
    ResourceRequirementsList = REG_RESOURCE_REQUIREMENTS_LIST,
    QWord = REG_QWORD,
    QWordLittleEndian = REG_QWORD_LITTLE_ENDIAN
};

class UnicodeString {
    wchar_t* buffer = nullptr;
    int size;
    char* ansi = nullptr;

    UnicodeString(const wchar_t* a, const wchar_t* b) {
        int alen = (int)wcslen(a);
        this->size = alen + (int)wcslen(b);
        this->buffer = new wchar_t[size + 1];
        wcscpy_s(this->buffer, (size_t)this->size + 1, a);
        wcscpy_s(&this->buffer[alen], (size_t)this->size + 1 - alen, b);
    }
public:
    UnicodeString() {}
    UnicodeString(int count, wchar_t ch) {
        size = count;
        buffer = new wchar_t[size + 1];
        for (int i = 0; i < count; i++) {
            buffer[i] = ch;
        }
    }
    UnicodeString(const wchar_t* unicode, int len = 0) {
        if (len == 0) {
            this->size = len = (int)wcslen(unicode);
        }
        else {
            this->size = len;
        }
        this->buffer = new wchar_t[(size_t)size + 1];
        wcsncpy_s(this->buffer, (size_t)len + 1, unicode, (size_t)len);
    }
    UnicodeString(char* ascii) {
        int len = (int)strlen(ascii);
        size = len;
        buffer = new wchar_t[size + 1];
        len = MultiByteToWideChar(1252, 0, ascii, len, buffer, size);
        buffer[len] = '\0';        
    }
    UnicodeString(const UnicodeString& other) {
        if (other.buffer != nullptr) {
            this->size = other.size;
            this->buffer = new wchar_t[size + 1];
            wcscpy_s(this->buffer, this->size + 1, other.buffer);
        }
    }

    UnicodeString(UnicodeString&& move) {
        this->buffer = move.buffer;
        this->size = move.size;
        this->ansi = move.ansi;
        move.buffer = nullptr;
        move.size = 0;
        move.ansi = nullptr;
    }

    ~UnicodeString() {
        if (buffer != nullptr) {
            delete[] buffer;
        }
        if (ansi != nullptr) {
            delete[] ansi;
        }
    }

    UnicodeString& operator=(const UnicodeString& other) {
        if (this->size < other.size) {
            if (buffer != nullptr) {
                delete[] buffer;
            }
            if (ansi != nullptr) {
                delete[] ansi;
            }

            this->size = other.size;
            this->buffer = new wchar_t[size + 1];
        }

        wcscpy_s(this->buffer, this->size + 1, other.buffer);
        return *this;
    }

    UnicodeString Head(wchar_t separator) {
        if (buffer != nullptr) {
            wchar_t* ptr = wcschr(buffer, separator);
            if (ptr != nullptr) {
                int len = (int)(ptr - buffer);
                return UnicodeString(buffer, len);
            }
            else {
                return *this;
            }
        }
        return UnicodeString();
    }

    UnicodeString Substring(int pos, int count = 0) {
        if (buffer != nullptr) {
            int len = length();
            if (pos > len) {
                return UnicodeString();
            }
            if (count == 0) {
                count = len - pos;
            }
            if (pos + count > len) {
                count = len - pos;
            }
            return UnicodeString(&buffer[pos], count);
        }
        return UnicodeString();
    }


    int length() {
        if (buffer == nullptr) {
            return 0;
        }
        return (int)wcslen(buffer);
    }

    const wchar_t* get() const {
        return buffer;
    }

    char* ascii(int codepage = 20127) {
        if (ansi != nullptr) {
            delete[] ansi;
            ansi = nullptr;
        }
        if (buffer == nullptr) {
            return nullptr;
        }
        char defaultChar = (char)0xff;
        BOOL usedDefaultChar = false;
        int size = length();
        int len = WideCharToMultiByte(codepage, 0, buffer, size, NULL, 0, NULL, NULL);
        ansi = new char[(size_t)len + 1];
        len = WideCharToMultiByte(codepage, 0, buffer, size, ansi, len, &defaultChar, &usedDefaultChar);
        ansi[len] = '\0';
        if (usedDefaultChar) {
            throw new std::runtime_error("found bad char!");
        }
        return ansi;
    }

    UnicodeString operator+(const UnicodeString& b) {
        return UnicodeString(this->buffer, b.get());
    }

    UnicodeString operator+(const wchar_t* b) {
        return UnicodeString(this->buffer, b);
    }

private:
};

class RegistryKey
{
    HKEY key = 0;

    RegistryKey(HKEY key, const UnicodeString name) : name(name), key(key)
    {
    }

public:
    UnicodeString name;

    RegistryKey(RegistryKey&& move) : name(move.name)
    {
        this->key = move.key;
        move.key = 0;
    }

    ~RegistryKey() {
        if (key != 0) {
            RegCloseKey(key);
        }
    }

    static RegistryKey Open(HKEY root, const wchar_t* name) 
    {
        HKEY subKey = 0;
        DWORD dwRet = RegOpenKey(root, name, &subKey);
        if (dwRet != ERROR_SUCCESS)
        {
            throw new std::runtime_error("key not found");
        }
        return RegistryKey(subKey, UnicodeString(name));
    }

    RegistryValueType GetValueType(const UnicodeString& name)
    {
        DWORD valueType = 0;
        DWORD dwRet = RegQueryValueEx(this->key,
            name.get(),
            NULL,
            &valueType,
            NULL,
            NULL);

        if (dwRet != 0) {
            throw new std::runtime_error("value not found");
        }

        return static_cast<RegistryValueType>(valueType);
    }

    UnicodeString GetStringValue(const UnicodeString& name) {

        DWORD cbData;
        DWORD valueType = 0;
        DWORD dwRet = RegQueryValueEx(this->key,
            name.get(),
NULL,
& valueType,
NULL,
& cbData);

if (dwRet != 0) {
    throw new std::runtime_error("value not found");
}

int count = (cbData / sizeof(wchar_t)) + 1;
UnicodeString buf(count, '\0');
dwRet = RegQueryValueEx(this->key,
    name.get(),
    NULL,
    &valueType,
    (LPBYTE)(buf.get()),
    &cbData);

if (dwRet != ERROR_SUCCESS) {
    throw new std::runtime_error("unexpected error getting value");

}
else {
    return buf;
}
    }

    std::vector<UnicodeString> GetValueNames() const {
        std::vector<UnicodeString> result;
        DWORD index = 0;
        const int MAX_BUFFER = 32767;
        wchar_t* buffer = new wchar_t[MAX_BUFFER];
        while (true) {
            DWORD cchValueName = MAX_BUFFER;
            int rc = RegEnumValue(this->key,
                index,
                buffer,
                &cchValueName,
                NULL,
                NULL,
                NULL,
                NULL);
            if (rc != 0) {
                break;
            }
            result.push_back(UnicodeString(buffer));
            index++;
        }
        return result;
    }

    std::vector<UnicodeString> GetSubKeyNames() const {
        std::vector<UnicodeString> result;
        DWORD index = 0;
        const int MAX_BUFFER = 32767;
        wchar_t* buffer = new wchar_t[MAX_BUFFER];
        while (true) {
            DWORD cchValueName = MAX_BUFFER;
            int rc = RegEnumKeyEx(this->key,
                index,
                buffer,
                &cchValueName,
                NULL,
                NULL,
                NULL,
                NULL);
            if (rc != 0) {
                break;
            }
            result.push_back(UnicodeString(buffer));
            index++;
        }
        return result;

    }

    RegistryKey OpenSubKey(const UnicodeString& name) {
        HKEY subKey = 0;
        int rc = RegOpenKey(this->key, name.get(), &subKey);
        if (rc != 0) {
            throw new std::runtime_error("key not found");
        }
        UnicodeString combined = this->name + L"\\" + name;
        return RegistryKey(subKey, combined);
    }
};

class Program
{
    int errors = 0;
    int strings = 0;
    bool verbose = false;
    UnicodeString keyname;

    std::map<std::string, HKEY> hiveMap;

public:
    Program() 
    {
        hiveMap["HKCR"] = HKEY_CLASSES_ROOT;
        hiveMap["HKEY_CLASSES_ROOT"] = HKEY_CLASSES_ROOT;
        hiveMap["HKLM"] = HKEY_LOCAL_MACHINE;
        hiveMap["HKEY_LOCAL_MACHINE"] = HKEY_LOCAL_MACHINE;
        hiveMap["HKCU"] = HKEY_CURRENT_USER;
        hiveMap["HKEY_CURRENT_USER"] = HKEY_CURRENT_USER;
        hiveMap["HKEY_USERS"] = HKEY_USERS;
        hiveMap["HKCC"] = HKEY_CURRENT_CONFIG;
        hiveMap["HKEY_CURRENT_CONFIG"] = HKEY_CURRENT_CONFIG;
    }

    bool ParseCommandLine(int argc, char* argv[]) {

        for (int i = 1; i < argc; i++)
        {
            char* arg = argv[i];
            if (strcmp(arg, "-verbose") == 0 || strcmp(arg, "--verbose") == 0)
            {
                verbose = true;
            }
            else if (keyname.get() == nullptr)
            {
                keyname = UnicodeString(arg);
            }
            else {
                std::cout << "too many arguments" << std::endl;
                return false;
            }
        }
        return true;
    }

    void PrintUsage()
    {
        std::cout << "usage: regcheck [--versbose] key" << std::endl;
        std::cout << "Scans all subkeys for string values that contain illegal characters" << std::endl;
    }

    int Run()
    {
        std::cout << "Checking all string values under: " << keyname.ascii() << std::endl;
        auto head = keyname.Head('\\');
        auto ptr = hiveMap.find(head.ascii());
        if (ptr == hiveMap.end()) {
            std::cout << "Registry root not found: " << head.ascii() << std::endl;
            return 1;
        }
        HKEY test = HKEY_LOCAL_MACHINE;
        keyname = keyname.Substring(head.length() + 1);
        RegistryKey key = RegistryKey::Open(ptr->second, keyname.get());
        check_registry(key, verbose);

        std::cout << "Found " << strings << " strings" << std::endl;
        if (errors > 0) {
            std::cerr << "*** Errors: " << errors << " found" << std::endl;
        }
        else {
            std::cout << "Found no invalid strings." << std::endl;
        }
        return errors;
    }

    void check_registry(RegistryKey& key, bool verbose)
    {
        for (auto name : key.GetValueNames())
        {
            RegistryValueType type = key.GetValueType(name);
            if (type == RegistryValueType::String || type == RegistryValueType::ExpandString || type == RegistryValueType::MultiString) {
                if (verbose) std::cout << key.name.ascii() << '\\' << name.ascii() << std::endl;
                UnicodeString value = key.GetStringValue(name);
                try {
                    strings++;
                    char* check = value.ascii();
                }
                catch (std::exception&) {
                    std::cout << "Ascii error in value " << name.ascii() << " of registry key " << key.name.ascii() << std::endl;
                    errors++;
                }
            }
        }
        for (auto name : key.GetSubKeyNames())
        {
            RegistryKey sub = key.OpenSubKey(name);
            check_registry(sub, verbose);
        }
    }
};

int main(int argc, char* argv[])
{
    Program p;
    if (p.ParseCommandLine(argc, argv)){
        return p.Run();
    }
    else {
        p.PrintUsage();
    }

    return 1;
}