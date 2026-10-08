using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace MoreWeaponsRegentExtend.Scripts.Cards;

/// <summary>
/// One-shot, native Godot effects for weapon attacks and casts. Scenes use
/// transparent weapon sprites or small vectors instead of card portraits or Spine rigs.
/// </summary>
public static class NUpdateCardVfx
{
    private const string SceneDirectory = "res://MoreWeaponsRegentExtend/scenes/";

    public static void Play(CardModel card, Creature? target = null) => Spawn(card, target, false);

    // Call only after the dagger actually grants its kill reward. Ordinary
    // dagger attacks never emit coins, so the visual matches the gold change.
    public static void PlayKillReward(CardModel card) => Spawn(card, null, true);

    private static void Spawn(CardModel card, Creature? target, bool goldReward)
    {
        if (TestMode.IsOn)
            return;

        var room = NCombatRoom.Instance;
        if (room == null || !GodotObject.IsInstanceValid(room) || !room.IsInsideTree()
            || DisplayServer.GetName() == "headless")
            return;

        string name = card.GetType().Name;
        if (name is not ("HandyWeapon" or "NeptuneTrident" or "SovereignKatana"
            or "SovereignWings" or "SovereignDagger" or "SovereignBludgeon"
            or "ApocalypseLongbow" or "SovereignCrystalBlade" or "SovereignShield"
            or "SovereignSpear" or "SovereignAxe" or "SovereignScythe"
            or "SovereignGun" or "CrystalDagger" or "Bullet" or "SeaCalmingStaff"
            or "GalaxyTrajectoryCannon" or "HolyCodex" or "RoyalBrassKnuckles" or "CannonCharge"))
            return;
        if (goldReward && name != "SovereignDagger")
            return;

        // Shield and gun casts belong to their owner even when the shield's
        // damage command supplies an enemy. Attack effects use the given target.
        var creature = name is "SovereignShield" or "SovereignGun" or "GalaxyTrajectoryCannon"
            ? card.Owner?.Creature : target ?? card.Owner?.Creature;
        var creatureNode = creature == null ? null : room.GetCreatureNode(creature);
        var container = room.CombatVfxContainer;
        if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode)
            || container == null || !GodotObject.IsInstanceValid(container))
            return;

        Node2D? root = null;
        try
        {
            // The old scenes beside the update scenes are persistent Spine
            // weapons. Keep one-shot effects in a separate namespace.
            string directory = name is "HandyWeapon" or "NeptuneTrident" or "SovereignKatana"
                or "SovereignWings" or "SovereignDagger" ? SceneDirectory : SceneDirectory + "cast/";
            string path = directory + name + ".tscn";
            if (!ResourceLoader.Exists(path))
                return;

            var scene = ResourceLoader.Load<PackedScene>(path);
            if (scene == null)
                return;

            root = scene.Instantiate<Node2D>();
            var effectRoot = root;
            Vector2 position = creatureNode.VfxSpawnPosition;
            // Capture the played stance before the card advances to its next stance.
            int stance = card is SeaCalmingStaff staff ? (int)staff.Stance : 0;
            var beamTarget = target == null ? null : room.GetCreatureNode(target);
            Vector2 beamEnd = beamTarget != null ? beamTarget.VfxSpawnPosition - position : new Vector2(380f, 0f);
            effectRoot.Ready += () =>
            {
                effectRoot.GlobalPosition = position;
                foreach (string beamName in new[] { "Beam", "BeamCore" })
                {
                    var beam = effectRoot.GetNodeOrNull<Line2D>(beamName);
                    if (beam != null)
                        beam.Points = new[] { new Vector2(75f, 0f), beamEnd };
                }
                Animate(effectRoot, name, goldReward, stance);
            };
            container.AddChildSafely(effectRoot);
        }
        catch (Exception exception)
        {
            if (root != null && GodotObject.IsInstanceValid(root))
                root.QueueFree();
            GD.PushWarning($"MoreWeaponsRegentExtend: {name} VFX could not play: {exception.Message}");
        }
    }

    private static void Animate(Node2D root, string name, bool goldReward, int stance = 0)
    {
        if (goldReward)
        {
            root.GetNode<Node2D>("Icon").Hide();
            root.GetNode<Node2D>("Slash").Hide();
            root.GetNode<Node2D>("Halo").Hide();
            Emit(root, "Coins");
        }
        else
        {
            Emit(root, "Sparks");
            var icon = root.GetNode<Node2D>("Icon");
            var motion = root.CreateTween().SetParallel();

            switch (name)
            {
                case "HandyWeapon":
                    var lid = root.GetNode<Node2D>("Icon/LidPivot");
                    lid.Rotation = 0.15f;
                    motion.TweenProperty(lid, "rotation", -0.72f, 0.20)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position:y", -18f, 0.25)
                        .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                    break;
                case "NeptuneTrident":
                    icon.Position = new Vector2(-28f, -36f);
                    motion.TweenProperty(icon, "position", new Vector2(12f, 12f), 0.16)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    PulseLightning(root);
                    break;
                case "SovereignKatana":
                    icon.Rotation = -0.65f;
                    motion.TweenProperty(icon, "rotation", 0.75f, 0.18)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(root.GetNode<Node2D>("Slash"), "scale", new Vector2(1.35f, 1.1f), 0.18);
                    break;
                case "SovereignWings":
                    var left = root.GetNode<Node2D>("Icon/LeftWing");
                    var right = root.GetNode<Node2D>("Icon/RightWing");
                    left.Rotation = 0.50f;
                    right.Rotation = -0.50f;
                    motion.TweenProperty(left, "rotation", -0.12f, 0.32)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(right, "rotation", 0.12f, 0.32)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position:y", -28f, 0.42);
                    break;
                case "SovereignDagger":
                    icon.Position = new Vector2(-55f, 24f);
                    motion.TweenProperty(icon, "position", new Vector2(25f, -18f), 0.13)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    break;
                case "SovereignBludgeon":
                    icon.Position = new Vector2(-12f, -54f);
                    icon.Rotation = -0.45f;
                    motion.TweenProperty(icon, "position:y", 16f, 0.19)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    motion.TweenProperty(icon, "rotation", 0.30f, 0.19);
                    ExpandAccent(root, motion, new Vector2(1.1f, 0.68f), 0.24);
                    break;
                case "ApocalypseLongbow":
                    icon.Position = new Vector2(-35f, 0f);
                    motion.TweenProperty(icon, "position:x", 20f, 0.16)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    ExpandAccent(root, motion, new Vector2(1.45f, 0.65f), 0.16);
                    break;
                case "SovereignCrystalBlade":
                    icon.Rotation = -0.85f;
                    motion.TweenProperty(icon, "rotation", 0.55f, 0.18)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    ExpandAccent(root, motion, new Vector2(1.2f, 1.0f), 0.20);
                    break;
                case "SovereignShield":
                    Vector2 shieldScale = icon.Scale;
                    icon.Scale *= 0.78f;
                    icon.Position = new Vector2(0f, 14f);
                    motion.TweenProperty(icon, "scale", shieldScale, 0.23)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position:y", -8f, 0.23);
                    ExpandAccent(root, motion, new Vector2(1.0f, 1.0f), 0.28);
                    break;
                case "SovereignSpear":
                    icon.Position = new Vector2(-65f, 32f);
                    motion.TweenProperty(icon, "position", new Vector2(25f, -16f), 0.14)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    ExpandAccent(root, motion, new Vector2(1.25f, 0.65f), 0.16);
                    break;
                case "SovereignAxe":
                    icon.Rotation = -0.95f;
                    motion.TweenProperty(icon, "rotation", 0.80f, 0.22)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    ExpandAccent(root, motion, new Vector2(1.05f, 1.05f), 0.25);
                    break;
                case "SovereignScythe":
                    icon.Rotation = -0.70f;
                    motion.TweenProperty(icon, "rotation", 0.85f, 0.23)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    ExpandAccent(root, motion, new Vector2(1.35f, 1.35f), 0.23);
                    break;
                case "SovereignGun":
                    Vector2 gunScale = icon.Scale;
                    icon.Scale *= 0.85f;
                    icon.Position = new Vector2(-12f, -10f);
                    motion.TweenProperty(icon, "scale", gunScale, 0.28)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position:x", 0f, 0.28);
                    ExpandAccent(root, motion, new Vector2(1.05f, 0.75f), 0.32);
                    break;
                case "CrystalDagger":
                    icon.Position = new Vector2(-65f, 20f);
                    motion.TweenProperty(icon, "position", new Vector2(18f, -22f), 0.12)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    ExpandAccent(root, motion, new Vector2(0.85f, 0.42f), 0.15);
                    break;
                case "Bullet":
                    icon.Position = new Vector2(-95f, 0f);
                    motion.TweenProperty(icon, "position:x", 8f, 0.10)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    ExpandAccent(root, motion, new Vector2(1.5f, 0.52f), 0.13);
                    break;
                case "SeaCalmingStaff":
                    icon.Rotation = stance == 2 ? -1.15f : -0.55f;
                    icon.Position = new Vector2(-20f, stance == 2 ? -62f : 0f);
                    motion.TweenProperty(icon, "rotation", stance == 2 ? 0.60f : 0.65f, stance == 2 ? 0.24 : 0.18)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position", new Vector2(20f, stance == 2 ? 18f : 0f), 0.24);
                    ExpandAccent(root, motion, stance == 1 ? new Vector2(1.8f, 0.75f)
                        : stance == 2 ? new Vector2(1.15f, 1.25f) : new Vector2(1.0f, 0.62f), 0.24);
                    break;
                case "GalaxyTrajectoryCannon":
                    icon.Position = new Vector2(0f, -8f);
                    motion.TweenProperty(icon, "position:x", -26f, 0.10)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
                    motion.Chain().TweenProperty(icon, "position:x", 0f, 0.22);
                    foreach (string beamName in new[] { "Beam", "BeamCore" })
                    {
                        var beam = root.GetNodeOrNull<Line2D>(beamName);
                        if (beam != null)
                            motion.TweenProperty(beam, "width", beam.Width * 0.25f, 0.34);
                    }
                    ExpandAccent(root, motion, new Vector2(1.3f, 0.70f), 0.26);
                    break;
                case "HolyCodex":
                    Vector2 bookScale = icon.Scale;
                    icon.Scale *= 0.75f;
                    icon.Position = new Vector2(0f, 12f);
                    motion.TweenProperty(icon, "scale", bookScale, 0.25)
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    motion.TweenProperty(icon, "position:y", -20f, 0.30);
                    ExpandAccent(root, motion, new Vector2(1.15f, 1.15f), 0.30);
                    break;
                case "RoyalBrassKnuckles":
                    icon.Position = new Vector2(-58f, 12f);
                    motion.TweenProperty(icon, "position", new Vector2(20f, -8f), 0.14)
                        .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
                    ExpandAccent(root, motion, new Vector2(1.1f, 0.72f), 0.18);
                    break;
                case "CannonCharge":
                    icon.Position = new Vector2(0f, 20f);
                    motion.TweenProperty(icon, "position:y", -18f, 0.30)
                        .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                    ExpandAccent(root, motion, new Vector2(0.9f, 0.9f), 0.25);
                    break;
            }

            var halo = root.GetNodeOrNull<Node2D>("Halo");
            if (halo != null)
                motion.TweenProperty(halo, "scale", halo.Scale * 1.6f, 0.45)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }

        // Tweens are bound to the scene root, so combat teardown cancels them.
        var lifetime = root.CreateTween();
        lifetime.TweenInterval(goldReward ? 0.55 : name == "Bullet" ? 0.18 : 0.38);
        lifetime.TweenProperty(root, "modulate:a", 0f, 0.50);
        lifetime.TweenCallback(Callable.From(root.QueueFree));
    }

    private static void ExpandAccent(Node2D root, Tween motion, Vector2 scale, double duration)
    {
        var accent = root.GetNodeOrNull<Node2D>("Slash");
        if (accent != null)
            motion.TweenProperty(accent, "scale", scale, duration)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    private static void Emit(Node root, string name)
    {
        var particles = root.GetNodeOrNull<CpuParticles2D>(name);
        if (particles == null)
            return;
        particles.Restart();
        particles.Emitting = true;
    }

    private static void PulseLightning(Node2D root)
    {
        var bolt = root.GetNodeOrNull<Node2D>("Lightning");
        if (bolt == null)
            return;
        var pulse = root.CreateTween();
        pulse.TweenProperty(bolt, "modulate:a", 0.18f, 0.07);
        pulse.TweenProperty(bolt, "modulate:a", 1f, 0.04);
        pulse.TweenProperty(bolt, "modulate:a", 0.10f, 0.09);
        pulse.TweenProperty(bolt, "modulate:a", 0.85f, 0.04);
        pulse.TweenProperty(bolt, "modulate:a", 0f, 0.18);
    }
}
