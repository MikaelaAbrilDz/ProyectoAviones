using Unity.Netcode;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void Host()
    {
        NetworkManager.Singleton.StartHost();
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
