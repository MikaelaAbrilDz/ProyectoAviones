using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] GameObject[] spawns;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled= false;
            return;
        }
        SpawnPlayer();
        base.OnNetworkSpawn();
    }
    private void SpawnPlayer()
    {
        spawns = GameObject.FindGameObjectsWithTag("Spawn");
        foreach (var spawn in spawns)
        {
            if (spawn.gameObject.activeSelf)
            {
                transform.position = spawn.transform.position;
                spawn.gameObject.SetActive(false);
                break;
            }
        }
    }
}
