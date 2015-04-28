#include "pch.h"
#include "EnergyClient.h"
#include <thread>

using namespace EnergyHub;
using namespace Platform;
using namespace std;
using namespace qcc;
using namespace ajn;

/*constants*/
static const char* INTERFACE_NAME = "org.alljoyn.Bus.energy";
static const char* SERVICE_NAME = "org.alljoyn.Bus.energy";
static const char* SERVICE_PATH = "/energy";
static const SessionPort SERVICE_PORT = 25;

/** AllJoynListener receives discovery events from AllJoyn */
class MyBusListener : public BusListener, public SessionListener {
private:
    EnergyClient^ client;
    qcc::String s_sessionHost;
public:
    MyBusListener(EnergyClient^ client)
    {
        this->client = client;
    }

    void FoundAdvertisedName(const char* name, TransportMask transport, const char* namePrefix)
    {
        if (0 == strcmp(name, SERVICE_NAME) && s_sessionHost.empty()) {
            printf("FoundAdvertisedName(name='%s', transport = 0x%x, prefix='%s')\n", name, transport, namePrefix);

            /* We found a remote bus that is advertising basic service's well-known name so connect to it. */
            /* Since we are in a callback we must enable concurrent callbacks before calling a synchronous method. */
            s_sessionHost = name;
            client->g_msgBus->EnableConcurrentCallbacks();
            SessionOpts opts(SessionOpts::TRAFFIC_MESSAGES, false, SessionOpts::PROXIMITY_ANY, TRANSPORT_ANY);
            QStatus status = client->g_msgBus->JoinSession(name, SERVICE_PORT, this, client->s_sessionId, opts);
            if (ER_OK == status) {
                printf("JoinSession SUCCESS (Session id=%d).\n", client->s_sessionId);
            }
            else {
                printf("JoinSession failed (status=%s).\n", QCC_StatusText(status));
            }
            client->s_joinComplete = true;
        }
    }

    void NameOwnerChanged(const char* busName, const char* previousOwner, const char* newOwner)
    {
        if (newOwner && (0 == strcmp(busName, SERVICE_NAME))) {
            printf("NameOwnerChanged: name='%s', oldOwner='%s', newOwner='%s'.\n",
                busName,
                previousOwner ? previousOwner : "<none>",
                newOwner ? newOwner : "<none>");
        }
    }
};

EnergyClient::EnergyClient()
{
}

EnergyClient::~EnergyClient()
{
    Close();
}

void EnergyClient::Close()
{
    if (!s_terminated)
    {
        s_terminated = true;

        if (g_msgBus != NULL && busListener != NULL)
        {
            g_msgBus->UnregisterBusListener(*busListener);
            delete busListener;
            /* Deallocate bus */
            delete g_msgBus;
            g_msgBus = NULL;
        }

        //    printf("Basic client exiting with status 0x%04x (%s).\n", status, QCC_StatusText(status));

#ifdef ROUTER
        AllJoynRouterShutdown();
#endif
        AllJoynShutdown();
    }
}


/** Main entry point */
int EnergyClient::InitializeAlljoynClient()
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

    /* Create message bus. */
    g_msgBus = new BusAttachment("myApp", true);

    /* This test for NULL is only required if new() behavior is to return NULL
    * instead of throwing an exception upon an out of memory failure.
    */
    if (!g_msgBus) {
        status = ER_OUT_OF_MEMORY;
    }

    if (ER_OK == status) {
        status = CreateInterface();
    }

    if (ER_OK == status) {
        status = StartMessageBus();
    }

    if (ER_OK == status) {
        status = ConnectToBus();
    }

    if (ER_OK == status) {
        RegisterBusListener();
        status = FindAdvertisedName();
    }

    if (ER_OK == status) {
        status = WaitForJoinSessionCompletion();
    }

    return (int)status;
}


/** Create the interface, report the result to stdout, and return the result status. */
QStatus EnergyClient::CreateInterface(void)
{
    /* Add org.alljoyn.Bus.method_sample interface */
    InterfaceDescription* testIntf = NULL;
    QStatus status = g_msgBus->CreateInterface(INTERFACE_NAME, testIntf);

    if (status == ER_OK) {
        printf("Interface '%s' created.\n", INTERFACE_NAME);
        testIntf->AddMethod("read", "", "s", "outStr", 0);
        testIntf->Activate();
    }
    else {
        printf("Failed to create interface '%s'.\n", INTERFACE_NAME);
    }

    return status;
}

