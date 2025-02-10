using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update

    public float movespeed;

    public bool isMoving;
    public int MaxHealth;
    public int health;

    public Vector2 input;
    private Rigidbody2D body;
    private bool facingRight = true;
    private bool grounded;
    private Animator anim;

    public Transform attackPoint;
    public float attackRange = 0.5f;

    public LayerMask enemylayers;
    public LayerMask BOSSlayer;


    // void Awake(){
    //     body = GetComponent<Rigidbody2D>();
    // }
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        health = MaxHealth;
        anim = GetComponent<Animator>();
    }

    public void TakingDamage(int damage){
        health -= damage;

        if(health <= 0){
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {

        body.velocity = new Vector2(Input.GetAxisRaw("Horizontal") * movespeed, body.velocity.y);
        float horizontalinput = Input.GetAxisRaw("Horizontal");

        if(Input.GetKey(KeyCode.Space) && grounded){
            Jump();
        }

        if(Input.GetKeyDown(KeyCode.F) && grounded){
            Attack();
        }

        if(Input.GetKeyDown(KeyCode.H) && grounded){
            Heal();
        }

        if(horizontalinput > 0 && !facingRight){
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            facingRight = !facingRight;
        }

        if(horizontalinput < 0 && facingRight){
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            facingRight = !facingRight;
        }
    
        anim.SetBool("run", horizontalinput != 0);
        anim.SetBool("grounded", grounded);
    }

    private void Jump(){
        body.velocity = new Vector2(body.velocity.x, movespeed);
        anim.SetTrigger("jump");
        grounded = false;
    }

    private void Attack(){
        anim.SetTrigger("attack");
        Collider2D[] enemy_hitpoints = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemylayers);

        foreach(Collider2D enemy_hitpoint in enemy_hitpoints){
            Debug.Log("We got'em !!!");
            enemy_hitpoint.GetComponent<Enemybehaviour>().TakeEnemyDamage(20);
        }

        Collider2D[] BOSS_hitpoints = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, BOSSlayer);

        foreach(Collider2D BOSS_hitpoint in BOSS_hitpoints){
            Debug.Log("We got'em !!!");
            BOSS_hitpoint.GetComponent<BOSS>().TakeEnemyDamage(10);
        }


    }

    private void Heal(){
        health += 10;
    }

    void OnDrawGizmosSelected(){

        if(attackPoint == null){
            return;
        }

        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private void OnCollisionEnter2D(Collision2D collision){
        if(collision.gameObject.tag == "Ground"){
            grounded = true;
        }
    }

}
