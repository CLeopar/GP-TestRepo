using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;

public class TaskComponent : Entity, ISupportedMultiEntity
{
    public List<FoodType> FoodSequence = new List<FoodType>();
    public int CurrentStep = 0;
    public bool IsCompleted = false;
    public bool IsFailed = false;

    public void StartCountdown()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
                itemComp.StartCountdown();
        }
    }

    public void StopAllCountdowns()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent itemComp)
                itemComp.StopCountdown();
        }
    }

    public void AdvanceStep()
    {
        if (CurrentStep < FoodSequence.Count - 1)
        {
            CurrentStep++;
            Log.Error($"[TaskComponent] Task {Id} advanced to step {CurrentStep}");
        }
        else
        {
            IsCompleted = true;
            var manager = GetParent<TaskManagerComponent>();
            manager?.CompleteTask(Id);
        }
    }

    public FoodType GetCurrentFoodType()
    {
        if (CurrentStep < FoodSequence.Count)
            return FoodSequence[CurrentStep];
        return FoodType.None;
    }

    public SCItemComponent GetCurrentItem()
    {
        foreach (var item in ForEachMultiEntity)
        {
            if (item is SCItemComponent scItem && scItem.Index == CurrentStep)
                return scItem;
        }
        return null;
    }

    public async FTask CheckAndSupplementCurrentFood()
    {
        var currentFoodType = GetCurrentFoodType();
        if (currentFoodType == FoodType.None) return;

        var foodManager = Scene.GetComponent<FoodManagerComponent>();
        if (foodManager == null) return;

        var dogCtrl = Scene.GetComponent<DogControlComponent>();

        bool hasFoodInScene = false;
        foreach (var item in foodManager.ForEachMultiEntity)
        {
            if (item is not FoodComponent food) continue;
            if (food.foodType != currentFoodType) continue;
            if (food.fruitStateType == FruitStateType.BeEaten) continue;
            if (food.isInPickUp) continue;
            if (food.Fruit_Go == null || food.Fruit_Tr == null) continue;
            if (dogCtrl != null && dogCtrl.CurEatFoodData.Item2 == food.Id) continue;

            hasFoodInScene = true;
            break;
        }

        if (!hasFoodInScene)
        {
            Log.Error($"[TaskComponent] Task {Id} step {CurrentStep} needs {currentFoodType} but none in scene, spawning...");
            await foodManager.AddNewFruitOfType(currentFoodType);
        }
    }
}