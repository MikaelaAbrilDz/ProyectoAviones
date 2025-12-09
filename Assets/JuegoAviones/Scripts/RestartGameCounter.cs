using Unity.Netcode;
using UnityEngine;

public class RestartGameCounter : NetworkBehaviour
{
    [SerializeField] GameObject replayCanvas;
    [HideInInspector] private NetworkVariable<int> restartCounter = new NetworkVariable<int>(0);

    public int _restartCounter
    {
        get { return restartCounter.Value; }

        set 
        {
            restartCounter.Value = value;
            if (restartCounter.Value >= 2 && replayCanvas.activeInHierarchy)
            {
                ApplyToAllClientRpc();
            }
        }
    }
    [ClientRpc (RequireOwnership = false)]
    void ApplyToAllClientRpc()
    {
        replayCanvas.SetActive(false);
        Time.timeScale = 1;
    }
}
