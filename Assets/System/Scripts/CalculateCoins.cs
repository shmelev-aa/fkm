using UnityEngine;

public class CalculateCoins : MonoBehaviour
{

    [SerializeField] TMPro.TMP_Text coinCounter;
    [SerializeField] TMPro.TMP_Text gemCounter;
    public int coinsCount = 0;
    public  int gemCount = 0;

    void Awake(){
        var objs = GameObject.FindGameObjectsWithTag("Coin");

        coinsCount = objs.Length;
        objs = GameObject.FindGameObjectsWithTag("Gem");
        gemCount = objs.Length;

        UpdateCoins(0);
        UpdateGems(0);
    }
    
    public void UpdateCoins(int amount){
        coinCounter.text = $"Собрано: {amount}/{coinsCount}";
    }

    public void UpdateGems(int amount){
        gemCounter.text = $"Собрано: {amount}/{gemCount}";
    }
}
