// qlock.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "pch.h"
#include <iostream>
#include <windows.h>
#include <winnt.h>
#include <winternl.h>
#include <fileapi.h>
#include <winbase.h>
#include <psapi.h>

#include <string>
#include <map>
#include <algorithm>
#include <cctype>


#define STATUS_INFO_LENGTH_MISMATCH 0xc0000004
#define OBJECT_INFO_LENGTH_MISMATCH 0x80000005
#define ObjectNameInformation 1

typedef NTSTATUS(NTAPI *_NtDuplicateObject)(
    HANDLE SourceProcessHandle,
    HANDLE SourceHandle,
    HANDLE TargetProcessHandle,
    PHANDLE TargetHandle,
    ACCESS_MASK DesiredAccess,
    ULONG Attributes,
    ULONG Options
    );

class SystemHandleSnapshot
{
    /* The following structure is actually called SYSTEM_HANDLE_TABLE_ENTRY_INFO, but SYSTEM_HANDLE is shorter. */
    typedef struct _SYSTEM_HANDLE
    {
        ULONG ProcessId;
        BYTE ObjectTypeNumber;
        BYTE Flags;
        USHORT Handle;
        PVOID Object;
        ACCESS_MASK GrantedAccess;
    } SYSTEM_HANDLE, *PSYSTEM_HANDLE;

    typedef struct _SYSTEM_HANDLE_INFORMATION
    {
        ULONG HandleCount; /* Or NumberOfHandles if you prefer. */
        SYSTEM_HANDLE Handles[1];
    } SYSTEM_HANDLE_INFORMATION, *PSYSTEM_HANDLE_INFORMATION;

    const int SystemHandleInformation = 16;
    SYSTEM_HANDLE_INFORMATION* buffer;

public:
    SystemHandleSnapshot()
    {
        ULONG returnLength = sizeof(SYSTEM_HANDLE_INFORMATION);
        buffer = (SYSTEM_HANDLE_INFORMATION*)malloc(returnLength);
        int rc = 0;
        do
        {
            free(buffer);
            returnLength += 8000; // accomodate any new handles..
            buffer = (SYSTEM_HANDLE_INFORMATION*)malloc(returnLength);
            rc = NtQuerySystemInformation((SYSTEM_INFORMATION_CLASS)SystemHandleInformation, buffer, returnLength, &returnLength);
        } while (rc == STATUS_INFO_LENGTH_MISMATCH);
        if (rc != 0)
        {
            free(buffer);
            buffer = nullptr;
        }
    }
    ~SystemHandleSnapshot()
    {
        if (buffer != nullptr)
        {
            free(buffer);
            buffer = nullptr;
        }
    }

    ULONG GetHandleCount() {
        if (buffer == nullptr) {
            return 0;
        }
        return buffer->HandleCount;
    }

    ULONG GetHandleProcessId(int index)
    {
        return buffer->Handles[index].ProcessId;
    }

    HANDLE GetHandle(int index)
    {
        return (HANDLE)buffer->Handles[index].Handle;
    }

    bool IsNamedPipe(int index) {
        DWORD access = buffer->Handles[index].GrantedAccess;
        return access == 0x0012019f || access == 0x00120189;
    }
};

class ObjectTypeInfo
{
    PUBLIC_OBJECT_TYPE_INFORMATION* info;
public:
    ObjectTypeInfo(HANDLE handle)
    {
        ULONG returnLength = sizeof(PUBLIC_OBJECT_TYPE_INFORMATION);
        info = (PUBLIC_OBJECT_TYPE_INFORMATION*)malloc(returnLength);
        NTSTATUS rc = 0;
        do
        {
            free(info);
            info = (PUBLIC_OBJECT_TYPE_INFORMATION*)malloc(returnLength);
            rc = NtQueryObject(handle, OBJECT_INFORMATION_CLASS::ObjectTypeInformation, info, returnLength, &returnLength);
        } while (rc == STATUS_INFO_LENGTH_MISMATCH);

        if (rc != 0)
        {
            free(info);
            info = nullptr;
        }
    }

    PCWSTR GetTypeName() {
        if (info == nullptr)
        {
            return L"error";
        }
        return info->TypeName.Buffer;
    }

    bool IsFile() {
        return info != nullptr && lstrcmpW(info->TypeName.Buffer, L"File") == 0;
    }

    bool IsDirectory() {
        return info != nullptr && lstrcmpW(info->TypeName.Buffer, L"Directory") == 0;
    }

    ~ObjectTypeInfo() {
        if (info != nullptr) {
            free(info);
        }
    }
};

class ObjectInfo
{
    typedef struct __PUBLIC_OBJECT_NAME_INFORMATION {

        UNICODE_STRING Name;

        ULONG Reserved[22];    // reserved for internal use

    } PUBLIC_OBJECT_NAME_INFORMATION, *PPUBLIC_OBJECT_NAME_INFORMATION;

    PUBLIC_OBJECT_NAME_INFORMATION* name;

    public:

    ObjectInfo(HANDLE handle) {
        ULONG returnLength = sizeof(PUBLIC_OBJECT_NAME_INFORMATION);
        NTSTATUS rc = 0;
        name = (PUBLIC_OBJECT_NAME_INFORMATION*)malloc(returnLength);
        do
        {
            free(name);
            name = (PUBLIC_OBJECT_NAME_INFORMATION*)malloc(returnLength);
            rc = NtQueryObject(handle, (OBJECT_INFORMATION_CLASS)ObjectNameInformation, name, returnLength, &returnLength);
            if (rc == STATUS_INFO_LENGTH_MISMATCH)
            {
                // do something
                rc = OBJECT_INFO_LENGTH_MISMATCH;
            }
        } while (rc == OBJECT_INFO_LENGTH_MISMATCH);
        if (rc != 0)
        {
            free(name);
            name = nullptr;
        }
    }

