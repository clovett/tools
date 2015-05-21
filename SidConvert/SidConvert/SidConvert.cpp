// SidConvert.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <stdio.h>
#include <stdarg.h>
#include <windows.h>
#include <Winnt.h>
#include <sddl.h>

bool
ConvertClassGuidToSid(
    _In_ GUID classGuid,
    _Outptr_ PSID * pClassSid
    )
{
    SID_IDENTIFIER_AUTHORITY ApplicationAuthority = SECURITY_APP_PACKAGE_AUTHORITY;
    DWORD subAuthorities[4];

    RtlCopyMemory(subAuthorities, &classGuid, sizeof(GUID));

    if (!AllocateAndInitializeSid(&ApplicationAuthority,
        5,
        SECURITY_CAPABILITY_BASE_RID,
        subAuthorities[0],
        subAuthorities[1],
        subAuthorities[2],
        subAuthorities[3],
        0,
        0,
        0,
        pClassSid))
    {
        return false;
    }

    return true;
}

HRESULT
ConvertClassStringToSid(
    _In_ PCWSTR pszClassSid,
    _Outptr_ PSID* pClassSid
    )
{
    GUID classGuid;
    NTSTATUS status;
    HRESULT hr = S_OK;

    status = UuidFromString((RPC_WSTR)pszClassSid, &classGuid);
    if (!SUCCEEDED(status))
    {
        hr = E_INVALIDARG;
    }

    if (SUCCEEDED(hr))
    {
        if (!ConvertClassGuidToSid(classGuid, pClassSid))
        {
            hr = E_INVALIDARG;
        }
    }

    return hr;
}

_Success_(return != false)
bool
ConvertSidToClassGuid(
    _In_ PSID pClassSid,
    _Out_ GUID * pClassGuid
    )
{
    if (*GetSidSubAuthorityCount(pClassSid) != 5)
    {
        return false;
    }

    DWORD subAuthorities[4];
    subAuthorities[0] = *GetSidSubAuthority(pClassSid, 1);
    subAuthorities[1] = *GetSidSubAuthority(pClassSid, 2);
    subAuthorities[2] = *GetSidSubAuthority(pClassSid, 3);
    subAuthorities[3] = *GetSidSubAuthority(pClassSid, 4);

    RtlCopyMemory(pClassGuid, subAuthorities, sizeof(GUID));

    return true;
}

class Program
{
private:
    bool reverse = false;
    wchar_t* name = nullptr;
public:

    bool ParseCommandLine(int argc, _TCHAR* argv[])
    {
        for (int i = 1; i < argc; i++)
        {
            _TCHAR* arg = argv[i];
            if (arg[0] == '-' || arg[0] == '/')
            {
                if (wcscmp(&arg[1], L"r") == 0)
                {
                    reverse = true;
                }
                else if (wcscmp(&arg[1], L"h") == 0 || wcscmp(&arg[1], L"?") == 0 || wcscmp(&arg[1], L"help") == 0)
                {
                    return false;
                }
            }
            else if (name == nullptr) {
                name = arg;
            }
            else {
                printf("### Too many arguments\n");
                return false;
            }
        }

        if (name == nullptr)
        {
            printf("### Missing name argument\n");
            return false;
        }

        return true;
    }

    void PrintUsage() {
        printf("Usage: SidConvert [-r] guid-or-sid \n\n");
        printf("This tool can convert between class GUID and SID\n");
        printf("Use -r option to go from SID to class GUID.\n");
    }

    int Process() {

        HRESULT hr = 0;

        if (reverse)
        {
            return Reverse();
        }
        else 
        {
            return Forward();
        }
    }

    int Reverse()
    {

        HRESULT hr = 0;

        PSID pSid;
        hr = ConvertStringSidToSid(name, &pSid);
        if (SUCCEEDED(hr))
        {
            GUID guid;
            hr = ConvertSidToClassGuid(pSid, &guid);

            if (SUCCEEDED(hr))
            {
                RPC_WSTR outputString = nullptr;
                hr = UuidToString(&guid, &outputString);

                if (SUCCEEDED(hr))
                {
                    wprintf(L"%s\n", outputString);
                }
                else {

                    wprintf(L"UuidToString failed with error code %x\n", hr);
                }
            }
            else {

                wprintf(L"ConvertSidToClassGuid failed with error code %x\n", hr);
            }
            LocalFree(pSid);
        }
        else {

            wprintf(L"ConvertClassStringToSid failed with error code %x\n", hr);
        }

        return hr;
    }

    int Forward()
    {
        GUID guid;
        HRESULT hr = UuidFromString((RPC_WSTR)name, &guid);
        if (SUCCEEDED(hr)) {
            PSID psid;
            hr = ConvertClassGuidToSid(guid, &psid);
            if (SUCCEEDED(hr)) {
                
                LPWSTR outputString = nullptr;
                hr = ConvertSidToStringSid(psid, &outputString);
                if (SUCCEEDED(hr))
                {
                    wprintf(L"%s\n", outputString);
                    LocalFree(outputString);
                }
                else {
                    wprintf(L"ConvertSidToStringSid failed with error code %x\n", hr);
                }
            }
            else {
                wprintf(L"ConvertClassGuidToSid failed with error code %x\n", hr);
            }
        }
        else {

            wprintf(L"UuidFromString failed with error code %x\n", hr);
        }
        return hr;
    }
};


int _tmain(int argc, _TCHAR* argv[])
{
    Program p;
    if (!p.ParseCommandLine(argc, argv))
    {
        p.PrintUsage();
        return 1;
    }
    return p.Process();
}

