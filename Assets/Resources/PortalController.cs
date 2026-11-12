using UnityEngine;

public class PortalController : MonoBehaviour
{
    [SerializeField] PortalController otherPortal;
    public bool isOnCooldown;
    

    public void OffCooldown()
    {
        isOnCooldown = false;
    }
   
    void OnTriggerStay2D(Collider2D col)
    {
        if(col.gameObject.CompareTag("Player") && !isOnCooldown)
        {
            if (otherPortal == null)
                return;

            isOnCooldown = true;
            otherPortal.isOnCooldown = true;
            Invoke("OffCooldown", 3);
            otherPortal.Invoke("OffCooldown", 3);
            col.transform.position = otherPortal.transform.position;
        }
    }
}
