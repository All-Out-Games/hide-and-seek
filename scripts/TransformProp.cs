using AO;

public partial class TransformProp : Component
{
    public Interactable Interactable;
    
    [Serialized] public int PropIndex; 

    public override void Start()
    {
        Interactable = Entity.GetComponent<Interactable>();
        Interactable.CanUseCallback = p =>
        {
            if (GameManager.Instance.State == GameState.WaitingForPlayers
            || GameManager.Instance.State == GameState.CountingDown
            || GameManager.Instance.State == GameState.EndRound) return false;

            return ((HNSPlayer)p).PlayerRole == PlayerRole.Prop;
        };
        Interactable.OnInteract = p =>
        {
            var swapAbility = p.GetAbility<SwapAbility>();
            if (swapAbility != null)
            {
                if (swapAbility.CooldownRemaining > 0f) return;
                swapAbility.CooldownRemaining = 10.0f;
            }

          
            if (Network.IsServer)
            {
                var player = p as HNSPlayer;
                player.CurrentPropIndex.Set(PropIndex);
              
            }
        };
              
    }

    public override void Update()
    {
        if (Interactable == null) return;
        var localPlayer = (HNSPlayer)Network.LocalPlayer;
        if (localPlayer.Alive())
        {
            var swapAbility = localPlayer.GetAbility<SwapAbility>();
            if (swapAbility != null)
            {
                if (swapAbility.CooldownRemaining > 0f)
                {
                    Interactable.Text = $"Wait for {(int)swapAbility.CooldownRemaining}s";
                    Interactable.HoldText = $"Wait for {(int)swapAbility.CooldownRemaining}s";
                }
                else
                {
                    Interactable.Text = $"Transform into";
                    Interactable.HoldText = $"Hold";
                }
            }
        }
    }
}