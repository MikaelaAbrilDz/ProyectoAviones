using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
public class BuildingManagerOnline : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        //GetComponent<NetworkAnimator>().Animator = GetComponent<Animator>();
    }
    public void PlayParticles()
    {
        if (IsServer)
        {
            PlayParticlesRpc();
            return;
        }
            GetComponentInChildren<ParticleSystem>().Play();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void PlayParticlesRpc()
    {
        GetComponentInChildren<ParticleSystem>().Play();
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
