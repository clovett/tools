#ifndef LOG
#define LOG

#include <stdio.h>
#include <string.h>

class Log
{
    FILE* filePtr;
    bool reading;
    static Log* _instance;
public:
    Log();
    ~Log();

    static Log* Instance();

    int AppendLog(const char* filename);
    void WriteLog(char* line);
    void CloseLog();
    int Truncate(const char* filename);

    // reading - while reading AppendLog and WriteLog do nothing.
    int OpenLog(const char* filename);
    int ReadLog(char* buffer, int bufsize);
    int CountLines();
};



#endif
