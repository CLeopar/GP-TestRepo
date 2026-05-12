using System;
using Fantasy;
using Fantasy.Async;
using Luban;
using UnityEngine;
using YooAsset;

public class GameEntry : MonoBehaviour
{
    public Scene _scene {get; private set;}

    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

    public static GameEntry Instance { get; private set; }
    
    private void Start()
    {
        Instance = this;
        Input.multiTouchEnabled = false;
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
        // 1. 初始化 Fantasy 框架
        await Fantasy.Platform.Unity.Entry.Initialize();

        // 2. 创建一个 Scene (客户端场景)
        // Scene 是 Fantasy 框架的核心容器,所有功能都在 Scene 下运行
        // SceneRuntimeMode.MainThread 表示在 Unity 主线程运行
        _scene = await Scene.Create(SceneRuntimeMode.MainThread);
        
        await _scene.AddComponent<ResourceLoaderComponent>().Init(PlayMode);
        await LoadAllConfigs();
        Debug.Log("Fantasy 框架初始化完成!");

        _scene.AddComponent<Level_1_Component>();
        _scene.AddComponent<PlayerInputComponent>(0);
        _scene.AddComponent<PlayerInputComponent>(1);
        _scene.AddComponent<FoodManagerComponent>();
        _scene.AddComponent<PropsManagerComponent>();
        _scene.AddComponent<TissueManagerComponent>();
        _scene.AddComponent<DogControlComponent>();
    }
    
    private async FTask LoadAllConfigs()
    {
        var itemConfigs = await _scene.GetComponent<ResourceLoaderComponent>()
            .LoadAllAssetsAsync<TextAsset>($"Assets/Bundles/Config/Bin/{nameof(ConstConfigCategory)}.bytes");
        _scene.GetOrAddComponent<Tables>().Init(file => new ByteBuf(itemConfigs[file].bytes));
    }

    private void OnDestroy()
    {
        // 销毁 Scene,释放所有资源
        _scene?.Dispose();
    }
}