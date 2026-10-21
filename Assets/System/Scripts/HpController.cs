using UnityEngine;
using UnityEngine.UI;
public class HpController : MonoBehaviour
{
    public Image hp;
    private float targetHp;
    private float rate = 0.01f;

    public void SetTargetHp(float targetPercentage){
        targetHp = targetPercentage;
    }
    
    void FixedUpdate(){
        if (hp.fillAmount > targetHp){
            hp.fillAmount-=rate;
        }
        if (hp.fillAmount < targetHp){
            hp.fillAmount+=rate ;

        }
        
    }
}