/** Start the message bus, report the result to stdout, and return the result status. */
QStatus EnergyClient::StartMessageBus(void)
{
    QStatus status = g_msgBus->Start();

    if (ER_OK == status) {
        printf("BusAttachment started.\n");
    }
    else {
        printf("BusAttachment::Start failed.\n");
    }

    return status;
}

/** Handle the connection to the bus, report the result to stdout, and return the result status. */
QStatus EnergyClient::ConnectToBus(void)
{
    QStatus status = g_msgBus->Connect();

    if (ER_OK == status) {
        printf("BusAttachment connected to '%s'.\n", g_msgBus->GetConnectSpec().c_str());
    }
    else {
        printf("BusAttachment::Connect('%s') failed.\n", g_msgBus->GetConnectSpec().c_str());
    }

    return status;
}

/** Register a bus listener in order to get discovery indications and report the event to stdout. */
void EnergyClient::RegisterBusListener(void)
{
    /* Static bus listener */
    busListener = new MyBusListener(this);

    g_msgBus->RegisterBusListener(*busListener);
    printf("BusListener Registered.\n");
}

/** Begin discovery on the well-known name of the service to be called, report the result to
stdout, and return the result status. */
QStatus EnergyClient::FindAdvertisedName(void)
{
    /* Begin discovery on the well-known name of the service to be called */
    QStatus status = g_msgBus->FindAdvertisedName(SERVICE_NAME);

    if (status == ER_OK) {
        printf("org.alljoyn.Bus.FindAdvertisedName ('%s') succeeded.\n", SERVICE_NAME);
    }
    else {
        printf("org.alljoyn.Bus.FindAdvertisedName ('%s') failed (%s).\n", SERVICE_NAME, QCC_StatusText(status));
    }

    return status;
}

/** Wait for join session to complete, report the event to stdout, and return the result status. */
QStatus EnergyClient::WaitForJoinSessionCompletion(void)
{
    unsigned int count = 0;

    while (!s_joinComplete && !s_terminated) {
        if (0 == (count++ % 10)) {
            printf("Waited %u seconds for JoinSession completion.\n", count / 10);
        }

#ifdef _WIN32
        Sleep(100);
#else
        usleep(100 * 1000);
#endif
    }

    return s_joinComplete  ? ER_OK : ER_ALLJOYN_JOINSESSION_REPLY_CONNECT_FAILED;
}


Platform::String^ EnergyClient::ReadLog()
{

    ProxyBusObject remoteObj(*g_msgBus, SERVICE_NAME, SERVICE_PATH, s_sessionId);
    const InterfaceDescription* alljoynTestIntf = g_msgBus->GetInterface(INTERFACE_NAME);

    assert(alljoynTestIntf);
    remoteObj.AddInterface(*alljoynTestIntf);

    Message reply(*g_msgBus);
    //MsgArg inputs[2];
    //inputs[0].Set("s", "Hello ");
    //inputs[1].Set("s", "World!");

    QStatus status = remoteObj.MethodCall(INTERFACE_NAME, "read", NULL, 0, reply, 5000);

    if (ER_OK == status) {
        /*printf("'%s.%s' (path='%s') returned '%s'.\n", SERVICE_NAME, "cat",
            SERVICE_PATH, reply->GetArg(0)->v_string.str);*/
        ajn::AllJoynString line = reply->GetArg(0)->v_string;
        return ToString(line.str, line.len );
    }
    else {
        //printf("MethodCall on '%s.%s' failed.", SERVICE_NAME, "cat");
    }

    return L"error: " + ((int)status).ToString(); //  (int)status;
}

Platform::String^ EnergyClient::ToString(const char* line, int len)
{
    Platform::String^ result = nullptr;
    int chars = MultiByteToWideChar(CP_ACP, 0, line, len, nullptr, 0);
    if (chars > 0)
    {
        wchar_t* buf = new wchar_t[chars + 1];
        MultiByteToWideChar(CP_ACP, 0, line, len, buf, chars);
        buf[chars] = '\0';
        result = ref new Platform::String(buf);
        delete buf;
    }
    return result;
}
