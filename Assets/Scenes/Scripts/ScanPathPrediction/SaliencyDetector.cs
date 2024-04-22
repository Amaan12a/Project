using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.XimgprocModule;
using OpenCVForUnity.Features2dModule;


public class SaliencyDetector {

    Size input_size;
    int backend;
    int target;
    float OTSU_factor = 2.0f; // increase the automatic salience threshold to reduce number of blobs
    Mat input_sizeMat;

    public SaliencyDetector(float OTSU_factor, Size inputSize, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU) {
        input_size = new Size(inputSize.width > 0 ? inputSize.width : 320, inputSize.height > 0 ? inputSize.height : 320);
        this.backend = backend;
        this.target = target;
        this.OTSU_factor = OTSU_factor;



    }


    public Mat EdgeDetect(Mat image, bool bw = false) {
        // pyimagesearch.com / 2021 / 05 / 12 / image - gradients - with - opencv - sobel - and - scharr /

        if (bw) {
            Imgproc.cvtColor(image, image, Imgproc.COLOR_BGR2GRAY);
        }

        Mat results = new Mat();
        Mat gx = new Mat();
        Mat gy = new Mat();

        // dx = 1 and dy = 0 indicates that we want to compute the gradient across the x direction
        int ddepth = CvType.CV_32F;
        int ksize = 3;

        Imgproc.Sobel(image, gx, ddepth, 1, 0, ksize);
        Imgproc.Sobel(image, gy, ddepth, 0, 1, ksize);

        Core.convertScaleAbs(gx, gx, 0.5);
        Core.convertScaleAbs(gy, gy, 0.5);
        Core.addWeighted(gx, 0.5, gy, 0.5, 0, results, CvType.CV_8UC3);

        return results;
    }


    public Mat Dialate(Mat image) {
        // https://docs.opencv.org/3.4/db/df6/tutorial_erosion_dilatation.html
        int dilation_type = Imgproc.MORPH_ELLIPSE; // Imgproc.MORPH_CROSS; Imgproc.MORPH_RECT;
        int dilation_size = 2;

        Mat element = Imgproc.getStructuringElement(dilation_type, new Size(2 * dilation_size + 1, 2 * dilation_size + 1), new Point(dilation_size, dilation_size));
        Imgproc.dilate(image, image, element);

        return image;
    }

    public Mat LineDet(Mat image) {
        // https://docs.opencv.org/3.4/d9/db0/tutorial_hough_lines.html
        // https://enoxsoftware.github.io/OpenCVForUnity/3.0.0/doc/html/class_open_c_v_for_unity_1_1_imgproc_module_1_1_imgproc.html#abcd8ba49f26a5dfd22d8235e9165176e

        Mat linesP = new Mat();
        Mat dst = new Mat();
        Mat cdstP = new Mat();

        Imgproc.cvtColor(image, image, Imgproc.COLOR_BGR2GRAY);
        Imgproc.Canny(image, dst, 50, 200, 3);

        //image.convertTo(dst, CvType.CV_8UC1);
        Imgproc.HoughLinesP(dst, linesP, 1, Mathf.PI / 180, 50, 50, 10);



        //// Draw the lines
        for (int i = 0; i < linesP.rows(); i++) {
            Mat l = linesP.row(i);
            Debug.Log(l);
            //Imgproc.line(cdstP, new Point(l.get(0,0)[0], l[1]), new Point(l[2], l[3]), new Scalar(0, 0, 255), 3); //, LINE_AA);
        }

        //        results.convertTo(results, CvType.CV_8UC3);

        return image;

        // LATER


    }



    public Mat EdgeDetect2(Mat image) {
        Mat results = new Mat();
        string modelFile = Path.Combine(Application.dataPath, "MLModels", "model.yml.gz");
        StructuredEdgeDetection sed = Ximgproc.createStructuredEdgeDetection(modelFile);
        image.convertTo(image, CvType.CV_32FC3);
        sed.detectEdges(image, results); // crashed unity...
        results.convertTo(results, CvType.CV_8UC3);
        return results;
    }




    public Mat ContrastEnhance(Mat image) {
        Mat results = new Mat();
        double th = 80;
        double max_val = 255;


        //THRESH_OTSU wants src_type == CV_8UC1 || src_type == CV_16UC1'
        image.convertTo(image, CvType.CV_8UC1);

        Imgproc.threshold(image, results, th, max_val, Imgproc.THRESH_OTSU);
        results.convertTo(results, CvType.CV_8UC3);
        return results;
    }



