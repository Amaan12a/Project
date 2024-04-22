using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using Unity.VisualScripting.Dependencies.Sqlite;
using System;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEngine.UIElements;


/* 
using sqlite with unity
https://www.mongodb.com/developer/code-examples/csharp/saving-data-in-unity3d-using-sqlite/
 - download precom bin from https://sqlite.org/download.html
 - add files—sqlite3.def and sqlite3.dll—to the Plugin folder in your Unity project.
 - copy "C:\Program Files\Unity\Hub\Editor\2022.2.12f1\Editor\Data\MonoBleedingEdge\lib\mono\unityjit-win32\Mono.Data.Sqlite.dll" to Plugins
*/



public class ImgDBQuery : MonoBehaviour {

    private IDbConnection dbconn;
    public int Interval = 3;
    public GameObject ImageHolder;


    void Start() {
        // OASIS range 1:7(?)
        string dbUri = "URI=file:" + Application.dataPath + "/Image_database/OASIS_bygender_censored.sqlite";

        dbconn = (IDbConnection)new SqliteConnection(dbUri);
        dbconn.Open(); //Open connection to the database.

        StartCoroutine(ChangeImage());
    }

    IEnumerator ChangeImage() {
        WaitForSeconds waitTime = new WaitForSeconds(Interval);
        IDbCommand dbcmd = null;
        while (true) {

            for (int iv = 2; iv < 7; iv++) {
                for (int ia = 2; ia < 7; ia++) {
                    Debug.Log("**********NEXT IMAGE");
                    dbcmd = dbconn.CreateCommand();

                    string sqlCountQuery = "select count(*) from OASIS_bygender_censored where Valence_mean_men like \"" + iv + "%\" and Arousal_mean_men like \"" + ia + "%\"";
                    dbcmd.CommandText = sqlCountQuery;
                    IDataReader reader = dbcmd.ExecuteReader();
                    int icount = -99;
                    while (reader.Read()) {
                        icount = reader.GetInt32(0);
                    }
                    reader.Close();
                    reader = null;
                    //Debug.Log("Number of records: " + icount);

                    string Theme = "";
                    float Valence_mean_men = -99f;
                    float Arousal_mean_men = -99f;


                    if (icount > 0) {
                        // determine the random index of the image we will chose:
                        int iRandIndex = new System.Random().Next(0, icount - 1);
                        //Debug.Log("iRandIndex: " + iRandIndex);

                        //                    string sqlQuery = "select Theme, Valence_mean_men, Arousal_mean_men from OASIS_bygender where Valence_mean_men like \"3%\" and Arousal_mean_men like \"3%\"";
                        string sqlQuery = "select Theme, Valence_mean_men, Arousal_mean_men from OASIS_bygender_censored where Valence_mean_men like \"" + iv + "%\" and Arousal_mean_men like \"" + ia + "%\"";
                        //Debug.Log(sqlQuery);
                        dbcmd.CommandText = sqlQuery;
                        reader = dbcmd.ExecuteReader();



                        int ii = 0;
                        while (reader.Read()) {
                            if (ii == iRandIndex) {
                                Theme = reader.GetString(0).TrimEnd();
                                //Valence_mean_men = (float)Convert.ToDouble(reader.GetString(1));
                                Valence_mean_men = reader.GetFloat(1);
                                //Arousal_mean_men = (float)Convert.ToDouble(reader.GetString(2));
                                Arousal_mean_men = reader.GetFloat(2);

                                //Debug.Log("Theme= " + Theme + "  Valence_mean_men =" + Valence_mean_men + "  Arousal_mean_men =" + Arousal_mean_men);
                                break;
                            }
                            ii++;
                        }
                        reader.Close();
                        reader = null;
                    }

                    if (Theme.Length > 0) {
                        string imagePath = Application.dataPath + "/Image_database/images/" + Theme + ".jpg";
                        Debug.Log("Loading: " + imagePath);
                        Texture2D texture = new(2, 2); // arbitrary size
                        var rawData = System.IO.File.ReadAllBytes(imagePath);
                        texture.LoadImage(rawData);
                        ImageHolder.GetComponent<Renderer>().material.mainTexture = texture;
                        GazeModelv01.imageChanged.Invoke();
                        EmotionExpression.imageChanged.Invoke((Valence_mean_men - 1) / 6f, (Arousal_mean_men - 1) / 6f);
                    }
                    yield return waitTime;
                }
            }
        }
        dbcmd.Dispose();
        dbcmd = null;
        dbconn.Close();
        dbconn = null;
    }
}


