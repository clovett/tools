/**
* @file
* @brief Sample implementation of an AllJoyn service.
*
* This sample will show how to set up an AllJoyn service that will registered with the
* wellknown name 'org.alljoyn.Bus.method_sample'.  The service will register a method call
* with the name 'cat'  this method will take two input strings and return a
* Concatenated version of the two strings.
*
*/

/******************************************************************************
* Copyright AllSeen Alliance. All rights reserved.
*
*    Permission to use, copy, modify, and/or distribute this software for any
*    purpose with or without fee is hereby granted, provided that the above
*    copyright notice and this permission notice appear in all copies.
*
*    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
*    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
*    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
*    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
*    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
*    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
*    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
******************************************************************************/
#include <qcc/platform.h>

#include <assert.h>
#include <signal.h>
#include <stdio.h>
#include <vector>

#include <qcc/String.h>

#include <alljoyn/AllJoynStd.h>
#include <alljoyn/BusAttachment.h>
#include <alljoyn/BusObject.h>
#include <alljoyn/DBusStd.h>
#include <alljoyn/Init.h>
#include <alljoyn/MsgArg.h>
#include <alljoyn/version.h>

#include <alljoyn/Status.h>
#include "Log.h"

using namespace std;
using namespace qcc;
using namespace ajn;

/*constants*/
static const char* INTERFACE_NAME = "org.alljoyn.Bus.energy";
static const char* SERVICE_NAME = "org.alljoyn.Bus.energy";
static const char* SERVICE_PATH = "/energy";
static const SessionPort SERVICE_PORT = 25;

class EnergyService : public BusObject {
private:
    const char* logFile;
public:
    EnergyService(BusAttachment& bus, const char* path, const char* logFile) :
        BusObject(path)
    {
        this->reading = false;
        this->logFile = logFile;

        /** Add the test interface to this object */
        const InterfaceDescription* exampleIntf = bus.GetInterface(INTERFACE_NAME);
        assert(exampleIntf);
        AddInterface(*exampleIntf);

        /** Register the method handlers with the object */
        const MethodEntry methodEntries[] = {
            { exampleIntf->GetMember("read"), static_cast<MessageReceiver::MethodHandler>(&EnergyService::Read) }
        };
        QStatus status = AddMethodHandlers(methodEntries, sizeof(methodEntries) / sizeof(methodEntries[0]));
        if (ER_OK != status) {
            printf("Failed to register method handlers for EnergyService.\n");
        }
    }

    void ObjectRegistered()
    {
        BusObject::ObjectRegistered();
        printf("ObjectRegistered has been called.\n");
    }

    void Open(const InterfaceDescription::Member* member, Message& msg)
    {
        QCC_UNUSED(member);

        Log* log = Log::Instance();
        log->CloseLog();
        log->OpenLog(logFile);
        int count = log->CountLines();
        log->CloseLog();
        log->OpenLog(logFile);
        reading = true;

        char buffer[100];
        sprintf(buffer, "%d", count);

        qcc::String outStr(buffer);

        MsgArg outArg("s", outStr.c_str());
        QStatus status = MethodReply(msg, &outArg, 1);
        if (ER_OK != status) {
            printf("Read: Error sending reply.\n");
        }
    }

    void Close(const InterfaceDescription::Member* member, Message& msg)
    {
        QCC_UNUSED(member);
        Log* log = Log::Instance();
        log->CloseLog();
        log->AppendLog(logFile);
        reading = false;

        MsgArg outArg("s", "ok");
        QStatus status = MethodReply(msg, &outArg, 1);
        if (ER_OK != status) {
            printf("Read: Error sending reply.\n");
        }
    }

    void Truncate(const InterfaceDescription::Member* member, Message& msg)
    {
        QCC_UNUSED(member);
        Log* log = Log::Instance();
        log->CloseLog();
        log->Truncate(logFile);

        MsgArg outArg("s", "ok");
        QStatus status = MethodReply(msg, &outArg, 1);
        if (ER_OK != status) {
            printf("Read: Error sending reply.\n");
        }
    }

