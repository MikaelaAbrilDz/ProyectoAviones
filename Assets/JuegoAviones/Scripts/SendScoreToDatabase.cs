
    
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SendMatchToDatabase : MonoBehaviour
{
    public string url = "http://localhost/unity_api/php/update_score.php";

    public void SendMatch(int matchNumber, string player1, string player2, int score1, int score2)
    {
        StartCoroutine(SendMatchCoroutine(matchNumber, player1, player2, score1, score2));
    }

    IEnumerator SendMatchCoroutine(int matchNumber, string player1, string player2, int score1, int score2)
    {
        WWWForm form = new WWWForm();

        form.AddField("numeroPartida", matchNumber);
        form.AddField("jugador1", player1);
        form.AddField("jugador2", player2);
        form.AddField("score1", score1);
        form.AddField("score2", score2);

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error: " + www.error);
        }
        else
        {
            Debug.Log("Servidor: " + www.downloadHandler.text);
        }
    }
}