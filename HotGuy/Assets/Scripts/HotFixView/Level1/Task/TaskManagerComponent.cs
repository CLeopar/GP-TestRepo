using System.Collections.Generic;
using System.Linq;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TaskManagerComponent : Entity
{
    private const int HARD_CODED_MAX_TASK_COUNT = 2;

    private float SpawnInterval
    {
        get
        {
            var data = Scene.GetComponent<Tables>()?.ConstConfigCategory?.Data;
            if (data == null) return 30f;
            return data.SCSpawnInterval;
        }
    }

    public int MinFoodCount = 2;
    public int MaxFoodCount = 3;

    public List<long> ActiveTaskIds = new List<long>();
    private long _taskIdGenerator = 1000;
    private long _spawnTimer;

    public void Init()
    {
        Log.Error($"[TaskManager] Init, HARD_CODED_MAX={HARD_CODED_MAX_TASK_COUNT}");
        float firstSpawnDelay = 30f;
        _spawnTimer = Scene.TimerComponent.Net.OnceTimer((long)(firstSpawnDelay * 1000), OnSpawnTimer);
    }

    private void StartSpawnTimer()
    {
        float interval = SpawnInterval;
        float randomInterval = Random.Range(interval - 5f, interval + 5f);
        _spawnTimer = Scene.TimerComponent.Net.OnceTimer((long)(randomInterval * 1000), OnSpawnTimer);
    }

    private void OnSpawnTimer()
    {
        if (ActiveTaskIds.Count >= HARD_CODED_MAX_TASK_COUNT || _isSpawning)
        {
            Log.Error($"[TaskManager] Skip (Active={ActiveTaskIds.Count}, Spawning={_isSpawning})");
            StartSpawnTimer();
            return;
        }

        TrySpawnTask();
        StartSpawnTimer();
    }

    private bool _isSpawning = false;

    private async void TrySpawnTask()
    {
        if (_isSpawning) return;
        _isSpawning = true;

        try
        {
            var taskComp = await GenerateTask();
            if (taskComp != null)
            {
                ActiveTaskIds.Add(taskComp.Id);
                Log.Error($"[TaskManager] Spawned {taskComp.Id}, Active={ActiveTaskIds.Count}/{HARD_CODED_MAX_TASK_COUNT}");
            }
        }
        finally
        {
            _isSpawning = false;
        }
    }

    private async FTask<TaskComponent> GenerateTask()
    {
        var availableFoods = GetAvailableFoods();
        if (availableFoods.Count == 0)
        {
            Log.Error("[TaskManager] No available foods");
            return null;
        }

        var foodCounts = new Dictionary<FoodType, int>();
        foreach (var ft in availableFoods)
        {
            if (!foodCounts.ContainsKey(ft))
                foodCounts[ft] = 0;
            foodCounts[ft]++;
        }

        int count = Random.Range(MinFoodCount, MaxFoodCount + 1);
        var foodSequence = new List<FoodType>();
        var tempCounts = new Dictionary<FoodType, int>(foodCounts);

        for (int i = 0; i < count; i++)
        {
            var candidates = tempCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            if (candidates.Count == 0)
            {
                Log.Error("[TaskManager] Not enough food instances");
                return null;
            }

            var selected = candidates[Random.Range(0, candidates.Count)];
            foodSequence.Add(selected);
            tempCounts[selected]--;
        }

        if (IsSequenceDuplicate(foodSequence))
        {
            Log.Error("[TaskManager] Duplicate sequence, skip");
            return null;
        }

        var taskComp = AddComponent<TaskComponent>(++_taskIdGenerator);
        taskComp.FoodSequence = foodSequence;
        taskComp.IsCompleted = false;
        taskComp.IsFailed = false;

        var scItemDataList = new List<SCItemData>();
        var config = Scene.GetComponent<Tables>()?.ConstConfigCategory?.Data;

        float rand = Random.value;
        float greenProb = config?.SCGreenProbability ?? 0.5f;
        var durationType = rand < greenProb ? SCDurationType.Green_10s : SCDurationType.Orange_8s;

        // 计算总时长
        float totalDuration = durationType == SCDurationType.Green_10s 
            ? config.SCGreenDuration 
            : config.SCOrangeDuration;

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
        }

        // 【修复】任务只有一个总倒计时，在TaskComponent上管理
        taskComp.StartCountdown(totalDuration);

        Scene.EventComponent.Publish(new SCTaskSpawned
        {
            TaskId = taskComp.Id,
            FoodSequence = foodSequence,
            SCItems = scItemDataList
        });

        return taskComp;
    }

    private List<FoodType> GetAvailableFoods()
    {
        var result = new List<FoodType>();
        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null) return result;

        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is FoodComponent food)
            {
                if (food.foodType == FoodType.None) continue;
                if (food.Fruit_Go == null || food.Fruit_Tr == null) continue;
                if (food.fruitStateType == FruitStateType.BeEaten) continue;

                var dogControl = Scene.GetComponent<DogControlComponent>();
                if (dogControl != null && dogControl.CurEatFoodData.Item2 == food.Id) continue;

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
            if (taskComp == null || taskComp.IsCompleted || taskComp.IsFailed) continue;
            if (taskComp.FoodSequence.Count != sequence.Count) continue;

            bool same = true;
            for (int i = 0; i < sequence.Count; i++)
            {
                if (taskComp.FoodSequence[i] != sequence[i])
                {
                    same = false;
                    break;
                }
            }
            if (same) return true;
        }
        return false;
    }

    public void RemoveTask(long taskId, bool silent = false)
    {
        var taskComp = GetComponent<TaskComponent>(taskId);
        if (taskComp != null)
        {
            // 【修复】先停倒计时
            taskComp.StopCountdown();

            var itemsToRemove = new List<SCItemComponent>();
            foreach (var item in taskComp.ForEachMultiEntity)
            {
                if (item is SCItemComponent itemComp)
                {
                    itemsToRemove.Add(itemComp);
                }
            }
            foreach (var itemComp in itemsToRemove)
                taskComp.RemoveComponent(itemComp);

            RemoveComponent(taskComp);
        }

        ActiveTaskIds.Remove(taskId);

        if (!silent)
            Scene.EventComponent.Publish(new SCTaskTimeout { TaskId = taskId });

        Log.Error($"[TaskManager] Removed {taskId}, Active={ActiveTaskIds.Count}");
    }

    public void CompleteTask(long taskId)
    {
        var taskComp = GetComponent<TaskComponent>(taskId);
        if (taskComp == null) return;

        taskComp.IsCompleted = true;
        taskComp.StopCountdown();

        Scene.EventComponent.Publish(new SCTaskCompleted { TaskId = taskId });

        Log.Error($"[TaskManager] Completed {taskId}, Active={ActiveTaskIds.Count}");
    }

    public void ClearAllTasks()
    {
        var idsToRemove = new List<long>(ActiveTaskIds);
        foreach (var taskId in idsToRemove)
            RemoveTask(taskId);

        Scene.TimerComponent.Net.Remove(ref _spawnTimer);
        Log.Error("[TaskManager] All cleared");
    }
}

public class TaskManagerComponent_Awake : AwakeSystem<TaskManagerComponent>
{
    protected override void Awake(TaskManagerComponent self) => self.Init();
}

public class TaskManagerComponent_Destroy : DestroySystem<TaskManagerComponent>
{
    protected override void Destroy(TaskManagerComponent self) => self.ClearAllTasks();
}
