using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI resultText;

    public GameObject canvasToDeactivate;
   

    public void StartLogin()
    {
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", usernameField.text);
        form.AddField("password", passwordField.text);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/unity_api/login.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                resultText.text = "Error: " + www.error;
            }
            else
            {
                string responseText = www.downloadHandler.text;
                print(responseText);
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(responseText);
                if (responseText.Contains("success"))
                {
                    resultText.text = "Login successful!";
                    Accounts.id = response.id;
                    yield return new WaitForSeconds(0.5f);
                    canvasToDeactivate.SetActive(false);
                }
                else
                {
                    resultText.text = "Login failed!";
                }
            }
        }
    }
}

public class LoginResponse
{
    public string status;
    public int id;
}
