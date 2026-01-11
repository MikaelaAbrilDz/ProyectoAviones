using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OnlineResetUI : MonoBehaviour
{
    public GameObject button;
    public void PlayAgain()
    {
        PlayerControllerOnline[] players = FindObjectsByType<PlayerControllerOnline>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlayerControllerOnline p in players)
        {
            if (p.IsOwner) p.RestartGame();
        }
    }
    public void Giveup()
    {
        EndGameRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    void EndGameRpc()
    {
        StartCoroutine(EndGameCo());
        CloseGameRpc();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    void CloseGameRpc()
    {
        SceneManager.LoadScene(0);
    }
    IEnumerator EndGameCo()
    {
        WWWForm form = new WWWForm();
        form.AddField("player_id", Accounts.id);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/unity_api/add_match.php", form))
        {
            yield return www.SendWebRequest();
        }
        NetworkManager.Singleton.Shutdown();
    }
}
