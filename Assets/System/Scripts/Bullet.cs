using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator anim;
    public Vector2 direction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
   
        Destroy(gameObject,40f);
    }

    public void Launch(Vector2 dir)
    {
        direction = dir;
        rb.linearVelocity = direction;
    }
    void OnTriggerEnter2D(Collider2D other){
        if (other.gameObject.CompareTag("Player")){
            rb.linearVelocity = new Vector2(0,0);
            anim.SetTrigger("Destroy");
            Destroy(gameObject,4);
        }
    }
}
