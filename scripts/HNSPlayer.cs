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

public abstract class MyEffect : AEffect
{
    public new HNSPlayer Player => (HNSPlayer)base.Player;
}

public class WaitForAnimEffect : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public override void OnEffectStart(bool isDropIn)
    {
        if (!isDropIn)
        {
            DurationRemaining = Player.SpineAnimator.SpineInstance.StateMachine.TryGetLayerByIndex(0).GetCurrentStateLength();
        }
    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    public override void OnEffectUpdate()
    {
    }
}

public class SpectatorEffect : MyEffect
{
    public override bool IsActiveEffect => false;
    public override bool IsValidTarget => false;

    public bool HasNameInvisReason = false;

    public void UpdateInvis()
    {
        if (Player.IsLocal || (Network.LocalPlayer.Alive() && Network.LocalPlayer.HasEffect<SpectatorEffect>()))
        {
            Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0.5f);
            if (HasNameInvisReason)
            {
                HasNameInvisReason = false;
                Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
            }
        }
        else
        {
            Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0);
            if (!HasNameInvisReason)
            {
                HasNameInvisReason = true;
                Player.AddNameInvisibilityReason(nameof(SpectatorEffect));
            }
        }
    }

    public override void OnEffectStart(bool isDropIn)
    {
        UpdateInvis();
        Player.SpineAnimator.DepthOffset = -10000;
        Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", true);
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = false;
        if (!isDropIn)
        {
            Player.AddEmoteBlockReason(nameof(SpectatorEffect));
        }

        if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
        {
            Game.SetVoiceEnabled(false);
        }
    }

    public override void OnEffectUpdate()
    {
        UpdateInvis();
    }

    public override void OnEffectEnd(bool interrupt)
    {
        Player.RemoveEmoteBlockReason(nameof(SpectatorEffect));
        Player.SpineAnimator.DepthOffset = 0;
        Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", false);
        Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
        if (HasNameInvisReason)
        {
            Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
        }
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = true;

        if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
        {
            Game.SetVoiceEnabled(true);
        }
    }
}

public partial class RoundStartAnimationEffect : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public float Hold01;

    public override void OnEffectStart(bool isDropIn)
    {
    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    [ServerRpc]
    public static void ServerSkipIntro()
    {
        var player = Network.GetRemoteCallContextPlayer();
        if (player == null) return;
        CallClient_SkipIntro(player);
    }

    [ClientRpc]
    public static void SkipIntro(Player player)
    {
        player.RemoveEffect<RoundStartAnimationEffect>(true);
    }

    public override void OnEffectUpdate()
    {
        var totalTime = 6f;
        if (Player.IsLocal)
        {
            var bgTint01 = Ease.FadeInAndOut(0.1f, totalTime, ElapsedTime);

            using var _1 = UI.PUSH_LAYER(GameManager.IntroLayer);
            using var _2 = UI.PUSH_COLOR_MULTIPLIER(new Vector4(bgTint01, bgTint01, bgTint01, bgTint01));

            UI.Image(UI.ScreenRect, null, new Vector4(0, 0, 0, 0.9f));

            var pos01 = Ease.SlideInAndOut(0.1f, totalTime, ElapsedTime);
            var ts = GameManager.GetTextSettings(52);
            ts.Color = Vector4.White;
            ts.WordWrap = true;
            var rect = UI.SafeRect.Offset(pos01 * 100, 0);
            switch (Player.PlayerRole)
            {
                case PlayerRole.Prop:
                {
                    var actualRect = UI.Text(rect, "Hide from the Seekers until time runs out!", ts);
                    ts.Size = 64;
                    ts.Color = new Vector4(0, 1, 1, 1);
                    UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are a Hider.\n\n", ts);
                    break;
                }
                case PlayerRole.Hunter:
                {
                    var actualRect = UI.Text(rect, "Kill all the Hiders before time runs out!", ts);
                    ts.Size = 64;
                    ts.Color = new Vector4(1, 0, 0, 1);
                    UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are a Seeker.\n\n", ts);
                    break;
                }
            }

            var emptyButtonSettings = new UI.ButtonSettings(){ ColorMultiplier = Vector4.Zero };
            if (UI.Button(UI.ScreenRect, "CLOSE", emptyButtonSettings, new UI.TextSettings()).Pressed)
            {
                Hold01 += Time.DeltaTime;
            }
            else
            {
                if (Hold01 < 1) Hold01 = 0;
            }

            if (Hold01 >= 1)
            {
                CallServer_ServerSkipIntro();
            }

            Hold01 = (float)Math.Clamp(Hold01, 0, 1);

            ts.Color = new Vector4(1, 1, 1, 1);
            var holdRectBg = UI.SafeRect.BottomCenterRect().Offset(0, 200).Grow(10, 150, 10, 150);
            var holdRect = holdRectBg.Inset(2, 2, 2, 2).SubRect(Hold01, 0, 1, 1);
            UI.Image(holdRectBg, null, new Vector4(0.8f, 0.8f, 0.8f, 1));
            UI.Image(holdRect,   null, new Vector4(0.1f, 0.1f, 0.1f, 1));
            ts.Size = 28;
            var str = "Click and hold to close";
            if (Game.IsMobile)
            {
                str = "Tap and hold to close";
            }
            UI.Text(holdRectBg.Offset(0, 35), str, ts);
        }

        if (ElapsedTime >= totalTime)
        {
            Player.RemoveEffect(this, false);
        }
    }
}