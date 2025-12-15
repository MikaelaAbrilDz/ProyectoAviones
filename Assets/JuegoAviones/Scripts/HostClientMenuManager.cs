using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class HostClientMenuManager : MonoBehaviour
{
    [SerializeField] GameObject hostPanel, clientPanel;
    [SerializeField] TextMeshProUGUI generatedCode;
    [SerializeField] TMP_InputField enteredCode;
    private void Update()
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2) EndSelection();        
    }
    public void OpenHostOptions()
    {
        hostPanel.SetActive(true);
    }
    public void OpenClientOptions()
    {
        clientPanel.SetActive(true);
    }
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
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }
    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
    public async void Host()
    {
        Time.timeScale = 0;
        generatedCode.text = await StartHostWithRelay(2, "udp");
    }
    public async void Client()
    {
        Time.timeScale = 0;
        await StartClientWithRelay(enteredCode.text, "udp");
    }
    private void EndSelection()
    {
        Time.timeScale = 1;
        gameObject.SetActive(false);
    }
}
