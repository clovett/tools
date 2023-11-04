// DriveSpaceCpp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <vector>
#include <string>
#include <algorithm>
#include <cctype>
#include <chrono>
#include <sstream>
#include <functional>
#include <filesystem>
#include "timer.h"
#include <windows.h>
#include <strsafe.h>

static inline void ltrim(std::string& s, unsigned char c) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [c](unsigned char ch) {
        return ch == c;
        }));
}

static inline void tolower(std::string& s) {
    std::transform(s.begin(), s.end(), s.begin(),
        [](unsigned char c) { return std::tolower(c); });
}
using namespace common_utils;

class Program
{
private:
    std::vector<std::wstring> folders;
public:
    Program() {}

    bool ParseCommandLine(int argc, char* argv[])
    {
        for (int i = 1; i < argc; i++)
        {
            std::string arg = argv[i];
            if (arg[0] == '-') {
                ltrim(arg, '-');
                tolower(arg);
                if (arg == "?" || arg == "h" || arg == "help") {
                    return false;
                }
                else {
                    std::string narrow(arg.begin(), arg.end());
                    std::cout << "### error: unknown argument --" << narrow << std::endl;
                    return false;
                }
            }
            else {
                std::wstring wide(arg.begin(), arg.end());
                folders.push_back(wide);
            }
        }
        if (folders.size() == 0) {
            std::cout << "### error: missing folder argument" << std::endl;
            return false;
        }
        return true;
    }

    void EnumerateFiles(LPCTSTR folder, std::function<void(WIN32_FIND_DATA&)> callback)
    {
        WIN32_FIND_DATA ffd;
        HANDLE hFind = INVALID_HANDLE_VALUE;
        TCHAR szDir[MAX_PATH];
        size_t length_of_arg;

        StringCchLength(folder, MAX_PATH, &length_of_arg);

        if (length_of_arg > (MAX_PATH - 3))
        {
            std::cout << "Directory path is too long." << std::endl;
            return;
        }
        StringCchCopy(szDir, MAX_PATH, folder);
        StringCchCat(szDir, MAX_PATH, L"\\*");
        DWORD dwError = 0;
        hFind = FindFirstFile(szDir, &ffd);
        long long total = 0;
        if (INVALID_HANDLE_VALUE == hFind)
        {
            return;
        }
        do
        {
            if (ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
            {
                // skip
            }
            else
            {
                callback(ffd);
            }
        } while (FindNextFile(hFind, &ffd) != 0);

        FindClose(hFind);
    }

    void EnumerateFolders(LPCTSTR folder, std::function<void(LPCTSTR path, WIN32_FIND_DATA&)> callback)
    {
        WIN32_FIND_DATA ffd;
        HANDLE hFind = INVALID_HANDLE_VALUE;
        TCHAR szDir[MAX_PATH];
        size_t length_of_arg;

        StringCchLength(folder, MAX_PATH, &length_of_arg);

        if (length_of_arg > (MAX_PATH - 3))
        {
            std::cout << "Directory path is too long." << std::endl;
            return;
        }
        StringCchCopy(szDir, MAX_PATH, folder);
        StringCchCat(szDir, MAX_PATH, L"\\*");
        DWORD dwError = 0;
        hFind = FindFirstFile(szDir, &ffd);
        long long total = 0;
        if (INVALID_HANDLE_VALUE == hFind)
        {
            return;
        }
        do
        {
            if (ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
            {
                size_t length_of_name;
                StringCchLength(ffd.cFileName, MAX_PATH, &length_of_name);
                if (length_of_name == 1 && ffd.cFileName[0] == L'.')
                {
                    // skip the '.' directory
                    continue; 
                }
                if (length_of_name == 2 && ffd.cFileName[0] == L'.' && ffd.cFileName[1] == L'.')
                {
                    // skip the '..' directory
                    continue;
                }
                if (length_of_name + length_of_arg > MAX_PATH) {
                    std::cout << "Directory path is too long." << std::endl;                    
                }
                else {
                    szDir[length_of_arg] = 0;
                    StringCchCat(szDir, MAX_PATH, L"\\");
                    StringCchCat(szDir, MAX_PATH, ffd.cFileName);
                    callback(szDir, ffd);
                }
            }
        } while (FindNextFile(hFind, &ffd) != 0);

        FindClose(hFind);
    }

    long long FileSpace(LPCTSTR folder) {
        long long total = 0;
        EnumerateFiles(folder, [&](auto ffd) {
            LARGE_INTEGER filesize;
            filesize.LowPart = ffd.nFileSizeLow;
            filesize.HighPart = ffd.nFileSizeHigh;
            total += filesize.QuadPart;
            });
        return total;
    }

    long long DriveSpace(LPCTSTR folder)
    {
        long long total = FileSpace(folder);
        EnumerateFolders(folder, [&](auto path, auto ffd) {
            total += DriveSpace(path);

        });
        return total;
    }

    int GetMaxFolderNameLength(LPCTSTR folder)
    {
        int longest = 10;
        EnumerateFolders(folder, [&](auto path, auto ffd) {
            size_t length_of_name;
            StringCchLength(ffd.cFileName, MAX_PATH, &length_of_name);
            if (length_of_name > longest) {
                longest = (int)length_of_name;
            }
            });
        return longest;
    }

    std::string padding(int indent, int len) {
        return std::string(indent - len, '_');
    }

    std::string FormatSize(long long size) {

        std::stringstream ss;
        if (size > 1e9) {
            size /= (long long)1e6;
            ss << (float)size / 1000 << " GB";
        }
        else if (size > 1e6) {
            size /= (long long)1e3;
            ss << (float)size / 1000 << " MB";
        }
        else if (size > 1e3) {
            ss << (float)size / 1000 << " KB";
        }
        else {
            ss << size;
        }
        return ss.str();
    }

    int Run() {
        for (auto name : folders) {
            auto total = FileSpace(name.c_str());
            auto indent = GetMaxFolderNameLength(name.c_str()) + 2;
            
            std::cout << "files" << padding(indent, 5) << ": " << FormatSize(total) << std::endl;
            int longest = 10;
            EnumerateFolders(name.c_str(), [&](auto path, auto ffd) {
                total = DriveSpace(path);
                auto filename = std::filesystem::path(path).filename().string();
                std::cout << filename << padding(indent, (int)filename.size()) << ": " << FormatSize(total) << std::endl;
                });
        }
        return 0;
    }

    void PrintUsage() {
        std::cout << "Usage: DriveSpace <folder>..." << std::endl;
        std::cout << "Prints summary of where the space is going in this folder." << std::endl;
    }
};

int main(int argc, char* argv[])
{
    Program p;
    if (!p.ParseCommandLine(argc, argv))
    {
        p.PrintUsage();
        return 1;
    }
    Timer t;
    t.start();
    auto rc = p.Run();
    t.stop();
    std::cout << "scanned files in " << t.seconds() << " seconds" << std::endl;
    return rc;
}
