using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ScoreManager : NetworkBehaviour
{
    public Text player1Text;
    public Text player2Text;

    public NetworkVariable<int> player1Score = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2Score = new NetworkVariable<int>(0);

    void Update()
    {
        player1Text.text = "Player 1: " + player1Score.Value;
        player2Text.text = "Player 2: " + player2Score.Value;
    }

    public void AddScore(ulong clientId)
    {
        // if (!IsServer) return;
        Debug.Log(clientId);
        if (clientId == 0)
            player1Score.Value += 10;
        else
            player2Score.Value += 10;
    }
}