    void Read(const InterfaceDescription::Member* member, Message& msg)
    {
        QCC_UNUSED(member);

        qcc::String outStr;

        if (!reading)
        {
            outStr = qcc::String("error:not open for reading");
        }
        else 
        {
            Log* log = Log::Instance();
            int rc = log->ReadLog(buffer, 1000);
            if (rc == 0)
            {
                outStr = qcc::String(buffer);
            }
            else {
                outStr = qcc::String("EOF");
            }
        }

        MsgArg outArg("s", outStr.c_str());
        QStatus status = MethodReply(msg, &outArg, 1);
        if (ER_OK != status) {
            printf("Read: Error sending reply.\n");
        }
    }

private:
    bool reading;
    char buffer[1000];
};


class MyBusListener : public BusListener, public SessionPortListener {
    void NameOwnerChanged(const char* busName, const char* previousOwner, const char* newOwner)
    {
        if (newOwner && (0 == strcmp(busName, SERVICE_NAME))) {
            printf("NameOwnerChanged: name=%s, oldOwner=%s, newOwner=%s.\n",
                busName,
                previousOwner ? previousOwner : "<none>",
                newOwner ? newOwner : "<none>");
        }
    }
    bool AcceptSessionJoiner(SessionPort sessionPort, const char* joiner, const SessionOpts& opts)
    {
        if (sessionPort != SERVICE_PORT) {
            printf("Rejecting join attempt on unexpected session port %d.\n", sessionPort);
            return false;
        }
        printf("Accepting join session request from %s (opts.proximity=%x, opts.traffic=%x, opts.transports=%x).\n",
            joiner, opts.proximity, opts.traffic, opts.transports);
        return true;
    }
};

/** The bus listener object. */
static MyBusListener s_busListener;

/** Top level message bus object. */
static BusAttachment* s_msgBus = NULL;

/** Create the interface, report the result to stdout, and return the result status. */
QStatus CreateInterface(void)
{
    /* Add org.alljoyn.Bus.method_sample interface */
    InterfaceDescription* testIntf = NULL;
    QStatus status = s_msgBus->CreateInterface(INTERFACE_NAME, testIntf);

    if (status == ER_OK) {
        printf("Interface created.\n");
        testIntf->AddMethod("open", nullptr, "s", "outStr", 0);
        testIntf->AddMethod("read", nullptr, "s", "outStr", 0);
        testIntf->AddMethod("close", nullptr, "s", "outStr", 0);
        testIntf->AddMethod("truncate", nullptr, "s", "outStr", 0);
        testIntf->Activate();
    }
    else {
        printf("Failed to create interface '%s'.\n", INTERFACE_NAME);
    }

    return status;
}

/** Register the bus object and connect, report the result to stdout, and return the status code. */
QStatus RegisterBusObject(EnergyService* obj)
{
    QStatus status = s_msgBus->RegisterBusObject(*obj);

    if (ER_OK == status) {
        printf("RegisterBusObject succeeded.\n");
    }
    else {
        printf("RegisterBusObject failed (%s).\n", QCC_StatusText(status));
    }

    return status;
}

/** Connect, report the result to stdout, and return the status code. */
QStatus ConnectBusAttachment(void)
{
    QStatus status = s_msgBus->Connect();

    if (ER_OK == status) {
        printf("Connect to '%s' succeeded.\n", s_msgBus->GetConnectSpec().c_str());
    }
    else {
        printf("Failed to connect to '%s' (%s).\n", s_msgBus->GetConnectSpec().c_str(), QCC_StatusText(status));
    }

    return status;
}

/** Start the message bus, report the result to stdout, and return the status code. */
QStatus StartMessageBus(void)
{
    QStatus status = s_msgBus->Start();

    if (ER_OK == status) {
        printf("BusAttachment started.\n");
    }
    else {
        printf("Start of BusAttachment failed (%s).\n", QCC_StatusText(status));
    }

    return status;
}

