using AO;

public partial class GunAbility : MyAbility
{
    public override TargettingMode TargettingMode => TargettingMode.Line;
    public override Texture Icon => Assets.GetAsset<Texture>("Ability_Icons/revolver_icon.png");
    public override Type TargettingEffect => typeof(AimingGun);
    public override float MaxDistance => 10f;
    public override float Cooldown => 15f;

    public override bool OnTryActivate(List<Player> targetPlayers, Vector2 positionOrDirection, float magnitude)
    {
        Game.SpawnProjectile(Player, "Bullet.prefab", "detective_bullet", Player.Entity.Position, positionOrDirection);
        SFX.Play(Assets.GetAsset<AudioAsset>("sfx/revolver_shoot.wav"), new SFX.PlaySoundDesc(){Positional=true, Position=Player.Entity.Position});
        return true;
    }

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;
        if (Player.PlayerRole != PlayerRole.Hunter) return false;
        return true;
    }
}

public class AimingGun : MyEffect
{
    public override bool IsActiveEffect => true;
    public override List<Type> AbilityWhitelist { get; } = new List<Type>(){typeof(GunAbility)};
    public Entity GunEntity;

    public override void OnEffectStart(bool isDropIn)
    {
        GunEntity = Entity.Instantiate(Assets.GetAsset<Prefab>("Gun.prefab"));
        Player.SetMouseIKEnabled(true);
    }

    public override void OnEffectEnd(bool interrupt)
    {
        GunEntity.Destroy();
        Player.SetMouseIKEnabled(false);
    }

    public override void OnEffectUpdate()
    {
        GunEntity.Position = Player.SpineAnimator.GetBonePosition("Hand_R");
        GunEntity.Rotation = Player.SpineAnimator.GetBoneRotation("Hand_R") * (Player.Entity.LocalScaleX < 0 ? -1 : 1) + (Player.Entity.LocalScaleX < 0 ? 180 : 0);
        GunEntity.LocalScaleY = Player.Entity.LocalScaleX < 0 ? -1 : 1;
    }
}

public class DetectiveGun : Component
{
    [Serialized] public Entity Barrell;
    [Serialized] public Entity BarrellTarget;
}

public partial class GunProjectile : Component
{
    public float Lifetime;
    public const float MaxLife = 1f;
    public bool AlreadyHitSomething;

    public override void Start()
    {
        Entity.GetComponent<Projectile>().OnHit += OnHit;
    }

    public override void Update()
    {
        Lifetime += Time.DeltaTime;
        if (Lifetime > MaxLife)
        {
            Entity.Destroy();
        }
    }

    private void OnHit(Entity other, bool predicted)
    {
        if (AlreadyHitSomething) return;
        if (other.GetComponent<ProjectileIgnore>() != null) return;

        HNSPlayer player = null;
        var collisionChild = other.GetComponent<PlayerCollisionChild>();
        if (collisionChild != null)
        {
            player = collisionChild.Player;
        }

        if (player == null) return;
        if (player.HasEffect<SpectatorEffect>()) return;

        var projectile = Entity.GetComponent<Projectile>();
        if (player == projectile.Owner) return;

        // HIT CONFIRMED
        AlreadyHitSomething = true;
        if (predicted == false)
        {
            CallClient_KillPlayer(player, (HNSPlayer) projectile.Owner);
        }

        Entity.Destroy();
    }

    [ClientRpc]
    public static void KillPlayer(HNSPlayer player, HNSPlayer killer)
    {
        player.AddEffect<KillEffect>(preInit: effect =>
        {
            effect.DeathSource = KillEffect.DeathSourceEnum.Bullet;
        });
    }
}