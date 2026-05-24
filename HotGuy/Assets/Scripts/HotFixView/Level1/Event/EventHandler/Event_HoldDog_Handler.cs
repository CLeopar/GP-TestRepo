using Fantasy;
using Fantasy.Event;

public class Event_HoldDog_Handler : EventSystem<HoldDog>
{
    protected override void Handler(HoldDog self)
    {
        var dogCtrl = GameEntry.Instance._scene.GetComponent<DogControlComponent>();
        dogCtrl?.ChangeDogState(self.State ? DogState.Hold : DogState.Normal, self.isL);
        
        // 新增：播放/停止握住音效
        var audioMgr = GameEntry.Instance._scene.GetComponent<AudioManagerComponent>();
        if (self.State)
        {
            audioMgr?.Play(SFXType.DogWhimperSad).Coroutine();
        }
        else
        {
            audioMgr?.StopDogWhimperSad();
        }
    }
}