using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OnlineResetUI : NetworkBehaviour
{
    public GameObject button, button2, canvas;


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
       if (IsServer) CloseGameRpc();
        else EndGameRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    void EndGameRpc()
    {
        CloseGameRpc();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    void CloseGameRpc()
    {
        StartCoroutine(EndGameCo());
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
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(0);
    }
}
