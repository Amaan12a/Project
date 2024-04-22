using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Speech.Synthesis;



// None of the Syste.Speech.Synthesis stuff works in Unity...
// Instead use: https://chadweisshaar.com/blog/2015/07/02/microsoft-speech-for-unity/

public class TTSTest : MonoBehaviour {

 //   private SpeechSynthesizer synthesizer;
    
    void Start() {
 //       synthesizer = new ();
 //       synthesizer.SetOutputToDefaultAudioDevice();

    }


    public void Speak(){
        StartCoroutine(SpeakCR());
    }

    IEnumerator SpeakCR() {
        Debug.Log("SpeakCR");

        //        WindowsVoice.theVoice.speak("All we need to do is to make sure we keep talking");


        WindowsVoice.theVoice.test();

//        synthesizer.Speak("All we need to do is to make sure we keep talking");

        yield return null;
    }


        void Update() {

    }
}
