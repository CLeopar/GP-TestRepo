using Fantasy;
using Fantasy.Event;

public class Event_PlaySFX_Handler : EventSystem<PlaySFX>
{
    protected override void Handler(PlaySFX self)
    {
        Log.Error($"[PlaySFX] 🔊 Event: Type={self.Type}, Pos={self.WorldPos}");
        
        var am = GameEntry.Instance._scene.GetComponent<AudioManagerComponent>();
        if (am == null)
        {
            Log.Error("[PlaySFX] ❌ AudioManagerComponent is NULL!");
            return;
        }
        
        Log.Error("[PlaySFX] AudioManager found, calling Play...");
        am.Play(self.Type, self.WorldPos);
    }
}