using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageChanger : MonoBehaviour {

    public int Interval = 3;
    public GameObject ImageHolder;
    public Texture2D[] Textures;

    private int counter;



    void Start() {
        counter = 0;
        StartCoroutine(ChangeImage());
    }

    IEnumerator ChangeImage() {
        WaitForSeconds waitTime = new WaitForSeconds(Interval);

        while (true) {
            counter++;
            if (counter >= Textures.Length) {
                counter = 0;
            }

            Texture2D texture = Textures[counter];
            ImageHolder.GetComponent<Renderer>().material.mainTexture = texture;

            GazeModelv01.imageChanged.Invoke(); //yay, this works

            yield return waitTime;
        }
    }
}


