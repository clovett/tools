#pragma once
#include <qcc/platform.h>

#include <assert.h>
#include <signal.h>
#include <stdio.h>
#include <vector>

#include <qcc/String.h>

#include <alljoyn/AllJoynStd.h>
#include <alljoyn/BusAttachment.h>
#include <alljoyn/Init.h>
#include <alljoyn/Status.h>
#include <alljoyn/version.h>

using namespace std;
using namespace qcc;
using namespace ajn;
class MyBusListener;

namespace EnergyHub
{
    public ref class EnergyClient sealed
    {
        ~EnergyClient();
    public:
        EnergyClient();

        int InitializeAlljoynClient();
        void Close();

        Platform::String^ ReadLog();

    private:
        Platform::String^ ToString(const char* line, int len);

        QStatus WaitForJoinSessionCompletion(void);
        QStatus MakeMethodCall(void);
        QStatus FindAdvertisedName(void);
        void RegisterBusListener(void);
        QStatus ConnectToBus(void);
        QStatus StartMessageBus(void);
        QStatus CreateInterface(void);

        /** Static top level message bus object */
        BusAttachment* g_msgBus = NULL;
        BusListener* busListener = NULL;
        bool s_joinComplete = false;
        bool s_terminated = false;
        SessionId s_sessionId = 0;

        friend class MyBusListener;
    };
}