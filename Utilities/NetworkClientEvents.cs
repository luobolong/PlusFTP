namespace Hani.Utilities
{
    internal abstract partial class NetworkClient
    {
        internal delegate void StatusHandler();
        internal event StatusHandler OnConnecting, OnConnected, OnDisconnected, OnFailedToConnect;

        protected void Connecting()
        {
            if (!DisplayEvents) return;
            if (OnConnecting != null) new StatusHandler(OnConnecting)();
        }

        protected void Connected()
        {
            if (!DisplayEvents) return;
            if (OnConnected != null) new StatusHandler(OnConnected)();
        }

        protected void Disconnected()
        {
            if (!DisplayEvents) return;
            if (OnDisconnected != null) new StatusHandler(OnDisconnected)();
        }

        protected void FailedToConnect()
        {
            if (!DisplayEvents) return;
            if (OnFailedToConnect != null) new StatusHandler(OnFailedToConnect)();
        }
    }
}