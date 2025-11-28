using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        EndSelection();
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    public async void Host()
    {
        print (await StartHostWithRelay(2, "udp"));
        EndSelection();
    }
    public void Client()
    {
        NetworkManager.Singleton.StartClient();
        EndSelection();
    }
    private void EndSelection()
    {
        gameObject.SetActive(false);
    }
}
