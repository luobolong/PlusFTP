namespace Hani.Utilities
{
    internal static partial class ClientHelper
    {
        internal delegate void StateHandler();
        internal static event StateHandler OnConnecting, OnConnected, OnDisconnected;

        internal delegate void ClientHandler();
        internal static event ClientHandler OnLock, OnUnLock;

        internal static void Connecting()
        {
            if (OnConnecting != null) new StateHandler(OnConnecting)();
        }

        internal static void Connected()
        {
            if (OnConnected != null) new StateHandler(OnConnected)();
        }

        internal static void Disconnected()
        {
            if (OnDisconnected != null) new StateHandler(OnDisconnected)();
        }

        internal static void Lock()
        {
            if (OnLock != null) new ClientHandler(OnLock)();
        }

        internal static void UnLock()
        {
            if (OnUnLock != null) new ClientHandler(OnUnLock)();
        }
    }
}