#ifndef LOG
#define LOG

void InitLog();
int AppendLog(const char* filename);
void WriteLog(char* line);
void CloseLog();

// reading
int OpenLog(const char* filename);
int ReadLog(char* buffer, int bufsize);

#endif
