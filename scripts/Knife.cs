using AO;

public partial class KnifeAbility : MyAbility
{
    public override TargettingMode TargettingMode => TargettingMode.Line;
    public override Texture Icon => Assets.GetAsset<Texture>("Ability_Icons/revolver_icon.png");
    public override Type Effect => typeof(KnifeSwingEffect);
    public override Type TargettingEffect => typeof(AimingKnife);
    public override float MaxDistance => 10f;
    public override float Cooldown => 4;

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;
        if (Player.PlayerRole != PlayerRole.Seeker) return false;
        return true;
    }

    [ClientRpc]
    public static void KillPlayer(HNSPlayer player, HNSPlayer killer)
    {
        player.AddEffect<KillEffect>(preInit: effect =>
        {
            effect.DeathSource = KillEffect.DeathSourceEnum.Knife;
        });
    }
}

public class AimingKnife : MyEffect
{
    public override bool IsActiveEffect => true;
    public override List<Type> AbilityWhitelist { get; } = new List<Type>(){typeof(KnifeAbility)};

    public override void OnEffectStart(bool isDropIn)
    {
    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    public override void OnEffectUpdate()
    {
    }
}

public class KnifeSwingEffect : MyEffect
{
    public override bool IsActiveEffect => true;

    public bool Done;

    public override void OnEffectStart(bool isDropIn)
    {
        Player.SpineAnimator.SpineInstance.StateMachine.SetTrigger("murder_attack");
        SFX.Play(Assets.GetAsset<AudioAsset>("sfx/MurderMystery Killer/killer-knife_swipe.wav"), new(){Positional=true, Position=Player.Position});
        Player.SetAimTarget(Player.Position + AbilityPositionOrDirection);
        DurationRemaining = Player.SpineAnimator.SpineInstance.StateMachine.TryGetLayerByName("murder_layer").GetCurrentStateLength();
    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    public override void OnEffectUpdate()
    {
        if (Util.OneTime(ElapsedTime >= 0.35f, ref Done))
        {
            var hitPos = Player.Position + new Vector2(0, 0.5f) + AbilityPositionOrDirection * 1;
            foreach (var p in AO.Player.AllPlayers)
            {
                var player = (HNSPlayer)p;
                if (player == Player) continue;
                if (player.PlayerRole != PlayerRole.Prop) continue;
                var playerCenter = player.Position + new Vector2(0, 0.5f);
                if ((playerCenter - hitPos).Length < 1)
                {
                    if (Network.IsServer)
                    {
                        KnifeAbility.CallClient_KillPlayer(player, Player);
                    }
                }
            }
        }
    }
}