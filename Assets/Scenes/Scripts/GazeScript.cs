using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeController : MonoBehaviour
{
    public GameObject redBall; // Reference to the red ball GameObject

    private Vector3 v3GazePosition;
    private Animator animator;
    private float lookIKWeight;

    void Start()
    {
        animator = GetComponent<Animator>();
        lookIKWeight = 1.0f;
    }

    void OnAnimatorIK(int layerIndex)
    {
        animator.SetLookAtWeight(lookIKWeight, 0.3f, 0.5f, 1.0f, 0f);
        animator.SetLookAtPosition(v3GazePosition);
    }

    // Update is called once per frame
    void Update()
    {
        // Make the gaze position follow the red ball
        if (redBall != null)
        {
            v3GazePosition = redBall.transform.position;
        }
    }
}
