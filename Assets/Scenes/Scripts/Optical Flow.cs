using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.VideoModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.Features2dModule;
using System.Collections.Generic;
using System.Collections;

public class OpticalFlowWithBlobDetection : MonoBehaviour
{
    public RawImage opticalFlowRawImage; // UI RawImage to display the optical flow visualization
    public RawImage blobRawImage; // UI RawImage to display the blob detection visualization
    public GameObject redBall; // Reference to your red ball GameObject

    private VideoCapture capture;
    private Mat prevGray;
    private MatOfKeyPoint keypoints;
    private SimpleBlobDetector detector;
    private bool isBlobDetectionDone = false;

    private Vector3 v3GazePosition;
    private float lookIKWeight;

    void Start()
    {
        capture = new VideoCapture(Application.streamingAssetsPath + "/videoEASports.mp4");
        if (!capture.isOpened())
        {
            Debug.LogError("Error: Couldn't open the video file.");
            return;
        }

        prevGray = new Mat(); // Initialize the previous grayscale frame

        // Create blob detector
        SimpleBlobDetector_Params parameters = new SimpleBlobDetector_Params();
        parameters.set_filterByArea(true);
        parameters.set_filterByInertia(false);
        parameters.set_filterByCircularity(false);
        parameters.set_filterByConvexity(false);
        parameters.set_maxThreshold(150);
        parameters.set_minThreshold(0);
        parameters.set_minArea(200f);
        detector = SimpleBlobDetector.create(parameters);

        StartCoroutine("ComputeOpticalFlowAndBlobDetection");
    }

    private IEnumerator ComputeOpticalFlowAndBlobDetection()
    {
        Mat frame = new Mat(); // Initialize a new Mat object to store the current frame
        while (true)
        {
            capture.read(frame); // Read a frame from the camera
            if (frame.empty()) yield return null; // Return if the frame is empty

            // Convert the frame to grayscale
            Mat gray = new Mat();
            Imgproc.cvtColor(frame, gray, Imgproc.COLOR_BGR2GRAY);

            if (prevGray.empty())
            {
                prevGray = gray.clone(); // Copy the grayscale frame to prevGray if it's empty
                yield return null;
            }

            // Compute the dense optical flow
            Mat flow = new Mat();
            Video.calcOpticalFlowFarneback(prevGray, gray, flow, 0.5, 3, 15, 3, 5, 1.2, 0);

            // Visualization of optical flow
            Mat flowVis = DrawOpticalFlow(flow);
            Texture2D opticalFlowTexture = new Texture2D(flowVis.cols(), flowVis.rows(), TextureFormat.RGB24, false); // Create a new Texture2D object
            Utils.matToTexture2D(flowVis, opticalFlowTexture); // Convert the Mat object to Texture2D
            opticalFlowRawImage.texture = opticalFlowTexture; // Set the RawImage texture to display the optical flow visualization

            // Detect blobs if it's not done yet
            //if (!isBlobDetectionDone)
            // {
            keypoints = new MatOfKeyPoint();
            detector.detect(frame, keypoints);
            isBlobDetectionDone = true;

            // Update red ball position and movement based on blob position
            UpdateRedBallPosition(keypoints);
            // }

            // Visualization of blob detection
            Mat blobImage = new Mat();
            Features2d.drawKeypoints(flowVis, keypoints, blobImage, new Scalar(0, 255, 0), Features2d.DrawMatchesFlags_DRAW_RICH_KEYPOINTS);
            Texture2D blobTexture = new Texture2D(blobImage.cols(), blobImage.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(blobImage, blobTexture);
            blobRawImage.texture = blobTexture;

            prevGray = gray.clone(); // Update prevGray with the current grayscale frame
            yield return null;
        }
    }

    protected virtual Mat DrawOpticalFlow(Mat flow)
    {
        List<Mat> flow_parts = new List<Mat>(2);
        Core.split(flow, flow_parts);
        Mat magnitude = new Mat();
        Mat angle = new Mat();
        Mat magn_norm = new Mat();
        Core.cartToPolar(flow_parts[0], flow_parts[1], magnitude, angle, true);
        Core.normalize(magnitude, magn_norm, 0.0f, 1.0f, Core.NORM_MINMAX);
        angle *= ((1f / 360f) * (180f / 255f));
        // Build hsv image
        Mat hsv = new Mat();
        Mat hsv8 = new Mat();
        Mat flowVis = new Mat();
        List<Mat> _hsv = new List<Mat>(new Mat[3]);
        _hsv[0] = angle;
        _hsv[1] = Mat.ones(angle.size(), CvType.CV_32F);
        _hsv[2] = magn_norm;
        Core.merge(_hsv, hsv);
        hsv.convertTo(hsv8, CvType.CV_8U, 255.0);
        Imgproc.cvtColor(hsv8, flowVis, Imgproc.COLOR_HSV2BGR);
        return flowVis;
    }
    private void UpdateRedBallPosition(MatOfKeyPoint keypoints)
    {
        Debug.Log("UpdateRedBallPosition");
        KeyPoint[] points = keypoints.toArray();
        List<KeyPoint> lstKP = keypoints.toList();
        Debug.Log(keypoints.toList().Count);
        if (points.Length > 0)
        {
            // Find the nearest blob to the red ball
            Debug.Log("moveball");
            KeyPoint nearestBlob = FindNearestBlob(points);
            Vector3 newPosition = new Vector3((float)nearestBlob.pt.x, (float)nearestBlob.pt.y, -1781f);
            redBall.transform.position = newPosition;

        }
    }

    private KeyPoint FindNearestBlob(KeyPoint[] blobs)
    {
        KeyPoint nearestBlob = blobs[0];
        float minDistance = Vector2.Distance(new Vector2((float)nearestBlob.pt.x, (float)nearestBlob.pt.y), new Vector2(redBall.transform.position.x, redBall.transform.position.y));

        foreach (KeyPoint blob in blobs)
        {
            float distance = Vector2.Distance(new Vector2((float)blob.pt.x, (float)blob.pt.y), new Vector2(redBall.transform.position.x, redBall.transform.position.y));
            if (distance < minDistance)
            {
                nearestBlob = blob;
                minDistance = distance;
            }
        }

        return nearestBlob;
    }


    void OnDestroy()
    {
        capture.release(); // Release the VideoCapture object when the script is destroyed
    }
}