using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMoveHelper : MonoBehaviour {

    public float movementForce;
    public List<Transform> waypoints;

    private Transform target;

    private Rigidbody m_Rigidbody;
    private bool bMoveHand;
    private bool bResetHand;
    private Vector3 vPositionInitial;
    private Quaternion qRotationInitial;

    private int iX;
    void Start() {
        //Fetch the Rigidbody from the GameObject with this script attached
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.freezeRotation = true;
        bMoveHand = false;
        bResetHand = false;
        iX = 0;
    }



    void FixedUpdate() { // physics-based stuff has to be here
        if (Input.GetKey("m")) {
            iX = 0;
            // store the initial rotation and position; needed for the hand to return to start
            qRotationInitial = transform.rotation;
            vPositionInitial = transform.position;
            bMoveHand = true;
            bResetHand = false;
        }


        if (Input.GetKey("r")) {
            iX = waypoints.Count - 2;
            bResetHand = true;
            bMoveHand = false;
        }


        if (bMoveHand) {
            target = waypoints[iX];
            Vector3 vv = (target.position - transform.position).normalized;
            m_Rigidbody.AddForce(vv * movementForce);
            float fDist = Vector3.Distance(transform.position, target.transform.position);
            float fWeight = Mathf.Pow(0.001f, fDist);
            Quaternion deltaRotation = Quaternion.Euler(Vector3.Lerp(qRotationInitial.eulerAngles, target.eulerAngles, fWeight));
            m_Rigidbody.rotation = deltaRotation;
            if (fDist < 0.07) {
                iX++;
                if (iX >= (waypoints.Count)) {
                    iX = 0;
                    m_Rigidbody.AddForce(new Vector3(0.0f, 0.0f, 0.0f));
                    bMoveHand = false;
                }
            }
        }


        if (bResetHand) {
            Quaternion qAngleTarget;
            Vector3 vPositionTarget;

            if (iX >= 0){
                qAngleTarget = waypoints[iX].rotation; 
                vPositionTarget = waypoints[iX].position;

            } else {
                qAngleTarget = qRotationInitial; 
                vPositionTarget = vPositionInitial;
            }

            Vector3 vv = (vPositionTarget - transform.position).normalized;
            m_Rigidbody.AddForce(vv * movementForce);

            float fDist = Vector3.Distance(transform.position, vPositionTarget);
            float fWeight = Mathf.Pow(0.001f, fDist);
//            Debug.Log("distance: " + fDist.ToString() + " weight: " + fWeight.ToString());

            Quaternion deltaRotation = Quaternion.Euler(Vector3.Lerp(qRotationInitial.eulerAngles, qAngleTarget.eulerAngles, fWeight));

            m_Rigidbody.rotation = deltaRotation;

            if (fDist < 0.07) {
                Debug.Log("reset NEXT");
                if (iX<0) {
                    m_Rigidbody.AddForce(new Vector3(0.0f, 0.0f, 0.0f));
                    bResetHand = false;
                    Debug.Log("reset END");
                }
                iX--;
            }
        }
    }

}

