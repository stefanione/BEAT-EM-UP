using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Scripting.APIUpdating;

public class DecisionTreeNode
{
    public string Attribute;
    public string Value;
    public List<DecisionTreeNode> Children;
    public bool IsLeaf;
    public string Action;

    public DecisionTreeNode()
    {
        Children = new List<DecisionTreeNode>();
    }

    public void PrintTree(string indent = "", bool last = true)
    {
        //Debug.Log(indent + "+- " + (IsLeaf ? $"[Leaf] Action: {Action}" : $"Attribute: {Attribute}, Value: {Value}"));
        indent += last ? "   " : "|  ";

        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].PrintTree(indent, i == Children.Count - 1);
        }
    }
}

public class DecisionTreeID3
{

    public DecisionTreeNode BuildTree(List<Dictionary<string, string>> data, List<string> attributes, string targetAttribute)
    {
        DecisionTreeNode node = new DecisionTreeNode();

        var uniqueValues = data.Select(d => d[targetAttribute]).Distinct().ToList(); // selectam atribute fara valoarea de target
        if (uniqueValues.Count == 1) 
        {
            node.IsLeaf = true;
            node.Action = uniqueValues[0];
            return node;
        }

        if (attributes.Count == 0)
        {
            node.IsLeaf = true;
            node.Action = data.GroupBy(d => d[targetAttribute]).OrderByDescending(g => g.Count()).First().Key;
            //Debug.Log("Creating leaf node with majority action: " + node.Action);
            return node;
        }
        // daca am unul atunci sigur este atributul cel mai bun

        string bestAttribute = GetBestAttribute(data, attributes, targetAttribute); // daca am mai multe selectam pe cel mai bun cu formulele de la curs si este si pus ca radacina
        node.Attribute = bestAttribute;
        //Debug.Log("Best attribute selected: " + bestAttribute);

        var attributeValues = data.Select(d => d[bestAttribute]).Distinct(); // selectam valorile celui mai bun atribut adica distincte
        foreach (var value in attributeValues)
        {
            var subset = data.Where(d => d[bestAttribute] == value).ToList(); // cream subseturi pentru fiecare valoare
            if (subset.Count == 0) 
            {
                var majorityAction = data.GroupBy(d => d[targetAttribute]).OrderByDescending(g => g.Count()).First().Key;
                node.Children.Add(new DecisionTreeNode
                {
                    IsLeaf = true,
                    Action = majorityAction,
                    Value = value
                });
                // daca nu avem selectam din datele atributului tinta sortate descrescator dupa numarul de elemente pe cel cu cele mai multe elemente
                //Debug.Log($"Creating leaf node for value {value} with majority action: {majorityAction}");
            }
            else // altfel cream subsetul cu cel mai bun atribut urmator
            {
                var newAttributes = new List<string>(attributes);
                newAttributes.Remove(bestAttribute);
                var childNode = BuildTree(subset, newAttributes, targetAttribute);
                childNode.Value = value;
                node.Children.Add(childNode);
                //Debug.Log($"Adding child node for value: {value}");
                // altfel creeaza nodul cu urmatoarele atribute. Apeleaza recursiv BuildTree
            }
        }

        return node;
    }


    private string GetBestAttribute(List<Dictionary<string, string>> data, List<string> attributes, string targetAttribute)
    {
        double baseEntropy = CalculateEntropy(data, targetAttribute);
        double bestInfoGain = double.MinValue;
        string bestAttribute = null;

        foreach (var attribute in attributes)
        {
            double newEntropy = 0.0;
            var attributeValues = data.Select(d => d[attribute]).Distinct();
            foreach (var value in attributeValues)
            {
                var subset = data.Where(d => d[attribute] == value).ToList();
                double subsetEntropy = CalculateEntropy(subset, targetAttribute);
                newEntropy += (double)subset.Count / data.Count * subsetEntropy;
            }

            double infoGain = baseEntropy - newEntropy;
            if (infoGain > bestInfoGain)
            {
                bestInfoGain = infoGain;
                bestAttribute = attribute;
            }
        }

        return bestAttribute;
    }

