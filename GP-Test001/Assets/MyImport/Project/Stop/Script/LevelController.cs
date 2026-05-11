using UnityEngine;

/// <summary>
/// 关卡控制器示例
/// 挂载在每个关卡场景的管理 GameObject 上。
/// 负责监听暂停键并告知 PauseMenu 当前是第几关。
/// </summary>
public class LevelController : MonoBehaviour
{
    [Header("关卡配置")]
    [Tooltip("当前关卡索引（0 = 第一关，1 = 第二关），用于显示对应操作提示图片")]
    [SerializeField] private int levelIndex = 0;

    [Header("暂停菜单预制体")]
    [Tooltip("将 PauseMenu 预制体拖到这里，或运行时动态实例化")]
    [SerializeField] private GameObject pauseMenuPrefab;

    private bool isPaused = false;

    private void Start()
    {
        // 如果场景里还没有 PauseMenu 实例，就实例化预制体
        if (PauseMenu.Instance == null && pauseMenuPrefab != null)
        {
            Instantiate(pauseMenuPrefab);
        }
    }

    private void Update()
    {
        // 按 Escape 键切换暂停状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        // 传入当前关卡索引，PauseMenu 会据此显示对应的操作提示图片
        PauseMenu.Instance?.Open(levelIndex);
    }

    public void Resume()
    {
        isPaused = false;
        PauseMenu.Instance?.Close();
    }
}
