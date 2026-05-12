using System;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;


/// <summary>
/// 远端资源地址查询服务类
/// </summary>
public class RemoteServices : IRemoteServices
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_defaultHostServer}/{fileName}";
    }

    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackHostServer}/{fileName}";
    }
}

public class ResourceLoaderComponent : Entity
{
    public EPlayMode playMode;

    public async FTask Init(EPlayMode ePlayMode)
    {
        playMode = ePlayMode;
        await CreatePackageAsync("DefaultPackage", true);
    }

    public async FTask CreatePackageAsync(string packageName, bool isDefault = false)
    {
        ResourcePackage package = YooAssets.CreatePackage(packageName);
        if (isDefault)
        {
            YooAssets.SetDefaultPackage(package);
        }

        // 编辑器下的模拟模式
        switch (playMode)
        {
            case EPlayMode.EditorSimulateMode:
            {
                var buildResult = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                var packageRoot = buildResult.PackageRootDirectory;
                var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

                var createParameters = new EditorSimulateModeParameters();
                createParameters.EditorFileSystemParameters = fileSystemParams;

                var initOperation = package.InitializeAsync(createParameters);
                await initOperation.Task;

                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    Log.Info("资源包初始化成功！");
                    break;
                }
                else
                {
                    Log.Error($"资源包初始化失败：{initOperation.Error}");
                    return;
                }

                //
                // EditorSimulateModeParameters createParameters = new();
                // createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("ScriptableBuildPipeline", packageName);
                // await package.InitializeAsync(createParameters).Task;
                // break;
            }
            case EPlayMode.OfflinePlayMode:
            {
                var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters = fileSystemParams;

                var initOperation = package.InitializeAsync(createParameters);
                await initOperation.Task;

                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    Log.Info("资源包初始化成功！");
                    break;
                }
                else
                {
                    Log.Error($"资源包初始化失败：{initOperation.Error}");
                    return;
                }
                // OfflinePlayModeParameters createParameters = new();
                // await package.InitializeAsync(createParameters).Task;
                // break;
            }
            case EPlayMode.HostPlayMode:
            {
                string defaultHostServer = GetHostServerURL();
                string fallbackHostServer = GetHostServerURL();
                // string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
                // string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
                IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                var createParameters = new HostPlayModeParameters();
                createParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                createParameters.CacheFileSystemParameters = cacheFileSystemParams;

                var initOperation = package.InitializeAsync(createParameters);
                await initOperation.Task;

                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    Log.Info("资源包初始化成功！");
                    break;
                }
                else
                {
                    Log.Error($"资源包初始化失败：{initOperation.Error}");
                    return;
                }
                // string defaultHostServer = GetHostServerURL();
                // string fallbackHostServer = GetHostServerURL();
                // HostPlayModeParameters createParameters = new();
                // createParameters.BuildinQueryServices = new GameQueryServices();
                // createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                // await package.InitializeAsync(createParameters).Task;
                // break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        // 2. 请求资源清单的版本信息
        var requestPackageVersionAsync = package.RequestPackageVersionAsync();
        await requestPackageVersionAsync.Task;
        if (requestPackageVersionAsync.Status != EOperationStatus.Succeed)
        {
            Log.Error("请求资源清单的版本信息 Error!!!");
            return;
        }

        // 3. 传入的版本信息更新资源清单
        Log.Debug($"PackageVersion == {requestPackageVersionAsync.PackageVersion}");
        var operation = package.UpdatePackageManifestAsync(requestPackageVersionAsync.PackageVersion);
        await operation.Task;
        if (operation.Status != EOperationStatus.Succeed)
        {
            Log.Error("传入的版本信息更新资源清单 Error!!!");
            return;
        }

        Log.Debug("初始化完成!");
    }

    static string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
        string hostServerIP = "http://127.0.0.1";
        string appVersion = "v1.0";

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
        {
            return $"{hostServerIP}/CDN/Android/{appVersion}";
        }
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
        {
            return $"{hostServerIP}/CDN/IPhone/{appVersion}";
        }
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
        {
            return $"{hostServerIP}/CDN/WebGL/{appVersion}";
        }

        return $"{hostServerIP}/CDN/PC/{appVersion}";
#else
            if (Application.platform == RuntimePlatform.Android)
            {
                return $"{hostServerIP}/CDN/Android/{appVersion}";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return $"{hostServerIP}/CDN/IPhone/{appVersion}";
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return $"{hostServerIP}/CDN/WebGL/{appVersion}";
            }

            return $"{hostServerIP}/CDN/PC/{appVersion}";
#endif
    }

    public void DestroyPackage(string packageName)
    {
        ResourcePackage package = YooAssets.GetPackage(packageName);
        package.UnloadUnusedAssetsAsync();
        // package.UnloadUnusedAssets();
    }

    /// <summary>
    /// 主要用来加载dll config aotdll，因为这时候纤程还没创建，无法使用ResourcesLoaderComponent。
    /// 游戏中的资源应该使用ResourcesLoaderComponent来加载
    /// </summary>
    public async FTask<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
    {
        using WaitCoroutineLock coroutineLock = await Scene.CoroutineLockComponent
            .Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
        AssetHandle handle = YooAssets.LoadAssetAsync<T>(location);
        await handle.Task;
        T t = (T)handle.AssetObject;
        handle.Release();
        return t;
    }

    /// <summary>
    /// 主要用来加载dll config aotdll，因为这时候纤程还没创建，无法使用ResourcesLoaderComponent。
    /// 游戏中的资源应该使用ResourcesLoaderComponent来加载
    /// </summary>
    public async FTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(string location) where T : UnityEngine.Object
    {
        using WaitCoroutineLock coroutineLock = await Scene.CoroutineLockComponent
            .Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
        AllAssetsHandle allAssetsOperationHandle = YooAssets.LoadAllAssetsAsync<T>(location);
        await allAssetsOperationHandle.Task;
        Dictionary<string, T> dictionary = new Dictionary<string, T>();
        foreach (UnityEngine.Object assetObj in allAssetsOperationHandle.AllAssetObjects)
        {
            T t = assetObj as T;
            dictionary.Add(t.name, t);
        }

        allAssetsOperationHandle.Release();
        return dictionary;
    }

    public async FTask LoadSceneAsync(string location, LoadSceneMode loadSceneMode)
    {
        using WaitCoroutineLock coroutineLock = await Scene.CoroutineLockComponent
            .Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
        var sceneHandle = YooAssets.LoadSceneAsync(location, loadSceneMode);
        await sceneHandle.Task;
    }
}

public class ResourceLoadComponent_Awake : AwakeSystem<ResourceLoaderComponent>
{
    protected override void Awake(ResourceLoaderComponent self)
    {
        // 初始化资源系统
        YooAssets.Initialize();
    }
}

public class ResourceLoadComponent_Destroy : DestroySystem<ResourceLoaderComponent>
{
    protected override void Destroy(ResourceLoaderComponent self)
    {
        YooAssets.Destroy();
    }
}