    private double CalculateEntropy(List<Dictionary<string, string>> data, string targetAttribute)
    {
        var valueCounts = data.GroupBy(d => d[targetAttribute]).Select(g => g.Count());
        double entropy = 0.0;
        foreach (var count in valueCounts)
        {
            double probability = (double)count / data.Count;
            entropy -= probability * Math.Log(probability, 2);
        }
        return entropy;
    }
}

public class Enemybehaviour : MonoBehaviour
{
    private DecisionTreeNode decisionTree;
    public float healthEnemyMax;
    public float healthEnemy;
    [SerializeField] private float distanceToPlayer;
    public PlayerMovement player;
    public int damage;
    private Rigidbody2D body;
    public float enemySpeed;
    public bool isFlipped;
    public Animator animator;
    public bool isAttacked;  // Define isAttacked
    public float distanceToBuddies;  // Define distanceToBuddies
    public float playerHealth;  // Define playerHealth
    [SerializeField] private float attackcooldown;
    private float cooldownTimer = Mathf.Infinity;
    [SerializeField] public BoxCollider2D Boxcollider; 
    [SerializeField] private LayerMask playerlayer;
    public float enemyrange;
    [SerializeField] private float AttackDistance;
    public UIManager uiManager;
    public int enemyId; // Identificator unic pentru fiecare inamic
    public static int nextId = 0; // Generator de identificatori unici
    public int sendToBoss = 0;

    public static List<Enemybehaviour> allEnemies = new List<Enemybehaviour>();
    public bool helpRequested;
    public bool helpMessageDisplayed;
    private BOSS boss;

    private void Awake() {
        enemyId = nextId++;
        allEnemies.Add(this);
    }

    void Start() {
        body = GetComponent<Rigidbody2D>();
        isFlipped = false;
        healthEnemy = healthEnemyMax;
        isAttacked = false;

        var data = ReadCSV("D:/college/files-summer-school/ID303.csv");
        var attributes = data.First().Keys.ToList();
        attributes.Remove("Action");

        DecisionTreeID3 id3 = new DecisionTreeID3();
        decisionTree = id3.BuildTree(data, attributes, "Action");

        //decisionTree.PrintTree();
        uiManager = FindObjectOfType<UIManager>();
        //Debug.Log("We have : " + allEnemies.Count() + " ENEMIES");
        boss = FindObjectOfType<BOSS>();
    }

    void Update()
    {
        string healthState = GetHealthState();
        string distanceState = GetDistanceState();
        string isAttackedState = GetIsAttackedState();  
        string distanceToBuddiesState = GetDistanceToBuddiesState();  
        string playerHealthState = GetPlayerHealthState();

        // if (enemyId == 2){
        //     sendToBoss = 30;
        // }


        Dictionary<string, string> currentState = new Dictionary<string, string>
        {
            { "Health", healthState },
            { "DistanceToPlayer", distanceState },
            { "isAttacked", isAttackedState },
            { "DistanceToBuddies", distanceToBuddiesState },
            { "PlayerHealth", playerHealthState }
        };

        string action = Classify(decisionTree, currentState);
        PerformAction(action);
    }

    string GetHealthState()
    {
        if (healthEnemy >= 70) return "High";
        if (healthEnemy >= 30) return "Medium";
        return "Low";
    }

    string GetDistanceState()
    {
        if (distanceToPlayer <= 10) return "Close";
        if (distanceToPlayer <= 30) return "Medium";
        return "Far";
    }

    string GetIsAttackedState()
    {
        return isAttacked ? "True" : "False";
    }

    string GetDistanceToBuddiesState()
    {
        if (distanceToBuddies <= 10) return "Close";
        if (distanceToBuddies <= 30) return "Medium";
        return "Far";
    }

