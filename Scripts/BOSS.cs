using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Scripting.APIUpdating;


public class DecisionTreeNodeBoss
{
    public string Attribute;
    public string Value;
    public List<DecisionTreeNodeBoss> Children;
    public bool IsLeaf;
    public string Action;

    public DecisionTreeNodeBoss()
    {
        Children = new List<DecisionTreeNodeBoss>();
    }

    public void PrintTree(string indent = "", bool last = true)
    {
        Debug.Log(indent + "+- " + (IsLeaf ? $"[Leaf] Action: {Action}" : $"Attribute: {Attribute}, Value: {Value}"));
        indent += last ? "   " : "|  ";

        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].PrintTree(indent, i == Children.Count - 1);
        }
    }
}

public class DecisionTreeID3Boss
{

    public DecisionTreeNodeBoss BuildTree(List<Dictionary<string, string>> data, List<string> attributes, string targetAttribute)
    {
        DecisionTreeNodeBoss node = new DecisionTreeNodeBoss();

        var uniqueValues = data.Select(d => d[targetAttribute]).Distinct().ToList(); // selectam atribute fara valoarea de target
        if (uniqueValues.Count == 1) 
        {
            node.IsLeaf = true;
            node.Action = uniqueValues[0];
            return node;
        }

        if (attributes.Count == 0) // daca nu am atribute se va crea un nod frunze a carui actiune este valoarea majoritara din atributul 
                                    // tinta  din datele curente data
        {
            node.IsLeaf = true;
            node.Action = data.GroupBy(d => d[targetAttribute]).OrderByDescending(g => g.Count()).First().Key;
            return node;
        }
        // daca am unul atunci sigur este atributul cel mai bun

        string bestAttribute = GetBestAttribute(data, attributes, targetAttribute); // daca am mai multe selectam pe cel mai bun cu formulele de la curs si este si pus ca radacina
        node.Attribute = bestAttribute;

        var attributeValues = data.Select(d => d[bestAttribute]).Distinct(); // selectam valorile celui mai bun atribut adica distincte
        foreach (var value in attributeValues)
        {
            var subset = data.Where(d => d[bestAttribute] == value).ToList(); // cream subseturi pentru fiecare valoare
            if (subset.Count == 0) 
            {
                var majorityAction = data.GroupBy(d => d[targetAttribute]).OrderByDescending(g => g.Count()).First().Key;
                node.Children.Add(new DecisionTreeNodeBoss
                {
                    IsLeaf = true,
                    Action = majorityAction,
                    Value = value
                });
                // daca nu avem selectam din datele atributului tinta sortate descrescator dupa numarul de elemente pe cel cu cele mai multe elemente
            }
            else // altfel cream subsetul cu cel mai bun atribut urmator
            {
                var newAttributes = new List<string>(attributes);
                newAttributes.Remove(bestAttribute);
                var childNode = BuildTree(subset, newAttributes, targetAttribute);
                childNode.Value = value;
                node.Children.Add(childNode);
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





public class BOSS : MonoBehaviour
{
    // Start is called before the first frame update

    private DecisionTreeNodeBoss decisionTree;
    public float healthEnemyMax;
    public float healthEnemy;
    [SerializeField] public float distanceToPlayer;
    public PlayerMovement player;
    public int damage;
    private Rigidbody2D body;
    public float enemySpeed;
    public bool isFlipped;
    public Animator animator;
    public bool isAttacked; 
    public float distanceToBuddies; 
    public float playerHealth; 
    [SerializeField] private float attackcooldown;
    private float cooldownTimer = Mathf.Infinity;
    [SerializeField] public BoxCollider2D Boxcollider; 
    [SerializeField] private LayerMask playerlayer;
    public float bossrange;
    [SerializeField] private float AttackDistance;
    public UIManagerBOSS uiManagerboss;
    public static int nextId = 0;
    public bool helpRequested;
    public bool helpMessageDisplayed;
    private bool isFinalBoss;
    private bool healing;

    
    private void Awake() {
        healing = false;
    }

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        isFlipped = false;
        healthEnemy = healthEnemyMax;
        isAttacked = false;
        isFinalBoss = true;
        //healing = false;

        var data = ReadCSV("D:/college/files-summer-school/ID304.csv");
        var attributes = data.First().Keys.ToList();
        attributes.Remove("Action");

        DecisionTreeID3Boss id3 = new DecisionTreeID3Boss();
        decisionTree = id3.BuildTree(data, attributes, "Action");

        decisionTree.PrintTree();
        uiManagerboss = FindObjectOfType<UIManagerBOSS>();
       


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

    // Update is called once per frame
    void Update()
    {
        isFinalBoss = true;
        string healthState = GetHealthState();
        string distanceState = GetDistanceState();
        string isAttackedState = GetIsAttackedState();  
        string distanceToBuddiesState = GetDistanceToBuddiesState();  
        string playerHealthState = GetPlayerHealthState();
        string isFinalBossState = GetisFinalBossState();


        Dictionary<string, string> currentState = new Dictionary<string, string>
        {
            { "Health", healthState },
            { "DistanceToPlayer", distanceState },
            { "isAttacked", isAttackedState },
            { "DistanceToBuddies", distanceToBuddiesState },
            { "PlayerHealth", playerHealthState },
            { "isFinalBoss", isFinalBossState }
        };     

        string action = Classify(decisionTree, currentState);
        PerformAction(action);
    }

    void PerformAction(string action)
    {
        switch (action)
        {
            case "Attack":
                cooldownTimer += Time.deltaTime;
                if(PlayerInsight() && player != null){
                    if(cooldownTimer >= attackcooldown){
                        cooldownTimer = 0;
                        animator.SetBool("attack1", true);
                        DealDamage();
                    }
                }
                else {
                    animator.SetBool("attack1", false);
                }
                animator.SetBool("walk", false);
                break;
            case "Attack2":
                cooldownTimer += Time.deltaTime;
                if(PlayerInsight() && player != null){
                    if(cooldownTimer >= attackcooldown){
                        cooldownTimer = 0;
                        animator.SetBool("attack2", true);
                        DealDamage();
                    }
                }
                else {
                    animator.SetBool("attack2", false);
                }
                animator.SetBool("walk", false);
                break;    
            case "Approach":
                animator.SetBool("walk", true);
                animator.SetBool("attack1", false);
                animator.SetBool("attack2", false);
                if (Vector2.Distance(transform.position, player.transform.position) > 3)
                {
                    Vector2 direction = (player.transform.position - transform.position).normalized;
                    body.velocity = direction * enemySpeed;
                }
                else
                {
                    body.velocity = Vector2.zero;
                    distanceToPlayer = 8;
                    animator.SetBool("walk", false);
                }
                
                break;
            case "Heal":
                Debug.Log("Imi dau heal !!!");
                animator.SetBool("attack1", false);
                animator.SetBool("attack2", false);
                animator.SetBool("walk", false);
                
                if (healing == false){
                    healthEnemy += 10;
                    if (uiManagerboss != null){
                        uiManagerboss.check = true;
                        uiManagerboss.ShowHelpMessageBOSS(transform.position);
                    }
                    healing = true;
                }
                
                break;
            case "default":
                animator.SetBool("walk", false);
                animator.SetBool("attack1", true);
                animator.SetBool("attack2", false);
                break;
        }
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

    string GetisFinalBossState(){
        return "True";
    }

    string Classify(DecisionTreeNodeBoss tree, Dictionary<string, string> record)
    {

        if (tree.IsLeaf)
        {
            return tree.Action;
        }
        Debug.Log("tree.ATTRIBUTE : " + tree.Attribute);
        Debug.Log("Record[tree.Attribute]" + record[tree.Attribute]);
        DecisionTreeNodeBoss childNode = null;
        foreach (var child in tree.Children)
        {
            if (child.Value == record[tree.Attribute])
            {
                childNode = child;
                break;
            }
        }

        
        Debug.Log("||||||||");
        childNode.PrintTree();
        Debug.Log("||||||||");
        if (childNode == null)
        {
            Debug.LogError($"No child with value {record[tree.Attribute]} for attribute {tree.Attribute}. Using default action.");
            return tree.Children.First().Action; // Return the action of the first child as a default action
        }

        return Classify(childNode, record);
    }

    private void DealDamage(){
        if(PlayerInsight()){
            player.TakingDamage(damage);
        }
    }

    public void TakeEnemyDamage(float damage)
    {
        healthEnemy -= damage;
        isAttacked = true;  
        animator.SetTrigger("Hit");
        if (healthEnemy <= 0)
        {
            Dead();
        }
    }

    private void Dead()
    {
        animator.SetBool("isdead", true);
        GetComponent<Collider2D>().enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }


        this.enabled = false;
        Destroy(gameObject, 3f);
    }

    private bool PlayerInsight(){

        
        Vector3 boxCastCenter = Boxcollider.bounds.center + transform.right * bossrange * transform.localScale.x * AttackDistance;
        Vector3 boxCastSize = new Vector3(Boxcollider.bounds.size.x * bossrange, Boxcollider.bounds.size.y, Boxcollider.bounds.size.z);
        
        RaycastHit2D hit = Physics2D.BoxCast(boxCastCenter, boxCastSize, 0, Vector2.left, 0, playerlayer);

       if (hit.collider != null)
        {
            return true;
        }

        return false;
    }
    void OnDrawGizmosSelected(){

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Boxcollider.bounds.center + transform.right * bossrange * transform.localScale.x * AttackDistance, 
                            new Vector3(Boxcollider.bounds.size.x * bossrange, Boxcollider.bounds.size.y, Boxcollider.bounds.size.z));
    }


}
