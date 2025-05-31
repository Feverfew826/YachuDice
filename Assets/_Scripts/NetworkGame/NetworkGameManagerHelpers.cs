using System.Linq;

using Unity.Multiplayer.Playmode;
using Unity.Netcode;

using UnityEngine;

internal static class NetworkGameManagerHelpers
{

    public static void AutoConnectIfNotStartedNetwork(NetworkManager networkManager)
    {
        if (networkManager.IsHost == false && networkManager.IsClient == false)
        {
            var mppmTag = CurrentPlayer.ReadOnlyTags();
            if (mppmTag.Contains("Host"))
            {
                networkManager.StartHost();
            }
            else if (mppmTag.Contains("Client"))
            {
                networkManager.StartClient();
            }
        }
    }

    public static void ShowNetworkStatus()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        StatusLabels();
        GUILayout.EndArea();
    }

    private static void StatusLabels()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        var mode = "None";
        if (networkManager.IsHost)
        {
            mode = "Host";
        }
        else if (networkManager.IsClient)
        {
            mode = "Client";
        }

        GUILayout.Label("Transport: " + networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}