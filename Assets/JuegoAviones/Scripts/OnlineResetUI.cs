using UnityEngine;

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
}
