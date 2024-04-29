using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD, PATROL, CHASE, DEAD }  //枚举怪物状态
[RequireComponent(typeof(NavMeshAgent))]        //为挂载目标自动添加NavMeshAgent组件
[RequireComponent(typeof(CharacterStats))]
public class EnemyController : MonoBehaviour,IEndGameObserver
{
    private EnemyStates enemyStates;    //怪物状态
    private NavMeshAgent agent;     //NavMeshAgent组件
    private Animator anim;      //动画
    private Collider coll;
    [Header("Patrol State")]
    public float patrolRange;   //巡逻范围
    [Header("Basic Settings")]  
    public float sightRadius;   //仇恨半径
    public bool isGuard;    //是否为站桩敌人
    public float lookAtTime;    //观察时间
    private float remainLookAtTime;     //剩余观察时间
    private float lastAttackTime;       //攻击后摇
    private float speed;     //速度
    private Quaternion guardRotation;   //站桩朝向
    protected GameObject attackTarget;    //攻击目标
    private Vector3 wayPoint;       //巡逻点
    private Vector3 guardPos;       //初始点
    protected CharacterStats characterStats;
    //用来转换动画
    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDead;
    bool playerDead;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();   //获取组件
        anim = GetComponent<Animator>();    //获取组件
        characterStats = GetComponent<CharacterStats>();
        coll = GetComponent<Collider>();

        speed = agent.speed;    //获取初始速度
        guardPos= transform.position;   //获取站桩坐标
        guardRotation = transform.rotation;
        remainLookAtTime = lookAtTime;
    }
    void Start()
    {
        //characterStats.MaxHealth = 2;
        if (isGuard)
        {
            enemyStates= EnemyStates.GUARD;
        }
        else
        {
            enemyStates= EnemyStates.PATROL;
            GetNewWayPoint();
        }
        GameManager.Instance.AddObserver(this);

    }
    void OnEnable()
    {
        //GameManager.Instance.AddObserver(this);
    }
    void OnDisable()
    {
        if (!GameManager.IsInitialized) return;
        GameManager.Instance.RemoveObserver(this);
    }

    void Update()
    {
        if (characterStats.CurrentHealth == 0)
            isDead = true;
        if (!playerDead)
        {
            SwitchStates();
            SwitchAnimation();
            lastAttackTime -= Time.deltaTime;
        }
    }
    void SwitchAnimation() 
    {
        /*动画控制
         * 关联bool值，通过传递bool到动画控制器来改变动画
         */
        anim.SetBool("Walk", isWalk);
        anim.SetBool("Chase", isChase);
        anim.SetBool("Follow", isFollow);
        anim.SetBool("Critical",characterStats.isCritical);     //暴击
        anim.SetBool("Death", isDead);
    }
    void SwitchStates()
    {
        if (isDead)
            enemyStates = EnemyStates.DEAD;
        else if (FoundPlayer())
        {
            enemyStates = EnemyStates.CHASE;    //怪物状态改为追击
            //Debug.Log("发现目标");        
        }


        switch (enemyStates)    //怪物状态控制
        {
            case EnemyStates.GUARD:
                /*站桩模式
                 */
                isChase = false;
                if (transform.position != guardPos)
                {
                    isWalk = true;
                    agent.isStopped = false;
                    agent.destination = guardPos;
                    if (Vector3.SqrMagnitude(guardPos - transform.position) <= agent.stoppingDistance)
                    {
                        isWalk = false;
                        transform.rotation = Quaternion.Lerp(transform.rotation, guardRotation, 0.01f);
                    }
                }
                break;
            case EnemyStates.PATROL:
                /*巡逻模式
                 */
                isChase = false;
                agent.speed= speed*0.5f;
                if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk= false;
                    if (remainLookAtTime > 0)
                        remainLookAtTime -= Time.deltaTime;
                    else
                        GetNewWayPoint();
                }
                else
                {
                    isWalk= true;
                    agent.destination= wayPoint;
                }
                break; 
            case EnemyStates.CHASE:
                /*追击模式
                 * 追击目标
                 * 当目标处于攻击范围内则进行攻击
                 * 怪物的动画切换
                 * 脱离仇恨后返回上一个状态
                 */
                isWalk = false;
                isChase = true;
                agent.speed = speed;
                if (!FoundPlayer())
                {
                    //脱离仇恨
                    isFollow= false;
                    if (remainLookAtTime > 0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if (isGuard)
                        enemyStates = EnemyStates.GUARD;
                    else
                        enemyStates = EnemyStates.PATROL;
                }
                else
                {
                    isFollow= true;
                    agent.isStopped = false;
                    agent.destination = attackTarget.transform.position;    //追击
                }
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if (lastAttackTime < 0)
                    {
                        lastAttackTime = characterStats.attackData.coolDown;
                        characterStats.isCritical = Random.value < characterStats.attackData.criticalChance;
                        Attack();
                    }
                }
                break;
            case EnemyStates.DEAD:
                coll.enabled = false;
                //agent.enabled = false;
                agent.radius= 0;
                Destroy(gameObject, 2f);
                break;
        }
    }

    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            anim.SetTrigger("Attack");
        }
        if (TargetInSkillRange())
        {
            anim.SetTrigger("Skill");
        }
    }
    bool FoundPlayer()  //判断玩家是否在仇恨范围内
    {
        var colliders=Physics.OverlapSphere(transform.position, sightRadius);   //Physics.OverlapSphere Unity自带的一种雷达功能（计算并存储接触球体或位于球体内部的碰撞体）返回值为Collider[] （一个数组，其中包含与球体接触或位于球体内部的所有碰撞体）
        foreach (var target in colliders)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;   //进入仇恨范围
                return true;
            }
        }
        attackTarget = null;    //脱离仇恨
        return false;
    }

    bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.attackRange;
        else
            return false;
    }

    bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.skillRange;
        else
            return false;
    }

    void GetNewWayPoint()   //获取随机巡逻点
    {
        remainLookAtTime = lookAtTime;
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);
        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);
        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }

    void OnDrawGizmosSelected()     //被选中时显示范围
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);     //监控范围
        
    }

    void Hit()
    {
        if (attackTarget != null && transform.IsFacingTarget(attackTarget.transform))
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();
            characterStats.TakeDamage(characterStats, targetStats);
        }
    }

    public void EndNotify()
    {
        anim.SetBool("Win", true);
        playerDead = true;
        isChase= false;
        isWalk= false;
        attackTarget= null;
    }
}
