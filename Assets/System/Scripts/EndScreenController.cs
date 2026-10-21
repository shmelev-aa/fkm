using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System;
public class EndScreenController : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text scoreT,timeT,hpT;
    [SerializeField] Image faderImage;
    [SerializeField] CanvasGroup group;

    void Awake(){
        faderImage.fillAmount = 0;
        group.alpha = 0;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStats(int hp,int maxHp, int score, float time){
        scoreT.text = $"Очков набрано:          {score}";
        hpT.text = $"Здоровья осталось:   {hp} / {maxHp}";

        int minutes = (int)time / 60;
        TimeSpan t = TimeSpan.FromSeconds(time);
        string answer = string.Format("{0:D2}:{1:D2}:{2:D3}", 
                t.Minutes, 
                t.Seconds, 
                t.Milliseconds);
        time -= minutes * 60;
        timeT.text = $"Время прохождения: {answer}";
    }

    public void ShowScreen(){
        StartCoroutine("ShowCoroutine");
    }

    IEnumerator ShowCoroutine(){
        yield return new WaitForSeconds(.1f);

        while(faderImage.fillAmount < 1){
            faderImage.fillAmount+=0.05f;
            yield return new WaitForSeconds(.05f);
        }
        yield return new WaitForSeconds(0.5f);
        while(group.alpha < 1){
            group.alpha+=0.05f;
            yield return new WaitForSeconds(.05f);
        }
    }

    public void RestartLevel(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
