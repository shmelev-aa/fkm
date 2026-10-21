using UnityEngine;

public class Canon : MonoBehaviour
{
    [Header("Настройки пушки")]
    [SerializeField] float delay = 3f;

    float cooldown = 0;
    bool onCooldown = false;

    [Header("А вот здесь не трогай, иначе что-то сломается ;)")]
    [SerializeField]GameObject bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] Animator anim;

    void Awake(){
        cooldown = delay;
    }

    // Update is called once per frame
    void Update()
    {
        cooldown-=Time.deltaTime;
        if(cooldown <=0){
            anim.SetTrigger("Shoot");
            cooldown = delay;
            Invoke("Shoot",0.1f);
            
        }
    }

    void Shoot(){
        GameObject bullet = Instantiate(bulletPrefab,shootPoint.position, Quaternion.identity);
        
        
    }


}
