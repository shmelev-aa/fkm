using UnityEngine;

public class Enabler : MonoBehaviour
{
    void Awake(){
        transform.GetChild(0).gameObject.SetActive(true);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
