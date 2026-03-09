using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SendScoreToDatabase : MonoBehaviour
{
    public string url = "http://localhost/juego_aviones/guardar_score.php";

    public void SendScore(string playerName, int score)
    {
        StartCoroutine(SendScoreCoroutine(playerName, score));
    }

    IEnumerator SendScoreCoroutine(string playerName, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("nombre", playerName);
        form.AddField("score", score);

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error: " + www.error);
        }
        else
        {
            Debug.Log("Respuesta servidor: " + www.downloadHandler.text);
        }
    }
}