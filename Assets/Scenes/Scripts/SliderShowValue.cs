using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/* Attach to slider
 * - need TMP_Text child named "Text"
 */

public class SliderShowValue : MonoBehaviour {
    TMP_Text txtValue;

    void Awake() {
        Transform childTrans = gameObject.transform.Find("Text");
        GameObject go = childTrans.gameObject;
        txtValue = go.GetComponent<TMP_Text>();
        txtValue.text = "voila";
        txtValue.text = GetComponent<Slider>().value.ToString();

    }

    public void textUpdate(float value) {
        txtValue.text = value.ToString("F1");
    }
}