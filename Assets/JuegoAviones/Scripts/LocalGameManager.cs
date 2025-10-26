using UnityEngine;
using UnityEngine.InputSystem;

public class LocalGameManager : MonoBehaviour
{
    int joined = 1;
    public void PlayerJoin(PlayerInput input)
    {
        if (joined == 1)
        {
            input.GetComponent<PlayerControllerLocal>().AtJoining(Unity.Cinemachine.OutputChannels.Channel01);
            joined = 2;
        }
        else if (joined == 2)
        {
            input.GetComponent<PlayerControllerLocal>().AtJoining(Unity.Cinemachine.OutputChannels.Channel02);
            joined = 3;
        }
    }
}
