using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ControlVLC : MonoBehaviour {

    public string VLCHost = "10.70.1.174";
    public string VLCPort = "8080";
    public string VLCPassword = "nopass";

    void Start() {
        StartCoroutine(TogglePauseVLC());
    }

    IEnumerator TogglePauseVLC() {
        WaitForSeconds waitTime = new WaitForSeconds(2f); //Do the memory allocation once
        string auth = ":" + VLCPassword;
        auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
        auth = "Basic " + auth;

        yield return waitTime;
        string url = "http://" + VLCHost + ":" + VLCPort + "/requests/status.xml?command=pl_pause";
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("AUTHORIZATION", auth);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
    }

}