using MegaCrit.Sts2.Core.Localization;

namespace MoreWeaponsRegentExtend.Scripts.Progression.UI;

/// <summary>Language-specific UI copy. Native localization entries take precedence.</summary>
public static class ProgressionUiText
{
    public const string Prefix = "MORE_WEAPONS_PROGRESSION.";
    private static readonly Dictionary<WeaponKind, (string Zhs, string Eng)> WeaponNames = new()
    {
        [WeaponKind.SovereignBludgeon] = ("君王重锤", "Sovereign Warhammer"),
        [WeaponKind.ApocalypseLongbow] = ("邪皇圣弓", "Dread Emperor's Sacred Bow"),
        [WeaponKind.SovereignCrystalBlade] = ("君王晶刃", "Sovereign Crystal Blade"),
        [WeaponKind.SovereignShield] = ("君王之盾", "Sovereign Shield"),
        [WeaponKind.SovereignSpear] = ("君王长矛", "Sovereign Spear"),
        [WeaponKind.SovereignAxe] = ("君王重斧", "Sovereign Greataxe"),
        [WeaponKind.SovereignGun] = ("皇室猎枪", "Royal Rifle"),
        [WeaponKind.SovereignScythe] = ("君王镰刀", "Sovereign Scythe"),
        [WeaponKind.NeptuneTrident] = ("海王三叉戟", "Neptune's Trident"),
        [WeaponKind.SovereignKatana] = ("天皇太刀", "Imperial Katana"),
        [WeaponKind.SovereignWings] = ("君王之翼", "Sovereign Wings"),
        [WeaponKind.SovereignDagger] = ("君王短匕", "Sovereign Dagger"),
        [WeaponKind.SeaCalmingStaff] = ("定海圣棍", "Sea Calming Staff"),
        [WeaponKind.GalaxyTrajectoryCannon] = ("银河轨迹炮", "Galactic Trajectory Cannon"),
        [WeaponKind.HolyCodex] = ("光之圣典", "Codex of Light"),
        [WeaponKind.RoyalBrassKnuckles] = ("皇家指虎", "Royal Knuckles"),
        [WeaponKind.SovereignBlade] = ("君王之剑", "Sovereign Blade")
    };
    public static readonly IReadOnlyDictionary<string, (string Zhs, string Eng)> Entries =
        new Dictionary<string, (string, string)>
        {
            ["TITLE"] = ("兵器淬炼", "Weapon Tempering"),
            ["SUBTITLE"] = ("击败首领，领取本局兵器的专属强化。", "Defeat bosses and claim weapon upgrades for this run."),
            ["WEAPONS"] = ("兵器", "Armory"),
            ["MAINLINE"] = ("淬炼之路", "The Tempering Path"),
            ["BOSS_1"] = ("击败第一幕首领", "Defeat the Act I boss"),
            ["BOSS_2"] = ("击败第二幕首领", "Defeat the Act II boss"),
            ["TASK"] = ("任务", "Task"),
            ["REWARD"] = ("奖励", "Reward"),
            ["TASK_3"] = ("赢得5场精英战斗", "Win 5 elite combats"),
            ["TASK_PROGRESS3"] = ("{0} / 5", "{0} / 5"),
            ["TASK_PROGRESS"] = ("{0} / {1}", "{0} / {1}"),
            ["TASK_PROGRESS_ALT8"] = ("事件：{0} / {1}\n佛珠手链：{2} / {3}", "Events: {0} / {1}\nJuzu Bracelet: {2} / {3}"),
            ["TASK_PROGRESS_ALT9"] = ("问号战斗：{0} / {1}\n全部战斗：{2} / {3}", "Unknown-room combats: {0} / {1}\nAll combats: {2} / {3}"),
            ["REWARD_SIDE"] = ("基础攻击伤害+4。", "Base attack damage +4."),
            ["TASK_4"] = ("[gold]升级[/gold]卡牌累计5次", "[gold]Upgrade[/gold] cards 5 times"),
            ["TASK_5"] = ("通过第一幕。\n与[gold]轻盈[/gold]互斥，每把武器二选一。", "Complete Act I.\nChoose Heavy or [gold]Light[/gold] for each weapon."),
            ["TASK_6"] = ("通过第一幕。\n与[gold]沉重[/gold]互斥，每把武器二选一。", "Complete Act I.\nChoose Light or [gold]Heavy[/gold] for each weapon."),
            ["TASK_7"] = ("在4回合内赢得精英战斗，累计3场", "Win 3 elite combats in 4 turns or fewer each"),
            ["TASK_8"] = ("持有[gold]佛珠手链[/gold]，或完成5个事件", "Have [gold]Juzu Bracelet[/gold] or complete 5 events"),
            ["TASK_9"] = ("赢得3场问号节点战斗，或累计赢得18场战斗", "Win 3 unknown-room combats or 18 combats in total"),
            ["REWARD_SIDE4"] = ("首次[gold]铸造[/gold]生成的君王武器会[gold]升级[/gold]。", "[gold]Upgrade[/gold] Sovereign weapons when first created by [gold]Forge[/gold]."),
            ["REWARD_SIDE5"] = ("基础费用+1，最终伤害+50%。", "Base Energy cost +1. Final damage +50%."),
            ["REWARD_SIDE5_GUN"] = ("基础费用+1。[gold]王之枪[/gold]射击的最终伤害+50%。", "Base Energy cost +1. [gold]King's Gun[/gold] final shot damage +50%."),
            ["REWARD_SIDE5_WINGS"] = ("基础费用+1。[gold]居高临下[/gold]的增伤额外+25个百分点。", "Base Energy cost +1. [gold]High Ground[/gold]'s damage bonus gains 25 percentage points."),
            ["REWARD_SIDE6"] = ("基础费用−1，最低为0；最终伤害−30%。", "Base Energy cost −1, minimum 0. Final damage −30%."),
            ["REWARD_SIDE6_GUN"] = ("基础费用−1，最低为0。[gold]王之枪[/gold]射击的最终伤害−30%。", "Base Energy cost −1, minimum 0. [gold]King's Gun[/gold] final shot damage −30%."),
            ["REWARD_SIDE6_WINGS"] = ("基础费用−1，最低为0。[gold]居高临下[/gold]的增伤降低30%，减伤不变。", "Base Energy cost −1, minimum 0. [gold]High Ground[/gold]'s damage bonus is reduced by 30%; its damage reduction is unchanged."),
            ["REWARD_SIDE6_DAGGER"] = ("基础费用−1，最低为0；最终伤害−30%。额外打出一次。", "Base Energy cost −1, minimum 0. Final damage −30%. Play this card one additional time."),
            ["REWARD_SIDE7"] = ("获得[gold]击剑指南[/gold]。\n首次[gold]铸造[/gold]生成时，获得[gold]完美契合[/gold]附魔。", "Obtain [gold]Fencing Manual[/gold].\nGain the [gold]Perfect Fit[/gold] Enchantment when first created by [gold]Forge[/gold]."),
            ["REWARD_SIDE8"] = ("首次[gold]铸造[/gold]生成时，获得[gold]强运[/gold]附魔：伤害−10%～+50%，每回合变化。", "Gain the [gold]Strong Fortune[/gold] Enchantment when first created by [gold]Forge[/gold]: damage varies from −10% to +50% each turn."),
            ["REWARD_SIDE9"] = ("首次[gold]铸造[/gold]生成时，获得[gold]怨气[/gold]附魔：伤害+100%；打出后获得[gold]诅咒[/gold]，本场战斗受到的伤害翻倍，不叠加。", "Gain the [gold]Resentment[/gold] Enchantment when first created by [gold]Forge[/gold]: damage +100%. After playing it, gain [gold]Curse[/gold]: take double damage this combat. Does not stack."),
            ["CLAIM"] = ("领取强化", "Claim Upgrade"),
            ["CLAIMED"] = ("已领取", "Claimed"),
            ["LOCKED"] = ("未解锁", "Locked"),
            ["EXCLUSIVELOCKED"] = ("已选择另一项特化", "The other specialization was chosen"),
            ["INCOMPLETE"] = ("未完成", "Incomplete"),
            ["CLAIMABLE"] = ("待领取", "Ready to Claim"),
            ["SELECTED_ONLY"] = ("本局选择的兵器可领取强化；其他兵器可查看。", "Claim upgrades for your chosen weapon. Other weapons can be previewed."),
            ["RANDOM_ALL"] = ("百武皆通：每件兵器均可分别领取强化。", "Random forging: claim upgrades for each weapon separately."),
            ["CURRENT_WEAPON"] = ("本局兵器", "Chosen Weapon"),
            ["UNSELECTED"] = ("本局未选用此兵器", "This weapon is not chosen for this run"),
            ["STEP_1"] = ("淬炼一", "Tempering I"),
            ["STEP_2"] = ("淬炼二", "Tempering II"),
            ["STEP_3"] = ("淬玉", "Jade Tempering"),
            ["STEP_4"] = ("淬火", "Quenching"),
            ["STEP_5"] = ("特化：沉重", "Specialization: Heavy"),
            ["STEP_6"] = ("特化：轻盈", "Specialization: Light"),
            ["STEP_7"] = ("断水", "Watercut"),
            ["STEP_8"] = ("理气", "Inner Balance"),
            ["STEP_9"] = ("怨魂", "Restless Spirit"),
            ["HINT"] = ("强化领取后生效，仅持续本局。", "Claimed upgrades last for this run."),
            ["NEEDS_FIRST"] = ("先领取淬炼一", "Claim Tempering I first"),
            ["CLOSE"] = ("关闭", "Close"),
            ["ENTRY"] = ("兵器淬炼：查看任务与领取强化", "Weapon Tempering: view tasks and claim upgrades"),
            ["EMPTY"] = ("尚未选择兵器", "No weapon chosen yet"),
            ["BATTLE_CLAIM_BLOCKED"] = ("战斗结束后可领取强化", "Claim upgrades after combat ends"),
            ["REWARD.SovereignBludgeon.1"] = ("打出时，额外给予目标1层[gold]缩小[/gold]。", "Apply 1 additional [gold]Shrink[/gold] to the target when played."),
            ["REWARD.SovereignBludgeon.2"] = ("基础伤害+10。", "Base damage +10."),
            ["REWARD.ApocalypseLongbow.1"] = ("基础[gold]天启[/gold]+1。", "Base [gold]Apocalypse[/gold] +1."),
            ["REWARD.ApocalypseLongbow.2"] = ("打出时给予目标1层[gold]命锁[/gold]：获得的负面效果额外增加2层。", "Apply 1 [gold]Fate Lock[/gold] to the target when played: debuffs received gain 2 extra stacks."),
            ["REWARD.SovereignCrystalBlade.1"] = ("[gold]碎裂[/gold]+1。", "[gold]Shatter[/gold] +1."),
            ["REWARD.SovereignCrystalBlade.2"] = ("额外造成一次伤害。", "Deal damage one additional time."),
            ["REWARD.SovereignShield.1"] = ("打出后不再获得[gold]晶态[/gold]。", "No longer gain [gold]Crystalline[/gold] when played."),
            ["REWARD.SovereignShield.2"] = ("每场战斗首次打出时，[gold]格挡[/gold]翻倍。", "Double [gold]Block[/gold] on this card's first play each combat."),
            ["REWARD.SovereignSpear.1"] = ("额外[gold]铸造[/gold]+2。", "Additional [gold]Forge[/gold] +2."),
            ["REWARD.SovereignSpear.2"] = ("目标有攻击意图时，伤害前给予其1层[gold]易伤[/gold]。", "Apply 1 [gold]Vulnerable[/gold] before damage if the target intends to attack."),
            ["REWARD.SovereignAxe.1"] = ("[gold]流血[/gold]额外造成2%最大生命值的伤害。", "[gold]Bleed[/gold] deals an additional 2% of maximum HP."),
            ["REWARD.SovereignAxe.2"] = ("打出时给予目标2层[gold]撕裂[/gold]：每次攻击损失10%最大生命值，并失去1层。", "Apply 2 [gold]Rend[/gold] to the target when played: each attack costs 10% of maximum HP and removes 1 stack."),
            ["REWARD.SovereignGun.1"] = ("基础费用−1。", "Base Energy cost −1."),
            ["REWARD.SovereignGun.2"] = ("[gold]王之枪[/gold][gold]消耗[/gold][gold]子弹[/gold]时，额外造成一次伤害。", "[gold]King's Gun[/gold] deals damage again when it [gold]Exhausts[/gold] a [gold]Bullet[/gold]."),
            ["REWARD.SovereignScythe.1"] = ("施加的[gold]灾厄[/gold]+50%。", "Apply 50% more [gold]Doom[/gold]."),
            ["REWARD.SovereignScythe.2"] = ("斩杀时，最大生命值+4。", "Gain 4 maximum HP on a kill."),
            ["REWARD.NeptuneTrident.1"] = ("每场战斗首次打出时，获得10层[gold]雷霆[/gold]。", "Gain 10 [gold]Thunder[/gold] on this card's first play each combat."),
            ["REWARD.NeptuneTrident.2"] = ("伤害前给予目标1层[gold]海啸[/gold]：当前及之后获得的[gold]格挡[/gold]减半。", "Apply 1 [gold]Tsunami[/gold] before damage: halve current [gold]Block[/gold] and future [gold]Block[/gold] gained."),
            ["REWARD.SovereignKatana.1"] = ("每场战斗首次打出时，伤害+50%。", "Damage +50% on this card's first play each combat."),
            ["REWARD.SovereignKatana.2"] = ("[gold]处决[/gold]门槛提升至50%生命值。", "Raise the [gold]execution[/gold] threshold to 50% HP."),
            ["REWARD.SovereignWings.1"] = ("打出时，额外获得1层[gold]居高临下[/gold]。", "Gain 1 additional [gold]High Ground[/gold] when played."),
            ["REWARD.SovereignWings.2"] = ("[gold]居高临下[/gold]的减伤与增伤均提升至75%。", "[gold]High Ground[/gold] reduces damage by 75% and increases damage by 75%."),
            ["REWARD.SovereignDagger.1"] = ("斩杀获得的金币×3。", "Gold gained on a kill ×3."),
            ["REWARD.SovereignDagger.2"] = ("目标无攻击意图或金币超过300时，伤害×3。", "Damage ×3 if the target does not intend to attack or you have more than 300 Gold."),
            ["REWARD.SeaCalmingStaff.1"] = ("每场战斗首次打出后，返回手牌。", "Return to your hand after this card's first play each combat."),
            ["REWARD.SeaCalmingStaff.2"] = ("每切换3次[gold]架势[/gold]，获得3点[gold]能量[/gold]。", "Gain 3 [gold]Energy[/gold] for every 3 [gold]stance[/gold] changes."),
            ["REWARD.GalaxyTrajectoryCannon.1"] = ("基础费用−1。", "Base Energy cost −1."),
            ["REWARD.GalaxyTrajectoryCannon.2"] = ("所需[gold]充能[/gold]次数−2。", "Require 2 fewer [gold]Charges[/gold]."),
            ["REWARD.HolyCodex.1"] = ("基础[gold]赐福[/gold]+1。", "Base [gold]Blessing[/gold] +1."),
            ["REWARD.HolyCodex.2"] = ("每场战斗首次打出时，恢复6点生命值。", "Heal 6 HP on this card's first play each combat."),
            ["REWARD.RoyalBrassKnuckles.1"] = ("突破[gold]格挡[/gold]时的[gold]铸造[/gold]+2。", "[gold]Forge[/gold] +2 when damage breaks through [gold]Block[/gold]."),
            ["REWARD.RoyalBrassKnuckles.2"] = ("[gold]适应[/gold]层数上限+2。", "Maximum [gold]Adaptation[/gold] stacks +2."),
            ["REWARD.SovereignBlade.1"] = ("基础费用−1。", "Base Energy cost −1."),
            ["REWARD.SovereignBlade.2"] = ("斩杀时，本局所有君王之剑的基础伤害+10，可累计。", "On a kill, all Sovereign Blades gain +10 base damage this run. Stacks.")
        };

