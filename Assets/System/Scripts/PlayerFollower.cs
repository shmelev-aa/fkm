using UnityEngine;

public class PlayerFollower : MonoBehaviour
{

    Transform player;
    float damping = 0.25f;
    Vector3 offset;
    Vector3 vel;
    void Awake(){
        player = GameObject.Find("Игрок").transform;
        offset = new Vector3(0,1,0);
    }

    void FixedUpdate(){
        Vector3 targetPosition = player.position + offset;
        targetPosition.z = -10;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref vel,damping);
    }

}