    string GetPlayerHealthState()
    {
        if (player.health >= 70) return "High";
        if (player.health >= 30) return "Medium";
        return "Low";
    }

    string Classify(DecisionTreeNode tree, Dictionary<string, string> record)
    {
        //Debug.Log("|||||||| before");
        //tree.PrintTree();
        //Debug.Log("|||||||| before ISLEAF");

        if (tree.IsLeaf)
        {
            return tree.Action;
        }
        //Debug.Log("tree.ATTRIBUTE : " + tree.Attribute);
        //Debug.Log("Record[tree.Attribute]" + record[tree.Attribute]);
        //var childNode = tree.Children.FirstOrDefault(c => c.Value == record[tree.Attribute]);
        DecisionTreeNode childNode = null;
        foreach (var child in tree.Children)
        {
            //Debug.Log("Child Value : " + child.Value);
            if (child.Value == record[tree.Attribute])
            {
                childNode = child;
                //Debug.Log("Am selectat : un copil");
                break;
            }
        }

        
        //Debug.Log("||||||||");
        //childNode.PrintTree();
        //Debug.Log("||||||||");
        if (childNode == null)
        {
            //Debug.LogError($"No child with value {record[tree.Attribute]} for attribute {tree.Attribute}. Using default action.");
            return tree.Children.First().Action; // Return the action of the first child as a default action
        }

        return Classify(childNode, record);
    }