    public static bool English => LocManager.Instance?.Language == "eng";

    /// <summary>Translate the native keyword color tags for Godot's rich text controls.</summary>
    public static string ToBbCode(string text) => text
        .Replace("[gold]", "[color=#e7c44e]", StringComparison.Ordinal)
        .Replace("[/gold]", "[/color]", StringComparison.Ordinal);

    /// <summary>Use plain text for tooltips, buttons and other non-rich controls.</summary>
    public static string StripMarkup(string text) => text
        .Replace("[gold]", string.Empty, StringComparison.Ordinal)
        .Replace("[/gold]", string.Empty, StringComparison.Ordinal);

    public static string RewardText(WeaponKind weapon, int node)
    {
        if (node <= 2) return Get($"REWARD.{weapon}.{node}");
        var suffix = node == 3 ? "REWARD_SIDE" : "REWARD_SIDE" + node;
        if (node is 5 or 6)
            suffix += weapon == WeaponKind.SovereignGun ? "_GUN"
                : weapon == WeaponKind.SovereignWings ? "_WINGS"
                : node == 6 && weapon == WeaponKind.SovereignDagger ? "_DAGGER" : string.Empty;
        return Get(suffix);
    }

    public static string Get(string suffix)
    {
        var key = Prefix + suffix;
        if (LocManager.Instance != null && LocString.Exists("gameplay_ui", key))
            return new LocString("gameplay_ui", key).GetRawText();
        return Entries.TryGetValue(suffix, out var text) ? (English ? text.Eng : text.Zhs) : suffix;
    }

    public static string WeaponName(WeaponKind weapon)
    {
        var names = weapon.ToString();
        var key = string.Concat(names.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString())).ToUpperInvariant();
        var id = weapon == WeaponKind.SovereignBlade ? "SOVEREIGN_BLADE" : "MORE_WEAPONS_REGENT_EXTEND_CARD_" + key;
        if (LocManager.Instance != null && LocString.Exists("cards", id + ".title"))
            return new LocString("cards", id + ".title").GetRawText();
        return WeaponNames.TryGetValue(weapon, out var text) ? (English ? text.Eng : text.Zhs) : weapon.ToString();
    }
}
