// SecurityFix.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "SecurityFix.h"
#include "Windows.h"
#include "Sddl.h"
#include "Objbase.h"
#include <stdlib.h>

/*---------------------------------------------------------------------------*\
* NAME: MakeSDAbsolute
* --------------------------------------------------------------------------*
* DESCRIPTION: Takes a self-relative security descriptor and returns a
* newly created absolute security descriptor.
* https://github.com/pauldotknopf/WindowsSDK7-Samples/blob/master/com/fundamentals/dcom/dcomperm/SDMgmt.Cpp
\*---------------------------------------------------------------------------*/
static DWORD MakeSDAbsolute(
    PSECURITY_DESCRIPTOR psidOld,
    PSECURITY_DESCRIPTOR *psidNew
)
{
    PSECURITY_DESCRIPTOR  pSid = NULL;
    DWORD                 cbDescriptor = 0;
    DWORD                 cbDacl = 0;
    DWORD                 cbSacl = 0;
    DWORD                 cbOwnerSID = 0;
    DWORD                 cbGroupSID = 0;
    PACL                  pDacl = NULL;
    PACL                  pSacl = NULL;
    PSID                  psidOwner = NULL;
    PSID                  psidGroup = NULL;
    BOOL                  fPresent = FALSE;
    BOOL                  fSystemDefault = FALSE;
    DWORD                 dwReturnValue = ERROR_SUCCESS;

    // Get SACL
    if (!GetSecurityDescriptorSacl(psidOld, &fPresent, &pSacl, &fSystemDefault))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

    if (pSacl && fPresent)
    {
        cbSacl = pSacl->AclSize;
    }

    // Get DACL
    if (!GetSecurityDescriptorDacl(psidOld, &fPresent, &pDacl, &fSystemDefault))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

    if (pDacl && fPresent)
    {
        cbDacl = pDacl->AclSize;
    }

    // Get Owner
    if (!GetSecurityDescriptorOwner(psidOld, &psidOwner, &fSystemDefault))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

    cbOwnerSID = GetLengthSid(psidOwner);

    // Get Group
    if (!GetSecurityDescriptorGroup(psidOld, &psidGroup, &fSystemDefault))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

    cbGroupSID = GetLengthSid(psidGroup);

    // Do the conversion
    cbDescriptor = 0;

    MakeAbsoluteSD(psidOld, pSid, &cbDescriptor, pDacl, &cbDacl, pSacl,
        &cbSacl, psidOwner, &cbOwnerSID, psidGroup,
        &cbGroupSID);

    pSid = (PSECURITY_DESCRIPTOR)malloc(cbDescriptor);
    if (!pSid)
    {
        dwReturnValue = ERROR_OUTOFMEMORY;
        goto CLEANUP;
    }

    ZeroMemory(pSid, cbDescriptor);

    if (!InitializeSecurityDescriptor(pSid, SECURITY_DESCRIPTOR_REVISION))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

    if (!MakeAbsoluteSD(psidOld, pSid, &cbDescriptor, pDacl, &cbDacl, pSacl,
        &cbSacl, psidOwner, &cbOwnerSID, psidGroup,
        &cbGroupSID))
    {
        dwReturnValue = GetLastError();
        goto CLEANUP;
    }

CLEANUP:

    if (dwReturnValue != ERROR_SUCCESS && pSid)
    {
        free(pSid);
        pSid = NULL;
    }

    *psidNew = pSid;

    return dwReturnValue;
}

bool BluetoothSecurityFix()
{
    const wchar_t* security = L"O:BAG:BAD:(A;;0x7;;;PS)(A;;0x3;;;SY)(A;;0x7;;;BA)(A;;0x3;;;AC)(A;;0x3;;;LS)(A;;0x3;;;NS)";
    PSECURITY_DESCRIPTOR pSecurityDescriptor;
    ULONG securityDescriptorSize;

    if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
        security,
        SDDL_REVISION_1,
        &pSecurityDescriptor,
        &securityDescriptorSize))
    {
        return false;
    }

    // MakeSDAbsolute as defined in
    // https://github.com/pauldotknopf/WindowsSDK7-Samples/blob/master/com/fundamentals/dcom/dcomperm/SDMgmt.Cpp
    PSECURITY_DESCRIPTOR pAbsoluteSecurityDescriptor = NULL;
    MakeSDAbsolute(pSecurityDescriptor, &pAbsoluteSecurityDescriptor);

    HRESULT hResult = CoInitializeSecurity(
        pAbsoluteSecurityDescriptor, // Converted from the above string.
        -1,
        nullptr,
        nullptr,
        RPC_C_AUTHN_LEVEL_DEFAULT,
        RPC_C_IMP_LEVEL_IDENTIFY,
        NULL,
        EOAC_NONE,
        nullptr);
    if (FAILED(hResult))
    {
        return false;
    }
    return true;
}