    void PerformAction(string action)
    {
        switch (action)
        {
            case "Attack":
                //Debug.Log("Inamicul atacă!");
                cooldownTimer += Time.deltaTime;
                if(PlayerInsight() && player != null){
                    //Debug.Log("VAD PLAYER");
                    if(cooldownTimer >= attackcooldown){
                        cooldownTimer = 0;
                        //Debug.Log("ATAC");
                        animator.SetBool("attack", true);
                        DealDamage();
                    }
                }
                else {
                    animator.SetBool("attack", false);
                }
                animator.SetBool("walk", false);
                break;
            case "RequestHelp":
                //Debug.Log("enemyID is : " + enemyId);
                // Boss.distanceToPlayer = 30;
                // Debug.Log("Am setat la 30 pt BOSS !!!!");
                if(enemyId == 1 && !helpMessageDisplayed){
                    //Debug.Log("vreau sa dau mesaj");
                    //sendToBoss = 30;
                    if (uiManager != null) {
                        uiManager.check = true;
                        uiManager.ShowHelpMessage(transform.position);
                        helpMessageDisplayed = true;
                    } else {
                        //Debug.Log("uimanager este null");
                    }
                    //Destroy(uiManager);
                }
                
                //animator.SetBool("attack", false);
                if(distanceToBuddies <= 10){
                    foreach(var enemy in allEnemies){
                        if (enemy != this && distanceToBuddies <= 10 && enemy.distanceToBuddies <= 10){//Vector2.Distance(transform.position, enemy.transform.position) > 3){ //&& Vector2.Distance(transform.position, enemy.transform.position) > 0){
                            ComeToHelp(this, enemy);
                            break;

                        }
                    }
                }
                
                break;
            case "Approach":
                animator.SetBool("walk", true);
                animator.SetBool("attack", false);
                if (Vector2.Distance(transform.position, player.transform.position) > 3)
                {
                    Vector2 direction = (player.transform.position - transform.position).normalized;
                    body.velocity = direction * enemySpeed;
                    //Debug.Log("Inamicul se apropie!");
                }
                else
                {
                    body.velocity = Vector2.zero;
                    //Debug.Log("Inamicul s-a oprit aproape de jucător!");
                    distanceToPlayer = 8;
                    animator.SetBool("walk", false);
                }
                
                break;
            case "default":
                animator.SetBool("walk", false);
                animator.SetBool("attack", true);
                break;
        }
    }


public void ComeToHelp(Enemybehaviour requester, Enemybehaviour requested)
    {  

        //Debug.Log("requester : " + requester + " requested : " + requested);
        if(requester.distanceToBuddies <= 10 && requested.distanceToBuddies <= 10){
            requested.animator.SetBool("walk", true);
            //Debug.Log("Un inamic răspunde la cererea de ajutor!");
            //StartCoroutine(HelpAllyRoutine(requester, requested));
            if(Vector2.Distance(requested.transform.position, requester.transform.position) > 4){
                Vector2 direction = (requester.transform.position - requested.transform.position).normalized;
                requested.body.velocity = direction * enemySpeed;
            } else {
                requested.body.velocity = Vector2.zero;
            // Debug.Log("Inamicul s-a oprit aproape de jucător!");
                requested.animator.SetBool("walk", false);
            }
        }
        
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            player.TakingDamage(damage);
        }
    }

    private void DealDamage(){
        if(PlayerInsight()){
            player.TakingDamage(damage);
        }
    }

    public void TakeEnemyDamage(float damage)
    {
        healthEnemy -= damage;
        isAttacked = true;  // Set isAttacked to true when taking damage
        //Debug.Log("health now is : " + healthEnemy);
        animator.SetTrigger("Hurt");
        if (healthEnemy <= 0)
        {
            Dead();
        }

        if (enemyId == 2 && boss != null)
        {
            // Set the BOSS's distanceToPlayer to 30
            boss.distanceToPlayer = 30;
            //Debug.Log("Enemy ID 2 attacked, setting Boss's distanceToPlayer to 30");
        }
    }

    private void Dead()
    {
        animator.SetBool("isdead", true);
        //animator.SetBool("walk", false);
        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }
        this.enabled = false;
        allEnemies.Remove(this);
        Destroy(gameObject, 3f);
    }


    private List<Dictionary<string, string>> ReadCSV(string filePath)
    {
        var data = new List<Dictionary<string, string>>();
        var lines = File.ReadAllLines(filePath);
        var headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            var record = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                record[headers[j]] = values[j];
            }
            data.Add(record);
        }

        return data;
    }

    private bool PlayerInsight(){
        // RaycastHit2D hit = Physics2D.BoxCast(boxcollider.bounds.center + transform.right * enemyrange * transform.localScale.x * AttackDistance,
        //                                     new Vector3(boxcollider.bounds.size.x * enemyrange, boxcollider.bounds.size.y, boxcollider.bounds.size.z), 
        //                                     0, 
        //                                     Vector2.right, 
        //                                     0, 
        //                                     playerlayer);

        
        Vector3 boxCastCenter = Boxcollider.bounds.center + transform.right * enemyrange * transform.localScale.x * AttackDistance;
        Vector3 boxCastSize = new Vector3(Boxcollider.bounds.size.x * enemyrange, Boxcollider.bounds.size.y, Boxcollider.bounds.size.z);
        
        // Perform the BoxCast
        RaycastHit2D hit = Physics2D.BoxCast(boxCastCenter, boxCastSize, 0, Vector2.left, 0, playerlayer);

        // Debug.Log("BoxCast Center: " + Boxcollider.bounds.center);
        // Debug.Log("BoxCast Size: " + Boxcollider.bounds.size);
        // Debug.Log("Transform Right: " + transform.right);

       if (hit.collider != null)
        {
            //Debug.Log("Player detectat în zona de atac");
            return true;
        }

        //Debug.Log("Player nu este în zona de atac");
        return false;
    }
    void OnDrawGizmosSelected(){

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Boxcollider.bounds.center + transform.right * enemyrange * transform.localScale.x * AttackDistance, 
                            new Vector3(Boxcollider.bounds.size.x * enemyrange, Boxcollider.bounds.size.y, Boxcollider.bounds.size.z));
    }
}




