using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReachController : MonoBehaviour {

    public GameObject Target;

    private Animator animator;

    private float pressIKWeight; 

    void Start() {
        animator = GetComponent<Animator>();
        pressIKWeight = 1.0f; 
    }

    void OnAnimatorIK(int layerIndex) {
        animator.SetIKPosition(AvatarIKGoal.LeftHand, Target.transform.position);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, pressIKWeight);
        
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, pressIKWeight);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, Target.transform.rotation);

    }
}