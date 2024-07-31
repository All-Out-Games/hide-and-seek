using AO;

public class HNSPlayer : Player
{
    private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRole
    {
        get => (PlayerRole)_playerRole.Value;
        set => _playerRole.Set((int)value);
    }

    public SyncVar<int> CurrentPropIndex = new(0);

    public override void Awake()
    {
        SpineAnimator.Entity.LocalScale = new Vector2(0.528f, 0.528f);
        CurrentPropIndex.OnSync += OnPropChange;
    }

    public void OnPropChange(int old, int newValue)
    {
    }
}