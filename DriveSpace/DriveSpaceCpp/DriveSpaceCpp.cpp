// DriveSpaceCpp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <vector>
#include <string>
#include <algorithm>
#include <cctype>
#include <chrono>
#include <filesystem>
#include <sstream>
#include "timer.h"

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
    std::vector<std::string> folders;
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
                    std::cout << "### error: unknown argument " << argv[i] << std::endl;
                    return false;
                }
            }
            else {
                folders.push_back(arg);
            }
        }
        if (folders.size() == 0) {
            std::cout << "### error: missing folder argument" << std::endl;
            return false;
        }
        return true;
    }

    long long FileSpace(const std::filesystem::path& folder) {
        long long total = 0;
        for (auto const& dir_entry : std::filesystem::directory_iterator{ folder })
        {
            if (!dir_entry.is_directory()) {
                total += dir_entry.file_size();
            }
        }
        return total;
    }

    long long DriveSpace(const std::filesystem::path& folder)
    {
        auto total = FileSpace(folder);
        // directory_iterator can be iterated using a range-for loop
        for (auto const& dir_entry : std::filesystem::directory_iterator{ folder })
            if (dir_entry.is_directory()) {
                total += DriveSpace(dir_entry.path());
            }
        return total;
    }

    int GetMaxFileNameLength(const std::filesystem::path& folder)
    {
        int longest = 10;
        // directory_iterator can be iterated using a range-for loop
        for (auto const& dir_entry : std::filesystem::directory_iterator{ folder })
            if (dir_entry.is_directory()) {
                auto x = dir_entry.path().filename().string().size();
                if (x > longest) {
                    longest = (int)x;
                }
            }
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
            auto total = FileSpace(name);
            auto indent = GetMaxFileNameLength(name) + 2;
            std::cout << "indent=" << indent << std::endl;

            std::cout << "files" << padding(indent, 5) << ": " << FormatSize(total) << std::endl;
            for (auto const& dir_entry : std::filesystem::directory_iterator{ name })
            {
                if (dir_entry.is_directory()) {
                    total = DriveSpace(dir_entry.path());
                    auto filename = dir_entry.path().filename().string();
                    std::cout << filename << padding(indent, (int)filename.size()) << ": " << FormatSize(total) << std::endl;
                }
            }
            DriveSpace(name);
        }
        return 0;
    }

    void PrintUsage() {
        std::cout << "Usage: DriveSpace <folder>..." << std::endl;
        std::cout << "Prints summary of where the space is going in this folder." << std::endl;
    }
};

int main(int argc, char* argv[] )
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
    std::cout << "scanned files in " << t.seconds ()<< " seconds" << std::endl;
    return rc;
}
