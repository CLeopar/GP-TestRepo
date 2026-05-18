using System.Collections.Generic;
using System.Linq;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TaskManagerComponent : Entity
{
    private float SpawnInterval => Scene.GetComponent<Tables>().ConstConfigCategory.Data.SCSpawnInterval;
    private int MaxTaskCount => Scene.GetComponent<Tables>().ConstConfigCategory.Data.SCMaxTaskCount;

    public int MinFoodCount = 2;
    public int MaxFoodCount = 3;

    public List<long> ActiveTaskIds = new List<long>();
    private long _taskIdGenerator = 1000;
    private long _spawnTimer;

    public void Init()
    {
        Log.Error("[TaskManager] === Init called ===");
        StartSpawnTimer();
        Log.Error($"[TaskManager] Initialized, SpawnInterval={SpawnInterval}s, MaxTaskCount={MaxTaskCount}");
    }

    private void StartSpawnTimer()
    {
        float interval = SpawnInterval;
        float randomInterval = Random.Range(interval - 5f, interval + 5f);
        _spawnTimer = Scene.TimerComponent.Net.OnceTimer((long)(randomInterval * 1000), OnSpawnTimer);
    }

    private void OnSpawnTimer()
    {
        Log.Error($"[TaskManager] === OnSpawnTimer called, ActiveTasks: {ActiveTaskIds.Count} ===");

        if (ActiveTaskIds.Count >= MaxTaskCount)
        {
            Log.Error($"[TaskManager] Max task count reached ({MaxTaskCount}), skip spawn");
            StartSpawnTimer();
            return;
        }

        TrySpawnTask();
        StartSpawnTimer();
    }

    private async void TrySpawnTask()
    {
        Log.Error("[TaskManager] === TrySpawnTask called ===");
        var taskComp = await GenerateTask();
        if (taskComp != null)
        {
            ActiveTaskIds.Add(taskComp.Id);
            Log.Error($"[TaskManager] New task spawned: {taskComp.Id}");
        }
        else
        {
            Log.Error("[TaskManager] GenerateTask returned null");
        }
    }

   private async FTask<TaskComponent> GenerateTask()
{
    // 1. 获取可用食物列表（平铺，每个实例占一项）
    var availableFoods = GetAvailableFoods();
    if (availableFoods.Count == 0)
    {
        Log.Error("[TaskManager] No available foods");
        return null;
    }

    // ========== 新增：统计场景中每种类型的实际数量 ==========
    var foodCounts = new Dictionary<FoodType, int>();
    foreach (var ft in availableFoods)
    {
        if (!foodCounts.ContainsKey(ft))
            foodCounts[ft] = 0;
        foodCounts[ft]++;
    }

    // 2. 生成食物序列（带数量扣除，防止超抽）
    int count = Random.Range(MinFoodCount, MaxFoodCount + 1);
    var foodSequence = new List<FoodType>();
    var tempCounts = new Dictionary<FoodType, int>(foodCounts);

    for (int i = 0; i < count; i++)
    {
        // 只从还有剩余数量的类型里选
        var candidates = tempCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
        if (candidates.Count == 0)
        {
            Log.Error("[TaskManager] Not enough food instances to generate sequence");
            return null;
        }

        var selected = candidates[Random.Range(0, candidates.Count)];
        foodSequence.Add(selected);
        tempCounts[selected]--;  // 扣掉一个库存
    }

    // 3. 检查是否与已有任务重复
    if (IsSequenceDuplicate(foodSequence))
    {
        Log.Error("[TaskManager] Duplicate sequence, skip");
        return null;
    }

    // 4. 创建 TaskComponent（后续代码完全不变）
    var taskComp = AddComponent<TaskComponent>(++_taskIdGenerator);
    taskComp.FoodSequence = foodSequence;
    taskComp.IsCompleted = false;
    taskComp.IsFailed = false;

    // 5. 创建 SCItemComponent 并收集 SCItemData
    var scItemDataList = new List<SCItemData>();
    var config = Scene.GetComponent<Tables>().ConstConfigCategory.Data;

    float rand = Random.value;
    var durationType = rand < config.SCGreenProbability
        ? SCDurationType.Green_10s
        : SCDurationType.Orange_8s;
    
    for (int i = 0; i < foodSequence.Count; i++)
    {
        var itemComp = taskComp.AddComponent<SCItemComponent>(i);
        itemComp.FoodType = foodSequence[i];
        itemComp.DurationType = durationType;
        itemComp.UIState = SCUIState.Normal;
        itemComp.IsCompleted = false;
        itemComp.TaskId = taskComp.Id;
        itemComp.Index = i;

        scItemDataList.Add(new SCItemData
        {
            Index = i,
            FoodType = foodSequence[i],
            DurationType = durationType
        });

        Log.Error($"[TaskManager] Item {i}: Type={durationType}, ConfigTime={itemComp.TotalDuration}s");
    }

    // 6. 发布 SCTaskSpawned 事件通知 UI
    Scene.EventComponent.Publish(new SCTaskSpawned
    {
        TaskId = taskComp.Id,
        FoodSequence = foodSequence,
        SCItems = scItemDataList
    });

    // 7. 启动所有 Item 的倒计时
    taskComp.StartCountdown();

    return taskComp;
}

    /// <summary>
    /// 获取当前场景中真正可用的食物（最严格过滤）
    /// </summary>
    private List<FoodType> GetAvailableFoods()
    {
        var result = new List<FoodType>();
        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null)
        {
            Log.Error("[TaskManager] FoodManagerComponent not found!");
            return result;
        }

        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is FoodComponent food)
            {
                // ========== 严格过滤 1：排除尚未完成异步初始化的实体 ==========
                // AddComponent 是同步的，但 Init/LoadItem 是异步的。
                // 如果 foodType 还是默认值 None，说明赋值尚未完成。
                if (food.foodType == FoodType.None)
                    continue;

                // ========== 严格过滤 2：排除 GameObject 尚未实例化完成的 ==========
                // Fruit_Go 在 LoadItem 的 await 之后才被赋值，如果为 null 则资源还没加载完。
                if (food.Fruit_Go == null || food.Fruit_Tr == null)
                    continue;

                // ========== 严格过滤 3：排除已被吃完的 ==========
                if (food.fruitStateType == FruitStateType.BeEaten)
                    continue;

                // ========== 严格过滤 4：排除正在被狗吃的 ==========
                var dogControl = Scene.GetComponent<DogControlComponent>();
                if (dogControl != null && dogControl.CurEatFoodData.Item2 == food.Id)
                    continue;

                result.Add(food.foodType);
            }
        }

        return result;
    }

    private bool IsSequenceDuplicate(List<FoodType> sequence)
    {
        foreach (var taskId in ActiveTaskIds)
        {
            var taskComp = GetComponent<TaskComponent>(taskId);
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed)
                continue;

            if (taskComp.FoodSequence.Count != sequence.Count)
                continue;

            bool same = true;
            for (int i = 0; i < sequence.Count; i++)
            {
                if (taskComp.FoodSequence[i] != sequence[i])
                {
                    same = false;
                    break;
                }
            }

            if (same)
                return true;
        }

        return false;
    }

    public void RemoveTask(long taskId, bool silent = false)
    {
        if (!ActiveTaskIds.Contains(taskId))
            return;

        var taskComp = GetComponent<TaskComponent>(taskId);
        if (taskComp != null)
        {
            var itemsToRemove = new List<SCItemComponent>();
            foreach (var item in taskComp.ForEachMultiEntity)
            {
                if (item is SCItemComponent itemComp)
                {
                    itemComp.StopCountdown();
                    itemsToRemove.Add(itemComp);
                }
            }

            foreach (var itemComp in itemsToRemove)
            {
                taskComp.RemoveComponent(itemComp);
            }

            RemoveComponent(taskComp);
        }

        ActiveTaskIds.Remove(taskId);

        // silent=true 时是完成后的清理，不发超时事件（UI动画已经播完了）
        if (!silent)
            Scene.EventComponent.Publish(new SCTaskTimeout { TaskId = taskId });

        Log.Error($"[TaskManager] Task removed: {taskId}, silent: {silent}");
    }

    public void CompleteTask(long taskId)
    {
        var taskComp = GetComponent<TaskComponent>(taskId);
        if (taskComp == null) return;

        taskComp.IsCompleted = true;
        taskComp.StopAllCountdowns();

        Scene.EventComponent.Publish(new SCTaskCompleted { TaskId = taskId });
        
    }

    public void ClearAllTasks()
    {
        var idsToRemove = new List<long>(ActiveTaskIds);
        foreach (var taskId in idsToRemove)
        {
            RemoveTask(taskId);
        }

        Scene.TimerComponent.Net.Remove(ref _spawnTimer);
        Log.Error("[TaskManager] All tasks cleared");
    }
}

public class TaskManagerComponent_Awake : AwakeSystem<TaskManagerComponent>
{
    protected override void Awake(TaskManagerComponent self)
    {
        self.Init();
    }
}

public class TaskManagerComponent_Destroy : DestroySystem<TaskManagerComponent>
{
    protected override void Destroy(TaskManagerComponent self)
    {
        self.ClearAllTasks();
    }
}