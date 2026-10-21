using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public class PlayerController : MonoBehaviour
{
    [SerializeField] int maxHp = 3;
    int currentHp;
    [SerializeField] float speed;
    [SerializeField] float jumpForce = 5;
    InputAction moveAction,jumpAction;
    Rigidbody2D rb;
    
    SpriteRenderer sr;

    float currentSpeed;
    Vector2 moveInput;
    bool isGrounded;
    Animator anim;


    int coins = 0;
    int gems = 0;
    List<GameObject> togglableObjects = new List<GameObject>();

    Vector3 startPosition;


    CalculateCoins calculateCoins;
    HpController hpController;
    bool onFan;
    bool isDead;
    float playTime;
    EndScreenController esc;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); 
        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        calculateCoins = GameObject.Find("Hud").GetComponent<CalculateCoins>();
        hpController   = GameObject.Find("Hud").GetComponent<HpController>();
        esc  = GameObject.Find("Fader").GetComponent<EndScreenController>();
        startPosition = transform.position;
        currentHp = maxHp;
    }

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        hpController.SetTargetHp(currentHp/maxHp);
    }

    // Update is called once per frame
    void Update()
    {
        playTime += Time.deltaTime;

        if(isDead) return;
        
        if (jumpAction.WasPressedThisFrame())
        {
            Jump();
        }

        moveInput = moveAction.ReadValue<Vector2>();
        if (moveInput.x > 0)
        {
            sr.flipX = false;
        }
        else if (moveInput.x < 0) {
            sr.flipX = true;
        }

    }

    void Jump(){
        RaycastHit2D hit = Physics2D.CircleCast(transform.position - new Vector3(0,0.5f,0), 0.1f,-Vector2.up, 0.05f);
        if(hit.collider != null && hit.transform.CompareTag("Ground")){

            isGrounded = true;
        }
        if (isGrounded){
            anim.SetTrigger("Jumped");
            rb.linearVelocityY = 0;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
        }
    }

    void MoveHorizontal()
    {
        rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
    }
    void FixedUpdate()
    {
        if(isDead) return;
        MoveHorizontal();
        ApplyFan();
    }

    void ApplyFan(){
        if(onFan){
            rb.AddForce(Vector2.up * 12);
            onFan = false;
        }
    }
    void LateUpdate(){
        if(isDead) return;
        if(rb.linearVelocityX > speed){
            rb.linearVelocityX = speed;
        }

        anim.SetBool("Running",Mathf.Abs(rb.linearVelocityX ) > 0.1f);
    }

    void ReceiveDamage(int amount, Vector3 sourcePos){
        currentHp -= amount;
        hpController.SetTargetHp((float)currentHp/(float)maxHp);
        rb.AddForce((transform.position - sourcePos).normalized * 3, ForceMode2D.Impulse);
        if(currentHp <= 0){
            anim.SetTrigger("Death");
            rb.bodyType = RigidbodyType2D.Static;
            rb.linearVelocity = Vector2.zero;
            Invoke("ResetLevel",2);
            isDead = true;
        }
        else{
            anim.SetTrigger("Hit");
        }
    }

    void ResetLevel(){
        currentHp = maxHp;
        coins = 0;
        gems = 0;
        playTime = 0;
        calculateCoins.UpdateCoins(0);
        calculateCoins.UpdateGems(0);
        transform.position = startPosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        anim.SetTrigger("Ressurected");
        isDead = false;
        foreach (var t in togglableObjects)
        {
            t.SetActive(true);
        }
        hpController.SetTargetHp(currentHp/maxHp);
        togglableObjects.Clear();
    }
    void OnTriggerEnter2D(Collider2D other){
                if(isDead) return;
        if(other.CompareTag("Coin")){
                coins++;
                togglableObjects.Add(other.gameObject);
                calculateCoins.UpdateCoins(coins);
                other.gameObject.SetActive(false);
        }
        if(other.CompareTag("Gem")){
                gems++;
                togglableObjects.Add(other.gameObject);
                other.gameObject.SetActive(false);
                calculateCoins.UpdateGems(gems);
        }

        if(other.CompareTag("Spikes") || other.CompareTag("Bullet")){    
            ReceiveDamage(1, other.transform.position);
        }

        if(other.CompareTag("Saw")){
            ReceiveDamage(currentHp,other.transform.position);
        }

        
        if(other.CompareTag("Spring")){
            other.GetComponent<Animator>().SetTrigger("Activated");
            anim.SetTrigger("Jumped");
            rb.linearVelocityY = 0;
            rb.AddForce(Vector2.up * 8, ForceMode2D.Impulse);
        }
    if(other.CompareTag("Finish")){
            FinishGame();
        }
    }
    void FinishGame(){

        //ReceiveDamage(currentHp,transform.position);
        rb.bodyType = RigidbodyType2D.Static;
        isDead = true;
        esc.SetStats(currentHp,maxHp,gems*3+coins,playTime);
        esc.ShowScreen();
    }
    void OnCollisionEnter2D(Collision2D col){
        if(isDead) return;
        anim.SetTrigger("Landed");
    }
    void OnTriggerStay2D(Collider2D other){
        if(isDead) return;
        if(other.CompareTag("FanZone")){
            onFan = true;
        }
    }


}