/** Create the session, report the result to stdout, and return the status code. */
QStatus CreateSession(TransportMask mask)
{
    SessionOpts opts(SessionOpts::TRAFFIC_MESSAGES, false, SessionOpts::PROXIMITY_ANY, mask);
    SessionPort sp = SERVICE_PORT;
    QStatus status = s_msgBus->BindSessionPort(sp, opts, s_busListener);

    if (ER_OK == status) {
        printf("BindSessionPort succeeded.\n");
    }
    else {
        printf("BindSessionPort failed (%s).\n", QCC_StatusText(status));
    }

    return status;
}

/** Advertise the service name, report the result to stdout, and return the status code. */
QStatus AdvertiseName(TransportMask mask)
{
    QStatus status = s_msgBus->AdvertiseName(SERVICE_NAME, mask);

    if (ER_OK == status) {
        printf("Advertisement of the service name '%s' succeeded.\n", SERVICE_NAME);
    }
    else {
        printf("Failed to advertise name '%s' (%s).\n", SERVICE_NAME, QCC_StatusText(status));
    }

    return status;
}

/** Request the service name, report the result to stdout, and return the status code. */
QStatus RequestName(void)
{
    const uint32_t flags = DBUS_NAME_FLAG_REPLACE_EXISTING | DBUS_NAME_FLAG_DO_NOT_QUEUE;
    QStatus status = s_msgBus->RequestName(SERVICE_NAME, flags);

    if (ER_OK == status) {
        printf("RequestName('%s') succeeded.\n", SERVICE_NAME);
    }
    else {
        printf("RequestName('%s') failed (status=%s).\n", SERVICE_NAME, QCC_StatusText(status));
    }

    return status;
}

EnergyService* testObj = NULL;

/** Main entry point */
int InitializeEnergyService(char* logFile)
{
    if (AllJoynInit() != ER_OK) {
        return 1;
    }
#ifdef ROUTER
    if (AllJoynRouterInit() != ER_OK) {
        AllJoynShutdown();
        return 1;
    }
#endif

    //printf("AllJoyn Library version: %s.\n", ajn::GetVersion());
    //printf("AllJoyn Library build info: %s.\n", ajn::GetBuildInfo());

    QStatus status = ER_OK;

    /* Create message bus */
    s_msgBus = new BusAttachment("Efergy", true);

    if (s_msgBus) {
        if (ER_OK == status) {
            status = CreateInterface();
        }

        if (ER_OK == status) {
            s_msgBus->RegisterBusListener(s_busListener);
        }

        if (ER_OK == status) {
            status = StartMessageBus();
        }

        testObj = new EnergyService(*s_msgBus, SERVICE_PATH, logFile);

        if (ER_OK == status) {
            status = RegisterBusObject(testObj);
        }

        if (ER_OK == status) {
            status = ConnectBusAttachment();
        }

        /*
        * Advertise this service on the bus.
        * There are three steps to advertising this service on the bus.
        * 1) Request a well-known name that will be used by the client to discover
        *    this service.
        * 2) Create a session.
        * 3) Advertise the well-known name.
        */
        if (ER_OK == status) {
            status = RequestName();
        }

        const TransportMask SERVICE_TRANSPORT_TYPE = TRANSPORT_ANY;

        if (ER_OK == status) {
            status = CreateSession(SERVICE_TRANSPORT_TYPE);
        }

        if (ER_OK == status) {
            status = AdvertiseName(SERVICE_TRANSPORT_TYPE);
        }

    }
    else {
        status = ER_OUT_OF_MEMORY;
    }

    return status;
}


int TerminateEnergyService()
{
    if (s_msgBus != NULL) {
        delete s_msgBus;
        s_msgBus = NULL;
        delete testObj;
        testObj = NULL;
    }
    printf("EnergyService exiting.\n");

#ifdef ROUTER
    AllJoynRouterShutdown();
#endif
    AllJoynShutdown();
    return 0;
}
