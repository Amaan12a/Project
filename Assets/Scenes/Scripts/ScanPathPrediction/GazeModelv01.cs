using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.Features2dModule;

/* COMMENTS
- images: Advanced-> Non-Power of 2->None
*/


public class GazeModelv01 : MonoBehaviour {
    public GameObject goInput;
    public GameObject goOutputFront;
    public GameObject goOutputBack;
    //public GameObject goMarker;

    public bool DetectObjects;
    public bool DetectFaces;
    public bool DetectSaliency;


    [Header("Object Detection Options")]
    [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
    public string model = "yolox_tiny.onnx";

    [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
    public string config = "";

    [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
    public string classes = "coco.names";

    [TooltipAttribute("Confidence threshold.")]
    public float confThreshold = 0.25f;

    [TooltipAttribute("Non-maximum suppression threshold.")]
    public float nmsThreshold = 0.45f;

    [TooltipAttribute("Maximum detections per image.")]
    public int topK = 1000;

    [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
    private int inpWidth = 416; // TODO compute

    [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
    private int inpHeight = 416; // TODO compute



    //    /// <summary>
    //    /// The YOLOX ObjectDetector.
    //    /// </summary>

    //YOLOXObjectDetectorMod objectDetector; // yolox_tiny.onnx
    YOLOv7ObjectDetectorMod objectDetector; // seems to do  best
    //YOLOv4ObjectDetectorMod objectDetector;

    // ---------------
    YuNetV2FaceDetectorMod faceDetector;


    [Header("Face Detection Options")]
    public float scoreThresholdFace = 0.9f;
    public float nmsThresholdFace = 0.3f;
    public int topKFace = 5000;
    public string faceDetectionModel = "face_detection_yunet_2023mar.onnx"; // "OpenCVForUnity/dnn/face_detection_yunet_2023mar.onnx"
    string face_detection_model_filepath;
    // --------------

    //    [TooltipAttribute("Path to test input image.")]
    //--public string testInputImage;

    protected string classes_filepath;
    protected string config_filepath;
    protected string model_filepath;

    // -----------
    [Header("Saliency Detection Options")]
    public float OTSU_factor = 2.0f; // increase the automatic salience threshold to reduce number of blobs
    SaliencyDetector saliencyDetector;
    //------

    private List<KeyPoint> lstKeyPoints;

    Queue<Vector3> queueTargetPositions;

    ///  string weightFile_filepath = Path.Combine(Application.dataPath, "FaceConfigs", weightFile);

    void Awake() {

        imageChanged = new UnityEvent();
        imageChanged.AddListener(GazeUpdate);

        if (!string.IsNullOrEmpty(classes)) {
            //--classes_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + classes);
            classes_filepath = Path.Combine(Application.dataPath, "MLModels", classes);
            if (string.IsNullOrEmpty(classes_filepath)) { Debug.Log("The file:" + classes + " did not exist in the folder “Assets/MLModels/”."); }
        }
        if (!string.IsNullOrEmpty(config)) {
            //--config_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + config);
            config_filepath = Path.Combine(Application.dataPath, "MLModels", config);
            if (string.IsNullOrEmpty(config_filepath)) { Debug.Log("The file:" + config + " did not exist in the folder “Assets/MLModels/”."); }
        }
        if (!string.IsNullOrEmpty(model)) {
            //--model_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + model);
            model_filepath = Path.Combine(Application.dataPath, "MLModels", model);
            if (string.IsNullOrEmpty(model_filepath)) { Debug.Log("The file:" + model + " did not exist in the folder “Assets/MLModels/”."); }
        }


        
        //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
        Utils.setDebugMode(true);


        if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath)) {
            Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded.");
        } else {
            //objectDetector = new YOLOXObjectDetectorMod(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
            objectDetector = new YOLOv7ObjectDetectorMod(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
            //objectDetector = new YOLOv4ObjectDetectorMod(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
        }

        //face_detection_model_filepath = Utils.getFilePath(FACE_DETECTION_MODEL_FILENAME);
        face_detection_model_filepath = Path.Combine(Application.dataPath, "MLModels", faceDetectionModel);
        if (string.IsNullOrEmpty(face_detection_model_filepath)) { Debug.Log("The file:" + faceDetectionModel + " did not exist in the folder “Assets/MLModels/”."); }



        faceDetector = new YuNetV2FaceDetectorMod(face_detection_model_filepath, "", new Size(inpWidth, inpHeight), scoreThresholdFace, nmsThresholdFace, topKFace);
        //Debug.Log(face_detection_model_filepath);
        //Debug.Log("FACE_DETECTION_MODEL_FILENAME");
        //Debug.Log(FACE_DETECTION_MODEL_FILENAME);

        saliencyDetector = new SaliencyDetector(0.5f, new Size(inpWidth, inpHeight));

        lstKeyPoints = new List<KeyPoint>();

        queueTargetPositions = new();

        StartCoroutine(AnalyseImage());
    }



    IEnumerator AnalyseImage() {

        //Texture2D texture2DIn = new Texture2D();
        //Utils.textureToTexture2D(goInput.GetComponent<Renderer>().material.mainTexture, texture2DIn);

        Texture2D texture2DIn = goInput.GetComponent<Renderer>().material.mainTexture as Texture2D;
        int imgWidth = texture2DIn.width;
        int imgHeight = texture2DIn.height;
        Mat img = new Mat(imgHeight, imgWidth, CvType.CV_8UC3, new Scalar(0, 0, 0));

        Utils.texture2DToMat(texture2DIn, img);
        Mat results = new();
        lstKeyPoints.Clear();

        MatOfKeyPoint keypoints_objects = new();
        MatOfKeyPoint keypoints_faces = new();
        MatOfKeyPoint keypoints_saliency = new();

        // OBJECTS
        /* */
        if (DetectObjects) {
            results = objectDetector.infer(img);
            //objectDetector.visualize(img, results, true, false);
            keypoints_objects = ObjectDetectorResults2KeyPoints(results);
            lstKeyPoints.AddRange(keypoints_objects.toList());
        }
        /* */


        // FACES
        if (DetectFaces) {
            Mat faces = faceDetector.infer(img);
            //faceDetector.visualize(img, faces, true, false);
            List<Mat> expressions = new List<Mat>();
            keypoints_faces = FaceDetectorResults2KeyPoints(faces);
            lstKeyPoints.AddRange(keypoints_faces.toList());
        }


        // SALIENCY
        /* */
        if (DetectSaliency) {
            keypoints_saliency = saliencyDetector.detect(ref img, false);
            lstKeyPoints.AddRange(keypoints_saliency.toList());
        }


        if (DetectObjects) {
            // objectDetector.visualize(img, results, true, false);
            Features2d.drawKeypoints(img, keypoints_objects, img, new Scalar(0, 0, 255), Features2d.DrawMatchesFlags_DRAW_RICH_KEYPOINTS);
        }
        /* */

        // FACES
        if (DetectFaces) {
            //faceDetector.visualize(img, faces, true, false);
            Features2d.drawKeypoints(img, keypoints_faces, img, new Scalar(255.0, 0, 0), Features2d.DrawMatchesFlags_DRAW_RICH_KEYPOINTS);
        }

        // SALIENCY
        /* */
        if (DetectSaliency) {
            Features2d.drawKeypoints(img, keypoints_saliency, img, new Scalar(0, 255.0, 0), Features2d.DrawMatchesFlags_DRAW_RICH_KEYPOINTS);
        }



        Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGB24, false);
        Utils.matToTexture2D(img, texture);
        goOutputFront.GetComponent<Renderer>().material.mainTexture = texture;
        goOutputBack.GetComponent<Renderer>().material.mainTexture = texture;

        SaccadeBehaviour(img);


        yield return null;
    }




    private void AddSaccadePoint(float xNorm, float yNorm) {
        /* some drawing testing */
        Vector3 ptOnGO = new Vector3();
        ptOnGO.x = .5f - xNorm;// * goOutput.transform.localScale.x);
        ptOnGO.y = .5f - yNorm;// * goOutput.transform.localScale.y);
        ptOnGO.z = 0.0f;

        // Debug.Log("imgWidth: " + imgWidth + ", imgHeight" + imgHeight + ", pt.x: " + pt.x + ",  ptOnGO.x: " + ptOnGO.x);
        Vector3 ptGlobal = goOutputFront.transform.TransformPoint(ptOnGO); // vector is relative to parent centre: 0.5,0.5 = top left corner!
//        goMarker.transform.position = ptGlobal;

        queueTargetPositions.Enqueue(ptGlobal);

    }


    private void SaccadeBehaviour(Mat img) {

        queueTargetPositions.Clear();

        float maxKPSizeFraction = .5f;

        Texture2D texture2DIn = goInput.GetComponent<Renderer>().material.mainTexture as Texture2D;
        int imgWidth = texture2DIn.width;
        int imgHeight = texture2DIn.height;

        // This is the data strcuture we will use the the saccading
        List<Vector2> listKPVectors = new();

        foreach (KeyPoint kp in lstKeyPoints) {
            Point pt = kp.pt;
            float sz = kp.size;

            /* normalize : */
            pt.x = pt.x / imgWidth;
            pt.y = pt.y / imgHeight;
            sz = sz / (imgWidth * imgHeight); // now in fractions...

                AddSaccadePoint((float)(pt.x), (float)(pt.y));
//                yield return new WaitForSeconds(0.3f);
//            }

            // filter the KeyPoints by size and add coordinates to new data structure
            if (sz < maxKPSizeFraction) {
                listKPVectors.Add(new Vector2((float)pt.x, (float)pt.y));
            }
        }

        // Find point closest to the centre
        //#  dist_mat_centre = distance_matrix(pts_arr,img_center, p=2)
        List<float> dist_mat_centre = CalculateDistances(new Vector2(0.5f, 0.5f), listKPVectors);

        // Find clostest point
        //# min_idx = np.argmin(dist_mat_centre)
        int min_idx = dist_mat_centre.IndexOf(dist_mat_centre.Min());

        //# pt_start = pts_arr[min_idx]
        Vector2 pt_start = listKPVectors[min_idx];


        AddSaccadePoint((float)(pt_start.x), (float)(pt_start.y));
        

        //# pts_arr = np.delete(pts_arr, min_idx, axis=0)
        listKPVectors.RemoveAt(min_idx);

        //# SACCADE_AMPL = 60 # desired amplitude
        float SACCADE_AMPL = .3f; // unit length

        //# SACCADE_AMPL_RANGE = 15 # +/- percentage of variance on the amplitude
        float SACCADE_AMPL_RANGE = .1f;

        Vector2 pt_old;

        //# pt_old = pt_start
        pt_old = pt_start;

        dist_mat_centre = CalculateDistances(new Vector2(0.5f, 0.5f), listKPVectors);

        //foreach (float ff in dist_mat_centre) {
        //    Debug.Log("\t* " + ff);
        //};

        //# while len(listKPVectors) > 0:
        while (listKPVectors.Count() > 0)
        {
            //G et indices of distances that are within range
            //# index_in_range = np.where(np.logical_and(dist_vect>=SACCADE_AMPL-SACCADE_AMPL_RANGE, dist_vect<=SACCADE_AMPL+SACCADE_AMPL_RANGE))[0]
            List<int> index_in_range = GetIndicesInRange(dist_mat_centre, SACCADE_AMPL - SACCADE_AMPL_RANGE, SACCADE_AMPL + SACCADE_AMPL_RANGE);

            //Debug.Log("index_in_range: ");
            //foreach (int ii in index_in_range) {
            //    Debug.Log("\t" + ii + " " + dist_mat_centre[ii]);
            //};

            int sel_idx = 0;
            if (index_in_range.Count() > 0)
            {
                //#     idx = random.randint(0, len(index_in_range)-1)
                sel_idx = new System.Random().Next(0, index_in_range.Count() - 1);
            }
            else
            {
                //# else: # if we find no point in range, we pick a random index...
                //#     sel_idx = random.randint(0, len(dist_vect)-1)

                sel_idx = new System.Random().Next(0, dist_mat_centre.Count() - 1);
            }

            //# pt_next = pts_arr[sel_idx]
            Vector2 pt_next = listKPVectors[sel_idx];

            // # Debug.Log("NEXT POINT: " + pt_next)
            // # listKPVectors = np.delete(pts_arr, sel_idx, axis=0)
            listKPVectors.RemoveAt(sel_idx);

            // # dist_vect = distance_matrix(pts_arr,[pt_next], p=2)
            List<float> dist_vect = CalculateDistances(pt_next, listKPVectors);

            AddSaccadePoint(pt_next.x, pt_next.y);

            bool bVisualise = true;
            if (bVisualise)
            {

                Imgproc.line(img,
                    new Point(pt_old.x * imgWidth, pt_old.y * imgHeight),
                    new Point(pt_next.x * imgWidth, pt_next.y * imgHeight),
                    new Scalar(0, 255, 0), 2);
            }


            pt_old = pt_next;

        }
      


//        yield return null;
    }

    List<float> CalculateDistances(Vector2 pIn, List<Vector2> lstPoints) {
        List<float> lstDistances = new();
        foreach (Vector2 p in lstPoints) {
            lstDistances.Add(Mathf.Abs(Vector2.Distance(pIn, p)));
        }
        return lstDistances;
    }

    private List<int> GetIndicesInRange(List<float> lst, float minVal, float maxVal) {
        List<int> lstIndices = new();

        for (int ii = 0; ii < lst.Count(); ii++) {
            float ff = lst[ii];
            if (ff > minVal && ff < maxVal) {
                lstIndices.Add(ii);
            }
        }
        return lstIndices;

    }






    //void Update_() {
    // SACCADING NEED TO HAPPEN IN A NEW COROUTINE....


    //        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame()) {

    //            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

    //            if (objectDetector == null) {
    //                Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
    //                Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
    //            } else {
    //                Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

    //                //TickMeter tm = new TickMeter();
    //                //tm.start();

    //                Mat results = objectDetector.infer(bgrMat);

    //                //tm.stop();
    //                //Debug.Log("YOLOXObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

    //                Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

    //                objectDetector.visualize(rgbaMat, results, false, true);
    //            }

    //            Utils.matToTexture2D(rgbaMat, texture);
    //        }
    // }


    public static UnityEvent imageChanged;


    private void GazeUpdate() {
        Debug.Log("GazeModelv01:EventReceived");
        StartCoroutine(AnalyseImage());
    }

    private MatOfKeyPoint ObjectDetectorResults2KeyPoints(Mat results) {

        List<KeyPoint> lstKPs = new List<KeyPoint>();


        for (int i = results.rows() - 1; i >= 0; --i) {
            float[] box = new float[4];
            results.get(i, 0, box);
            //float[] conf = new float[1];
            //results.get(i, 4, conf);
            //float[] cls = new float[1];
            //results.get(i, 5, cls);

            float left = box[0];
            float top = box[1];
            float right = box[2];
            float bottom = box[3];
            float box_width = right - left;
            float box_height = bottom - top;

            float box_size = (float)(Mathf.Sqrt(box_width * box_height / Mathf.PI) * 2.0);

            KeyPoint kp = new KeyPoint(
                left + box_width / 2,
                top + box_height / 2,
                box_size);

            lstKPs.Add(kp);

        }

        MatOfKeyPoint keypoints = new MatOfKeyPoint();
        keypoints.fromList(lstKPs);
        return keypoints;
    }


    private MatOfKeyPoint FaceDetectorResults2KeyPoints(Mat results) {

        List<KeyPoint> lstKPs = new List<KeyPoint>();


        for (int i = results.rows() - 1; i >= 0; --i) {
            float[] box = new float[4];
            results.get(i, 0, box);
            float[] conf = new float[1];
            results.get(i, 14, conf);
            float[] landmarks = new float[10];
            results.get(i, 4, landmarks);

            float left = box[0];
            float top = box[1];
            float box_width = box[2];
            float box_height = box[3];

            float box_size = (float)(Mathf.Sqrt(box_width * box_height / Mathf.PI) * 2.0);

            /* we don't need to add the face itself
            KeyPoint kp = new KeyPoint(
                left + box_width / 2,
                top + box_height / 2,
                box_size);

            lstKPs.Add(kp);
            */


            int lmSize = 10;
            lstKPs.Add(new KeyPoint(landmarks[0], landmarks[1], lmSize)); // right eye
            lstKPs.Add(new KeyPoint(landmarks[2], landmarks[3], lmSize)); // left eye
            lstKPs.Add(new KeyPoint(landmarks[4], landmarks[5], lmSize)); // nose tip
            lstKPs.Add(new KeyPoint((landmarks[6] + landmarks[8]) / 2,
                (landmarks[7] + landmarks[9]) / 2,
                lmSize)); // centre of mouth

        }

        MatOfKeyPoint keypoints = new MatOfKeyPoint();
        keypoints.fromList(lstKPs);
        return keypoints;
    }


}
