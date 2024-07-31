using AO;

public class SwapAbility : Ability
{
    public override TargettingMode TargettingMode => TargettingMode.Self;

    public override bool CanTarget(Player player)
    {
        return true;
    }

    public override bool CanUse()
    {
        return (Player as HNSPlayer).PlayerRole == PlayerRole.Prop;
    }

    public override bool OnTryActivate(List<Player> targetPlayers, Vector2 positionOrDirection, float magnitude)
    {
        if (Network.IsServer)
        {
            var player = Player as HNSPlayer;
            player.CurrentPropIndex.Set(player.CurrentPropIndex + 1);
        }
        return true;
    }
}