#include <stdio.h>
#include <signal.h>
#include "Log.h"
#include "Efergy.h"
#include "EnergyService.h"
#include <alljoyn/AllJoynStd.h>

#ifdef QCC_OS_LINUX
#define Sleep _sleep
#else
#include <Windows.h>
#endif

static volatile sig_atomic_t s_interrupt = false;

static void CDECL_CALL SigIntHandler(int sig)
{
    QCC_UNUSED(sig);
    s_interrupt = true;
}

class Program
{
private:
    char* logFile;
    bool debug;
    Log log;


public:
    Program(){
        logFile = nullptr;
        debug = false;
    }

    bool ParseCommandLine(int argc, char**argv)
    {
        for (int i = 1; i < argc; i++)
        {
            char* arg = argv[i];
            if (arg[0] == '-' || arg[0] == '/')
            {
                arg++;
                if (_stricmp(arg, "d") == 0) {
                    debug = true;
                }
                else if (_stricmp(arg, "h") == 0 ||
                    _stricmp(arg, "?") == 0 ||
                    _stricmp(arg, "help") == 0) {
                    return false;
                }
            }
            else if (logFile == nullptr) {
                logFile = arg;
            }
            else {
                printf("Too many arguments");
                return false;
            }
        }
        if (logFile == nullptr){

            printf("Missing log file argument");
            return false;
        }
        return true;
    }

    void Run()
    {
        int rc = log.AppendLog(logFile);
        if (rc != 0)
        {
            printf("### Error opening logfile: %s", logFile);
            return;
        }
        InitializeEnergyService(logFile);

        if (debug)
        {
            printf("### Debugging: you can now test the AllJoyn service named 'org.alljoyn.Bus.energy'.\n");
            printf("Press any key to terminate...");
            getc(stdin);
        }
        else 
        {
            ReadEnergyData(&s_interrupt);
        }

        TerminateEnergyService();
    }

};

int main(int argc, char**argv)
{
    /* Install SIGINT handler */
    signal(SIGINT, SigIntHandler);

    Program program;
    if (program.ParseCommandLine(argc, argv))
    {
        program.Run();
    }
    else {
        printf("Usage: rtl_fm -f 433510000 -s 200000 -r 96000 | EnergyService log.csv \n");
        printf("\n"); 
        printf("The EnergyService parses the output of the rtl_fm command, extracts the wattage information and \n");
        printf("saves the data with timestamps to the given log file (log.csv).\n");
        printf("It also exposes an AllJoyn service that allows you to open, read, close and/or truncate that log file\n");
        printf("from any machine in your network.  For example, if this code is running on a headless raspberry pi then\n");
        printf("the AllJoyn services allows you to remotely manage the data.\n");
    }


    return 0;
}