    public Mat ThresholdImageAdaptive(Mat image) {
        Mat results = new Mat();
        // convert to grayscale
        Imgproc.cvtColor(image, results, Imgproc.COLOR_BGR2GRAY);

        // threshold
        Mat temp = new Mat();
        double otsu = Imgproc.threshold(results, temp, 0, 255, Imgproc.THRESH_OTSU | Imgproc.THRESH_BINARY_INV);

        Imgproc.threshold(results, results, otsu * 1.8, 255, Imgproc.THRESH_BINARY_INV);

        return results;
    }


    public Mat ThresholdImage(Mat image, double th) {
        Mat results = new Mat();
        // convert to grayscale
        Imgproc.cvtColor(image, results, Imgproc.COLOR_BGR2GRAY);
        // threshold
        Imgproc.threshold(results, results, th, 255, Imgproc.THRESH_BINARY_INV);
        return results;
    }


    // public  Mat infer(Mat image) {
    public MatOfKeyPoint detect(ref Mat image, bool showFiltered = false) { // pass by reference: allow to change the image within the function
        /* Comments:
         * After some experimenting, thresholding is probably enough for saliency detection and edge detect etc does not add much
         */

        // Preprocess: scale image
        //not needed? image = preprocess(image);

        //Mat img_temp = Dialate(EdgeDetect(image));
        //Mat img_temp = EdgeDetect2(image);
        // Mat img_temp = LineDet(image);
        //Mat img_temp = ThresholdImage(EnhanceContrast(EdgeDetect(image)), 150);



        Mat img_temp = ThresholdImageAdaptive(image);
        //Mat img_temp = Dialate(ThresholdImage(EnhanceContrast(image), 230));

        /* 
         * https://docs.opencv.org/3.4/d8/da7/structcv_1_1SimpleBlobDetector_1_1Params.html#a25b5d5542f6f92ff0779d14a273ba629
         * https://enoxsoftware.github.io/OpenCVForUnity/3.0.0/doc/html/class_open_c_v_for_unity_1_1_features2d_module_1_1_simple_blob_detector.html
        */


        //SimpleBlobDetector_Params params;
        SimpleBlobDetector_Params prms = new SimpleBlobDetector_Params();
        prms.set_filterByArea(true);
        prms.set_filterByInertia(false);
        prms.set_filterByCircularity(false);
        prms.set_filterByConvexity(false);
        //prms.set_collectContours(true);
        prms.set_maxThreshold(20);
        prms.set_minThreshold(0);
        prms.set_minArea(200f);


        // PARAMETERS: https://learnopencv.com/blob-detection-using-opencv-python-c/
        //Debug.Log("----------");
        //Debug.Log(prms.get_minThreshold() + " " + prms.get_maxThreshold());
        //Debug.Log(prms.get_minDistBetweenBlobs());
        //Debug.Log(prms.get_thresholdStep());

        SimpleBlobDetector detector = SimpleBlobDetector.create(prms);


        //img_temp = ~img_temp; // invert the image for the blob detection
        MatOfKeyPoint keypoints_saliency = new MatOfKeyPoint();
        detector.detect(img_temp, keypoints_saliency);
        //img_temp = ~img_temp; // invert the image after the blob detection
        //Debug.Log(keypoints_saliency);
        Features2d.drawKeypoints(img_temp, keypoints_saliency, img_temp, new Scalar(255.0, 0, 0), Features2d.DrawMatchesFlags_DRAW_RICH_KEYPOINTS); // this flag is needed to draw the correct size!

        /*
        Mat temp = new Mat();
        results.convertTo(temp, CvType.CV_8UC3);
        */

        if (showFiltered) {
            img_temp.copyTo(image);
        }

        //return results;
        return keypoints_saliency;
    }

    protected virtual Mat preprocess(Mat image) {
        int h = (int)input_size.height;
        int w = (int)input_size.width;

        // WHY?
        if (input_sizeMat == null) {
            input_sizeMat = new Mat(new Size(w, h), CvType.CV_8UC3);// [h, w]
        }
        Imgproc.resize(image, input_sizeMat, new Size(w, h));
        return input_sizeMat;// [h, w, 3]
    }


    public Mat EnhanceContrast(Mat image) {
        // following: https://stackoverflow.com/questions/39308030/how-do-i-increase-the-contrast-of-an-image-in-python-opencv
        Mat results = new Mat();
        List<Mat> mv = new List<Mat>();
        Mat lab = new Mat();
        Imgproc.cvtColor(image, lab, Imgproc.COLOR_RGB2Lab);
        Core.split(lab, mv);
        CLAHE clahe = Imgproc.createCLAHE(2.0, new Size(8, 8));
        Mat cl = new Mat();
        clahe.apply(mv[0], cl);
        Core.merge(new List<Mat>() { cl, mv[1], mv[2] }, results);
        Imgproc.cvtColor(results, results, Imgproc.COLOR_Lab2RGB);
        return results;
    }



}



