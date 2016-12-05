using UnityEngine;
using System.Collections;

public class PlayerLocomotionDebug : MonoBehaviour
{
    [SerializeField]
    private PlayerLocomotion locomotion;


    void Reset()
    {
        locomotion = GetComponent<PlayerLocomotion>();
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(locomotion.serverState.position, 1.0f);
    }
}