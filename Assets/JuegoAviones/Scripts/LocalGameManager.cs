using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LocalGameManager : MonoBehaviour
{
    int joined = 1;
    [SerializeField] Image[] playerIcons;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] GameObject canvas;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] LayerMask mainCamMask_p0;
    [SerializeField] LayerMask mainCamMask_p1;
    [SerializeField] LayerMask uiCamMask_p0;
    [SerializeField] LayerMask uiCamMask_p1;

    int _joined
    {
        get
        {
            return joined;
        }
        set
        {
            joined = value;

            if (joined == 3) 
            {
                StartCoroutine(GameStarter());
            }
        }
    }

    private void Start()
    {
        Time.timeScale = 0;
    }
    public void PlayerJoin(PlayerInput input)
    {
        if (_joined == 1)
        {
            input.GetComponent<PlayerControllerLocal>().AtJoining(Unity.Cinemachine.OutputChannels.Channel01, mainCamMask_p0, uiCamMask_p0, 0);
            input.GetComponent<Transform>().position = spawnPoints[0].position;
            input.GetComponent<Transform>().rotation = spawnPoints[0].rotation;
            playerIcons[0].color = Color.green;
            _joined = 2;
        }
        else if (_joined == 2)
        {
            input.GetComponent<PlayerControllerLocal>().AtJoining(Unity.Cinemachine.OutputChannels.Channel02, mainCamMask_p1, uiCamMask_p1, 1);
            input.GetComponent<Transform>().position = spawnPoints[1].position;
            input.GetComponent<Transform>().rotation = spawnPoints[1].rotation;
            playerIcons[1].color = Color.green;
            _joined = 3;
        }
    }
    private IEnumerator GameStarter()
    {
        text.text = "Starting game in 3";
        yield return new WaitForSecondsRealtime(0.3f);
        text.text = "Starting game in 2";
        yield return new WaitForSecondsRealtime(0.3f);
        text.text = "Starting game in 1";
        yield return new WaitForSecondsRealtime(0.3f);
        canvas.SetActive(false);
        Time.timeScale = 1;
    }
}
