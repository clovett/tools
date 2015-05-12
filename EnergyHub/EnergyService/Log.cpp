#include <stdio.h>
#include <string.h>
#include "Log.h"

#if _WIN32
#include <Windows.h>
#endif

Log* Log::_instance = nullptr;

Log::Log()
{
    filePtr = nullptr;
    reading = false;
    _instance = this;
}

Log* Log::Instance()
{
    return _instance;
}

Log::~Log()
{
    CloseLog();
    _instance = nullptr;
}


int Log::AppendLog(const char* filename)
{
    if (reading) {
        return 1;
    }
    CloseLog();
    filePtr = fopen(filename, "a");
    if (filePtr == nullptr)
    {
        return -1;
    }
    return 0;
}

int Log::Truncate(const char* filename)
{
    if (reading) {
        return 1;
    }
    CloseLog();
    filePtr = fopen(filename, "w");
    if (filePtr == nullptr)
    {
        return -1;
    }
    return 0;
}

void Log::WriteLog(char* line)
{
    if (reading) {
        return;
    }
    if (filePtr != nullptr)
    {
        fwrite(line, 1, strlen(line), filePtr);
        fflush(filePtr);
    }
}

void Log::CloseLog()
{
    if (filePtr != nullptr){
        fclose(filePtr);
        filePtr = nullptr;
    }
    reading = false;
}

// reading
int Log::OpenLog(const char* filename)
{
    CloseLog();
    filePtr = fopen(filename, "r");
    if (filePtr == nullptr)
    {
        return -1;
    }
    reading = true;
    return 0;
}

int Log::ReadLog(char* buffer, int bufsize)
{
    if (!reading)
    {
        return 1;
    }
    buffer[0] = '\0';
    if (filePtr != nullptr){
        
        int ch = fgetc(filePtr);
        if (ch == EOF)
        {
            return EOF;
        }
        int pos = 0;
        while (ch != -1)
        {
            if (ch == '\r' || ch == '\n')
            {
                if (pos > 0) {
                    break;
                }
            }
            else if (pos < bufsize - 1)
            {
                buffer[pos++] = ch;
                buffer[pos] = '\0';
            }            
            ch = fgetc(filePtr);
        }
    }
    return 0;
}


int Log::CountLines()
{
    if (!reading)
    {
        return 1;
    }
    int count = 0;
    if (filePtr != nullptr){

        int ch = fgetc(filePtr);
        if (ch == EOF)
        {
            return EOF;
        }
        int pos = 0;
        while (ch != -1)
        {
            if (ch == '\r' || ch == '\n')
            {
                if (pos > 0) {
                    count++;
                    pos = 0;
                }
            }
            else {
                pos++;
            }
            ch = fgetc(filePtr);
        }
    }
    return count;
}
