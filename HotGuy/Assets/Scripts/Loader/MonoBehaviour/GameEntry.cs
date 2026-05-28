using System;
using Fantasy;
using Fantasy.Async;
using Luban;
using UnityEngine;
using YooAsset;

public class GameEntry : MonoBehaviour
{
    public Scene _scene { get; private set; }
    public EPlayMode PlayMode;
    public static GameEntry Instance { get; private set; }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[GameEntry] 发现重复实例！销毁多余的");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Input.multiTouchEnabled = false;
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
#if UNITY_EDITOR
        PlayMode = EPlayMode.EditorSimulateMode;
#else
        PlayMode = EPlayMode.OfflinePlayMode;
#endif

        try
        {
            Debug.Log("[GameEntry] Initializing Fantasy...");
            await Fantasy.Platform.Unity.Entry.Initialize();
            Debug.Log("[GameEntry] Fantasy initialized OK");

            Debug.Log("[GameEntry] Creating Scene...");
            _scene = await Scene.Create(SceneRuntimeMode.MainThread);
            Debug.Log("[GameEntry] Scene created OK");

            Debug.Log("[GameEntry] Loading ResourceLoader...");
            await _scene.AddComponent<ResourceLoaderComponent>().Init(PlayMode);
            Debug.Log("[GameEntry] ResourceLoader OK");

            Debug.Log("[GameEntry] Loading configs...");
            await LoadAllConfigs();
            Debug.Log("[GameEntry] Configs loaded OK");

            Debug.Log("[GameEntry] Adding components...");
            _scene.AddComponent<Level_1_Component>();
            _scene.AddComponent<PlayerInputComponent>(0);
            _scene.AddComponent<PlayerInputComponent>(1);
            _scene.AddComponent<FoodManagerComponent>();
            _scene.AddComponent<PropsManagerComponent>();
            _scene.AddComponent<TissueManagerComponent>();
            _scene.AddComponent<DogControlComponent>();
            //_scene.AddComponent<CameraShakeComponent>();
            //_scene.AddComponent<FoodParticleEffectComponent>();
           // _scene.AddComponent<LevelTimerUIComponent>();
            //_scene.AddComponent<ScoreUIComponent>();
           // _scene.AddComponent<SCUIComponent>();
           // _scene.AddComponent<TaskManagerComponent>();
          //  _scene.AddComponent<DanmakuManagerComponent>();
           // _scene.AddComponent<DanmakuUIComponent>();
          //  _scene.AddComponent<LevelStatsComponent>();
          //  _scene.AddComponent<FadePanelUIComponent>();
          //  _scene.AddComponent<FoodBoundaryComponent>();
            Debug.Log("[GameEntry] All components added OK");
        }
        catch (Exception e)
        {
            Debug.LogError("[GameEntry] FAILED: " + e);
        }
    }

    private async FTask LoadAllConfigs()
    {
        var itemConfigs = await _scene.GetComponent<ResourceLoaderComponent>()
            .LoadAllAssetsAsync<TextAsset>($"Assets/Bundles/Config/Bin/{nameof(ConstConfigCategory)}.bytes");

        var tables = _scene.GetComponent<Tables>();
        if (tables == null)
            tables = _scene.AddComponent<Tables>();

        tables.Init(file => new ByteBuf(itemConfigs[file].bytes));
    }

    private void OnDestroy()
    {
        _scene?.Dispose();
    }
}