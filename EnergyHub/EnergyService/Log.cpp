#include <stdio.h>
#include <string.h>
#include "Log.h"

#if _WIN32
#include <Windows.h>
#endif

FILE* filePtr;

void InitLog()
{
    filePtr = nullptr;
}

int AppendLog(const char* filename)
{
    CloseLog();
    filePtr = fopen(filename, "a");
    if (filePtr == nullptr)
    {
        return -1;
    }
    return 0;
}

void WriteLog(char* line)
{
    if (filePtr != nullptr)
    {
        fwrite(line, 1, strlen(line), filePtr);
        fflush(filePtr);
    }
}

void CloseLog()
{
    if (filePtr != nullptr){
        fclose(filePtr);
        filePtr = nullptr;
    }
}

// reading
int OpenLog(const char* filename)
{
    CloseLog();
    filePtr = fopen(filename, "r");
    if (filePtr == nullptr)
    {
        return -1;
    }
    return 0;
}

int ReadLog(char* buffer, int bufsize)
{
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
