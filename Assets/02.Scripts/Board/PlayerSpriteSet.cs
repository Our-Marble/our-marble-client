using UnityEngine;

public enum PlayerPawnAnimation
{
    Idle,
    Hop,
    Cheer,
    Sad
}

public enum PlayerPawnDirection
{
    SouthEast,
    SouthWest,
    NorthEast,
    NorthWest
}

[CreateAssetMenu(fileName = "PlayerSpriteSet", menuName = "OurMarble/Player Sprite Set")]
public class PlayerSpriteSet : ScriptableObject
{
    [SerializeField] private Sprite[] idle;
    [SerializeField] private Sprite[] hop;
    [SerializeField] private Sprite[] cheer;
    [SerializeField] private Sprite[] sad;

    public int GetFrameCount(PlayerPawnAnimation animation)
    {
        switch (animation)
        {
            case PlayerPawnAnimation.Hop: return 8;
            case PlayerPawnAnimation.Idle:
            case PlayerPawnAnimation.Cheer:
            case PlayerPawnAnimation.Sad:
                return 6;
            default: return 0;
        }
    }

    public float GetDefaultDuration(PlayerPawnAnimation animation)
    {
        switch (animation)
        {
            case PlayerPawnAnimation.Idle: return 1.2f;
            case PlayerPawnAnimation.Hop: return 0.68f;
            case PlayerPawnAnimation.Cheer: return 0.72f;
            case PlayerPawnAnimation.Sad: return 1.02f;
            default: return 1f;
        }
    }

    public Sprite GetFrame(PlayerPawnAnimation animation, PlayerPawnDirection direction, int frameIndex)
    {
        Sprite[] frames = GetFrames(animation);
        int frameCount = GetFrameCount(animation);
        int index = (int)direction * frameCount + Mathf.Clamp(frameIndex, 0, frameCount - 1);
        return frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
    }

    public void SetFrames(Sprite[] idleFrames, Sprite[] hopFrames, Sprite[] cheerFrames, Sprite[] sadFrames)
    {
        idle = idleFrames;
        hop = hopFrames;
        cheer = cheerFrames;
        sad = sadFrames;
    }

    private Sprite[] GetFrames(PlayerPawnAnimation animation)
    {
        switch (animation)
        {
            case PlayerPawnAnimation.Idle: return idle;
            case PlayerPawnAnimation.Hop: return hop;
            case PlayerPawnAnimation.Cheer: return cheer;
            case PlayerPawnAnimation.Sad: return sad;
            default: return null;
        }
    }
}
