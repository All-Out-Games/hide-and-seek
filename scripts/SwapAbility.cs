using AO;

public class SwapAbility : MyAbility
{
    public override TargettingMode TargettingMode => TargettingMode.Self;
    public override Texture Icon => Assets.GetAsset<Texture>("Ability_Icons/hidenseek/randomize_1.png");
    
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