using UnityEngine;
using Unity.Netcode;

public class EndGameManager : MonoBehaviour
{
    public ScoreManager scoreManager;
    public SendMatchToDatabase database;

    public int matchNumber = 1;

    public void EndGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int score1 = scoreManager.player1Score.Value;
        int score2 = scoreManager.player2Score.Value;

        database.SendMatch(
            matchNumber,
            "Player1",
            "Player2",
            score1,
            score2
        );

        matchNumber++;

        Debug.Log("Partida enviada a la base de datos");
    }
}