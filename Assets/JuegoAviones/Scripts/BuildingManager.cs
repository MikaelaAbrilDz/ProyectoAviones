using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
public class BuildingManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //GetComponent<NetworkAnimator>().Animator = GetComponent<Animator>();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void CollapseRpc()
    {
        GetComponent<Animator>().SetBool("Collapse", true);
    }
    public void Collapse()
    {
        if (IsServer)
        {
            CollapseRpc();
            return;
        }
        GetComponent<Animator>().SetBool("Collapse", true);
    }
}
