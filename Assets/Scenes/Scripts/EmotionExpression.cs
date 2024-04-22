using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[System.Serializable]
public class MyFloatEvent : UnityEvent<float, float> {
}


public class EmotionExpression : MonoBehaviour {

    public static MyFloatEvent imageChanged;

    void Awake() {

        imageChanged = new MyFloatEvent();
        imageChanged.AddListener(ExpressEmotion);

    }

    private void ExpressEmotion(float fv, float fa) {
        Debug.Log("EmotionExpression:EventReceived");
        FaceController FC = GetComponent<FaceController>();
        FC.setPAD2AUNorm(fv, fa, 0.5f, .2f, 2f, .2f);

    }

    // Update is called once per frame
    void Update() {

    }
}
