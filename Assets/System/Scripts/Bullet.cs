using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb.linearVelocity = new Vector2(-1,0);
        Destroy(gameObject,40f);
    }

    void OnTriggerEnter2D(Collider2D other){
        if (other.gameObject.CompareTag("Player")){
            rb.linearVelocity = new Vector2(0,0);
            anim.SetTrigger("Destroy");
            Destroy(gameObject,4);
        }
    }
}
