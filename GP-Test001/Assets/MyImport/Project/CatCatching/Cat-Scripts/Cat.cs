using UnityEngine;

public class Cat : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2f;        // 移动速度
    public float minChangeTargetTime = 2f; // 最小改变目标时间
    public float maxChangeTargetTime = 5f; // 最大改变目标时间
    
    [Header("停留设置")]
    public float minRestTime = 1f;      // 最小停留时间
    public float maxRestTime = 3f;      // 最大停留时间
    public float minRestChance = 0.2f;  // 最小停留概率
    public float maxRestChance = 0.5f;  // 最大停留概率
    public float minMoveTime = 5f;      // 最小连续移动时间
    public float maxMoveTime = 10f;     // 最大连续移动时间
    
    [Header("防重合设置")]
    public float avoidanceRadius = 1f;  // 检测其他小猫的半径
    public float separationForce = 2f;  // 分离力强度
    public LayerMask catLayerMask;      // 小猫所在的层级
    
    private Vector2 currentTarget;      // 当前移动目标
    private float timer;                // 目标改变计时器
    private SpriteRenderer spriteRenderer; // 用于控制朝向
    private bool isResting = false;     // 是否正在停留
    private float restTimer;            // 停留计时器
    private float continuousMoveTimer;  // 连续移动时间计时器
    private float currentChangeTargetTime; // 当前改变目标时间
    private float currentRestChance;    // 当前停留概率
    private float currentMaxMoveTime;   // 当前最大移动时间

    void Start()
    {
        // 获取组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 为每只小猫设置不同的随机参数
        InitializeRandomParameters();
        
        // 设置初始随机目标
        GetNewRandomTarget();
        
        // 初始化计时器
        timer = currentChangeTargetTime;
        continuousMoveTimer = 0f;
    }
    
    void InitializeRandomParameters()
    {
        // 为每只小猫设置不同的行为参数
        currentChangeTargetTime = Random.Range(minChangeTargetTime, maxChangeTargetTime);
        currentRestChance = Random.Range(minRestChance, maxRestChance);
        currentMaxMoveTime = Random.Range(minMoveTime, maxMoveTime);
        
        // 初始停留时间也随机化
        restTimer = Random.Range(minRestTime, maxRestTime);
    }
    
    void Update()
    {
        // 如果正在停留，处理停留逻辑
        if (isResting)
        {
            HandleResting();
            return; // 停留时跳过移动逻辑
        }
        
        // 更新连续移动计时器
        continuousMoveTimer += Time.deltaTime;
        
        // 检查是否超过最大连续移动时间（每只小猫不同）
        if (continuousMoveTimer >= currentMaxMoveTime)
        {
            StartResting();
            return;
        }
        
        // 移动小猫
        MoveCat();
        
        // 更新计时器并检查是否需要新目标
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            GetNewRandomTarget();
            // 每次改变目标时也随机化时间间隔
            currentChangeTargetTime = Random.Range(minChangeTargetTime, maxChangeTargetTime);
            timer = currentChangeTargetTime;
        }
    }
    
    void MoveCat()
    {
        // 计算分离力
        Vector2 separation = CalculateSeparation();
        
        // 计算目标方向
        Vector2 targetDirection = (currentTarget - (Vector2)transform.position).normalized;
        
        // 结合目标方向和分离力
        Vector2 finalDirection = targetDirection;
        
        // 如果有分离力，优先处理分离
        if (separation != Vector2.zero)
        {
            finalDirection = separation.normalized;
        }
        
        // 应用移动
        transform.position = Vector2.MoveTowards(
            transform.position, 
            (Vector2)transform.position + finalDirection, 
            moveSpeed * Time.deltaTime
        );
        
        // 根据最终移动方向更新朝向
        if (finalDirection.x > 0)
        {
            spriteRenderer.flipX = false; // 脸朝右
        }
        else
        {
            spriteRenderer.flipX = true;  // 脸朝左
        }
        
        // 如果非常接近目标点，决定下一步行动
        if (Vector2.Distance(transform.position, currentTarget) < 0.1f)
        {
            // 使用每只小猫独立的停留概率
            if (Random.value < currentRestChance)
            {
                StartResting();
            }
            else
            {
                GetNewRandomTarget();
            }
        }
    }
    
    Vector2 CalculateSeparation()
    {
        Vector2 separation = Vector2.zero;
        int neighborCount = 0;
        
        // 检测附近的小猫
        Collider2D[] nearbyCats = Physics2D.OverlapCircleAll(transform.position, avoidanceRadius, catLayerMask);
        
        foreach (Collider2D cat in nearbyCats)
        {
            // 跳过自己
            if (cat.gameObject == gameObject) continue;
            
            // 计算到其他小猫的方向和距离
            Vector2 directionToCat = transform.position - cat.transform.position;
            float distance = directionToCat.magnitude;
            
            // 如果距离太近，添加分离力
            if (distance < avoidanceRadius && distance > 0)
            {
                // 距离越近，分离力越强
                separation += directionToCat.normalized / distance;
                neighborCount++;
            }
        }
        
        // 计算平均分离力
        if (neighborCount > 0)
        {
            separation /= neighborCount;
            separation = separation.normalized * separationForce;
        }
        
        return separation;
    }
    
    void GetNewRandomTarget()
    {
        // 获取主摄像机
        Camera mainCamera = Camera.main;
        
        // 计算摄像机视野范围
        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        // 摄像机中心位置
        Vector2 cameraCenter = mainCamera.transform.position;
        
        // 在摄像机视野内随机生成目标位置
        currentTarget = new Vector2(
            Random.Range(cameraCenter.x - cameraWidth, cameraCenter.x + cameraWidth),
            Random.Range(cameraCenter.y - cameraHeight, cameraCenter.y + cameraHeight)
        );
    }
    
    void StartResting()
    {
        // 设置停留状态
        isResting = true;
        
        // 重置连续移动计时器
        continuousMoveTimer = 0f;
        
        // 随机停留时间（在最小和最大停留时间之间）
        restTimer = Random.Range(minRestTime, maxRestTime);
        
        // 每次停留后也随机化停留概率
        currentRestChance = Random.Range(minRestChance, maxRestChance);
    }
    
    void HandleResting()
    {
        // 更新停留计时器
        restTimer -= Time.deltaTime;
        
        // 如果停留时间结束，恢复移动
        if (restTimer <= 0f)
        {
            isResting = false;
            GetNewRandomTarget();
            
            // 停留结束后重新随机化最大移动时间
            currentMaxMoveTime = Random.Range(minMoveTime, maxMoveTime);
        }
    }
}