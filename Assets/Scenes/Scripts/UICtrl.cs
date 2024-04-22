using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UICtrl : MonoBehaviour {

    public Slider sliderCatAmplitude;
    public Slider sliderCatEaseIn;
    public Slider sliderCatHold;
    public Slider sliderCatEaseOut;
    public TMP_Dropdown dropdownCategory;

    public Slider sliderPADEaseIn;
    public Slider sliderPADHold;
    public Slider sliderPADEaseOut;
    public Slider sliderPleasure;
    public Slider sliderArousal;
    public Slider sliderDominance;

    public GameObject goCharacter;
    
    private FaceController faceCtrl;

    void Start() {
        faceCtrl = goCharacter.GetComponent<FaceController>();
    }

    public void button_sendCategorical() {
        float fWeight = sliderCatAmplitude.value;
        float timeIn = sliderCatEaseIn.value;
        float timeHold = sliderCatHold.value;
        float timeOut = sliderCatEaseOut.value;
        string strEmotionName = dropdownCategory.options[dropdownCategory.value].text;

        faceCtrl.setCategoricalEmotion(strEmotionName, fWeight, timeIn, timeHold, timeOut);
    }


    public void button_sendPAD() {
        Debug.Log("Button PAD pressed");

        float timeIn = sliderPADEaseIn.value;
        float timeHold = sliderPADHold.value;
        float timeOut = sliderPADEaseOut.value;
        float pleasure = sliderPleasure.value;
        float arousal = sliderArousal.value;
        float dominance = sliderDominance.value;

        faceCtrl.setPAD2AUNorm(pleasure, arousal, dominance, timeIn, timeHold, timeOut);
        //faceCtrl.setPAD2AUNorm(pleasure, arousal, dominance);
    }

}
