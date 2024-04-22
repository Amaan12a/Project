using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadMovementController : MonoBehaviour
{
    public Transform headBone; // Reference to the bone representing the head
    public Transform target; // Target position for the head to look at
    public float headTurnSpeed = 5.0f; // Adjust as needed for the desired head movement speed

    void Update()
    {
        if (headBone != null && target != null)
        {
            // Calculate the direction from the head bone to the target position
            Vector3 targetDirection = target.position - headBone.position;
            targetDirection.y = 0f; // Optional: Ensure the head only rotates horizontally

            // Calculate the rotation to look at the target position
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Smoothly rotate the head bone towards the target rotation
            headBone.rotation = Quaternion.Slerp(headBone.rotation, targetRotation, Time.deltaTime * headTurnSpeed);
        }
    }
}
