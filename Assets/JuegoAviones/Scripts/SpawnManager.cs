using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] SpawnIndicator[] spawns;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SpawnPlayer();
    }
    private void Awake()
    {
        spawns = FindObjectsByType<SpawnIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
    private void SpawnPlayer()
    {
        foreach (var spawn in spawns)
        {
            if (spawn.gameObject.activeSelf)
            {
                transform.position = spawn.transform.position;
                transform.rotation = spawn.transform.rotation;
                spawn.gameObject.SetActive(false);
                break;
            }
        }
    }
    public void RespawnPlayer()
    {
        foreach (var spawn in spawns)
        {
            if (spawn.gameObject.activeSelf)
            {
                print("START RESPAWN");
                SetPositionClientRpc(spawn.transform.position, spawn.transform.rotation);
                spawn.gameObject.SetActive(false);
                print("END RESPAWN");
                break;
            }
        }
    }
    [ClientRpc (RequireOwnership = false)]
    void SetPositionClientRpc(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }
    public void ResetSpawns()
    {
        spawns = FindObjectsByType<SpawnIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var spawn in spawns)
        {
            spawn.gameObject.SetActive(true);
        }
    }
}
