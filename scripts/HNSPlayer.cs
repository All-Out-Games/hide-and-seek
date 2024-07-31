using AO;

public class HNSPlayer : Player
{
    private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRole
    {
        get => (PlayerRole)_playerRole.Value;
        set => _playerRole.Set((int)value);
    }

    public override void Awake()
    {
        SpineAnimator.Entity.LocalScale = new Vector2(0.528f, 0.528f);
    }
}