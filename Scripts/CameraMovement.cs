using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{    
    [SerializeField] private float CameraSpeed;
    private float CurrenPosX;
    private Vector3 velocity = Vector3.zero;
    [SerializeField] private Transform player;

    private void Update(){
        transform.position = new Vector3(player.position.x, transform.position.y, transform.position.z);
    }
}