    ~ObjectInfo() {
        if (name != 0)
        {
            free(name);
            name = nullptr;
        }
    }

    PCWSTR GetName() {
        if (name == nullptr)
        {
            return L"???";
        }
        return name->Name.Buffer;
    }

};

class DosDeviceNameMap
{
    std::map<std::wstring, std::wstring> _drives;
public:
    DosDeviceNameMap()
    {
        for (WCHAR c = 'A'; c <= 'Z'; c++)
        {
            WCHAR drive[3];
            WCHAR targetPath[1000];
            drive[0] = c;
            drive[1] = ':';
            drive[2] = 0;
            DWORD rc = QueryDosDevice(drive, targetPath, 1000);
            if (rc != 0) {
                _drives[targetPath] = drive;
            }
        }
    }

    std::wstring MapPath(std::wstring path)
    {
        if (path[0] == '\\')
        {
            auto i = path.find_first_of('\\', 1);
            if (i != std::string::npos)
            {
                std::wstring prefix = path.substr(0, i);
                if (prefix == L"\\Device")
                {
                    auto j = path.find_first_of('\\', i + 1);
                    if (j != std::string::npos)
                    {
                        std::wstring drive = path.substr(0, j);
                        if (_drives.find(drive) != _drives.end())
                        {
                            std::wstring letter = _drives[drive];
                            if (letter.size() > 0)
                            {
                                return letter + path.substr(j);
                            }
                        }
                    }
                }
            }
        }
        return path;
    }
};

PVOID GetLibraryProcAddress(PCSTR LibraryName, PCSTR ProcName)
{
    return GetProcAddress(GetModuleHandleA(LibraryName), ProcName);
}

std::wstring GetCurrentWorkingDirectory()
{
    const int MAX_FILE_PATH = 32000;
    WCHAR temp[MAX_FILE_PATH];
    DWORD hr = GetCurrentDirectory(MAX_FILE_PATH, temp);
    return temp;
}

std::wstring GetProcessName(HANDLE processHandle)
{
    const int MAX_FILE_PATH = 32000;
    WCHAR temp[MAX_FILE_PATH];
    DWORD size = MAX_FILE_PATH;
    DWORD hr = QueryFullProcessImageName(processHandle, 0, temp, &size);
    if (hr == 0)
    {
        DWORD error = GetLastError();
        if (error != ERROR_ACCESS_DENIED) {
            printf("%x\n", error);
        }
        return L"<admin process>";
    }
    return temp;
}

int main(int argc, char* argv[])
{    
    _NtDuplicateObject NtDuplicateObject = (_NtDuplicateObject)GetLibraryProcAddress("ntdll.dll", "NtDuplicateObject");

    ULONG last_pid = 0;
    SystemHandleSnapshot snapshot;
    HANDLE processHandle = 0;
    NTSTATUS rc;
    HANDLE owner_process = GetCurrentProcess();

    ULONG procid = 0;
    bool printProcTitle = false;
    bool first = true;

    if (argc > 1)
    {
        // query files locked by a given process
        procid = atol(argv[1]);
    }
    else
    {
        // query all processes locking something in the current directory.
    }

    std::wstring cwd = GetCurrentWorkingDirectory();    
    DosDeviceNameMap drives;

    for (ULONG i = 0; i < snapshot.GetHandleCount(); i++)
    {
        /* Check if this handle belongs to the PID the user specified. */
        ULONG pid = snapshot.GetHandleProcessId(i);
        HANDLE handle = snapshot.GetHandle(i);
        HANDLE dupHandle = NULL;

        if (procid != 0 && procid != pid) {
            continue;
        }

        if (snapshot.IsNamedPipe(i))
        {
            // NtQueryObject may hang on file handles pointing to named pipes
            continue; 
        }

        if (pid != last_pid)
        {
            if (processHandle != 0)
            {
                CloseHandle(processHandle);
            }
            last_pid = pid;
            if (!(processHandle = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION, FALSE, pid)))
            {
                //printf("Could not open PID %d! (Don't try to open a system process.)\n", pid);
                continue;
            }
            printProcTitle = true;
        }
        if (!processHandle) {
            continue;
        }

        /* Duplicate the handle so we can query it. */
        rc = NtDuplicateObject(processHandle, handle, owner_process, &dupHandle, 0, 0, 0);
        if (rc != 0)
        {
            //printf("[%#x] Error duplicating handle!\n", rc);
            continue;
        }

        ObjectTypeInfo type(dupHandle);
        if (type.IsFile() || type.IsDirectory())
        {
            // found a file handle.
            ObjectInfo info(dupHandle);

            auto name = info.GetName();
            if (name != nullptr)
            {
                auto path = drives.MapPath(name);
                bool match = false;
                if (procid == 0)
                {
                    auto it = std::search(path.begin(), path.end(), cwd.begin(), cwd.end(),
                        [](char ch1, char ch2) { return std::toupper(ch1) == std::toupper(ch2); }
                    );
                    if (it != path.end())
                    {
                        match = true;
                    }
                }
                else 
                {
                    match = true;
                }
                if (match)
                {
                    if (printProcTitle) {
                        if (!first) {
                            printf("\n");
                        }
                        first = false;
                        auto procName = GetProcessName(processHandle);
                        printf("%S (%d)\n", procName.c_str(), pid);
                        printProcTitle = false;
                    }
                    printf("    %S: %S\n", type.GetTypeName(), path.c_str());
                }                
            }

        }
        else
        {
            //printf("%d\t%S\n", pid, type.GetTypeName());
        }
        CloseHandle(dupHandle);
    }

    if (processHandle != 0)
    {
        CloseHandle(processHandle);
    }
    return 0;
}