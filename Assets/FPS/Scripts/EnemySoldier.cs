using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;



public class EnemySoldier : MonoBehaviour
{
    [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
    public float pathReachingRadius = 2f;

    [Tooltip("The speed at which the enemy rotates")]
    public float orientationSpeed = 10f;

    [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
    public float deathDuration = 0f;

    [Header("Sounds")]
    [Tooltip("Sound played when recieving damages")]
    public AudioClip damageTick;
    
    [Header("VFX")]
    [Tooltip("The VFX prefab spawned when the enemy dies")]
    public GameObject deathVFX;
    [Tooltip("The point at which the death VFX is spawned")]
    public Transform deathVFXSpawnPoint;

    [Header("Debug Display")]
    [Tooltip("Color of the sphere gizmo representing the path reaching range")]
    public Color pathReachingRangeColor = Color.yellow;
    [Tooltip("Color of the sphere gizmo representing the attack range")]
    public Color attackRangeColor = Color.red;
    [Tooltip("Color of the sphere gizmo representing the detection range")]
    public Color detectionRangeColor = Color.blue;

    public Actions EnemyActions;
    Collider[] m_SelfColliders;
    Actor m_Actor;
    Health m_Health;
    Animator anim;

    EnemyManager m_EnemyManager;

    public RuntimeAnimatorController RAC;

    public PatrolPathSoldier patrolPath { get; set; }

    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;

    public GameObject knownDetectedTarget => m_DetectionModule.knownDetectedTarget;
    public bool isTargetInAttackRange => m_DetectionModule.isTargetInAttackRange;
    public bool isSeeingTarget => m_DetectionModule.isSeeingTarget;
    public bool hadKnownTarget => m_DetectionModule.hadKnownTarget;
    public NavMeshAgent m_NavMeshAgent { get; private set; }
    public DetectionModuleSoldier m_DetectionModule { get; private set; }
    public WeaponController m_CurrentWeapon;
    float m_LastTimeDamaged = float.NegativeInfinity;
    bool m_WasDamagedThisFrame;






    int m_PathDestinationNodeIndex;


     
    // Start is called before the first frame update
    void Start()
    {
        FindAndInitializeAllWeapons();

        m_EnemyManager = FindObjectOfType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemySoldier>(m_EnemyManager, this);

        m_EnemyManager.RegisterSoldier(this);

        EnemyActions = GetComponent<Actions>();
        anim =this.GetComponent<Animator>();
        anim.runtimeAnimatorController = RAC;

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemySoldier>(m_Actor, this, gameObject);

        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

        var detectionModules = GetComponentsInChildren<DetectionModuleSoldier>();
        DebugUtility.HandleErrorIfNoComponentFound<DetectionModuleSoldier, EnemySoldier>(detectionModules.Length, this, gameObject);
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModuleSoldier, EnemySoldier>(detectionModules.Length, this, gameObject);
        // Initialize detection module
        m_DetectionModule = detectionModules[0];
        m_DetectionModule.onDetectedTarget += OnDetectedTarget;
        m_DetectionModule.onLostTarget += OnLostTarget;
        onAttack += m_DetectionModule.OnAttack;


       // Subscribe to damage & death actions
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;

    }

    // Update is called once per frame
    void Update()
    {
        if(m_NavMeshAgent){
            m_DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);
        }   
        m_WasDamagedThisFrame = false;
    }

    
    void OnLostTarget()
    {
        onLostTarget.Invoke();
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();
    }

     void OnDamaged(float damage, GameObject damageSource)
    {
        // test if the damage source is the player
        if (damageSource && damageSource.GetComponent<PlayerCharacterController>())
        {
            // pursue the player
            m_DetectionModule.OnDamaged(damageSource);

            if (onDamaged != null)
            {
                onDamaged.Invoke();
            }
            m_LastTimeDamaged = Time.time;

            // play the damage tick sound
            if (damageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(damageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

            m_WasDamagedThisFrame = true;
        }
    }
    void OnDie()
    {
        // spawn a particle system when dying
        //var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
        //Destroy(vfx, 5f);

        // tells the game flow manager to handle the enemy destuction
        //m_EnemyManager.UnregisterEnemy(this);

        // loot an object
        /*
        if (TryDropItem())
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }*/

        // this will call the OnDestroy function
        m_EnemyManager.UnregisterSoldier(this);
        m_NavMeshAgent.isStopped = true;
        EnemyActions.Death();
        Destroy(gameObject, deathDuration);
    }

    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orientationSpeed);
        }
    }

    

    private bool IsPathValid()
    {
        return patrolPath && patrolPath.pathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < patrolPath.pathNodes.Count; i++)
            {
                float distanceToPathNode = patrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < patrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (m_NavMeshAgent)
        {
            m_NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= pathReachingRadius)
            {
                // increment path destination index
                m_PathDestinationNodeIndex = inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += patrolPath.pathNodes.Count;
                }
                if (m_PathDestinationNodeIndex >= patrolPath.pathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= patrolPath.pathNodes.Count;
                }
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        // Path reaching range
        Gizmos.color = pathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, pathReachingRadius);

        if (m_DetectionModule != null)
        {
            // Detection range
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.detectionRange);

            // Attack range
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.attackRange);
        }
    }
    bool FiringAnimatorIsPlaying(){
        if(anim.GetCurrentAnimatorStateInfo(0).IsName("Fire")){
            Debug.Log("Fire");
            return true;
        }
        else{
            return false;
        }
    }

     public bool TryAtack(Vector3 enemyPosition)
        {
            OrientWeaponsTowards(enemyPosition);
            // Shoot the weapon
            bool didFire = false;
            if(FiringAnimatorIsPlaying()){
                didFire = m_CurrentWeapon.HandleShootInputs(false, true, false);
            }
            if (onAttack != null)
            {   
                onAttack.Invoke();
            }
            return didFire;
        }


    public void OrientWeaponsTowards(Vector3 lookPosition)
    {

            // orient weapon towards player
            Vector3 weaponForward = (lookPosition - m_CurrentWeapon.weaponRoot.transform.position).normalized;
            m_CurrentWeapon.transform.forward = weaponForward;
        
    }

    void FindAndInitializeAllWeapons()
    {
        // Check if we already found and initialized the weapons
        if (m_CurrentWeapon == null)
        {
            m_CurrentWeapon = GetComponentInChildren<WeaponController>();
            m_CurrentWeapon.owner = gameObject;
        }
    }
}
