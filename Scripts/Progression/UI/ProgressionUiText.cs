using MegaCrit.Sts2.Core.Localization;

namespace MoreWeaponsRegentExtend.Scripts.Progression.UI;

/// <summary>Plain, bilingual UI copy. Native localization entries take precedence.</summary>
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
            ["REWARD_SIDE"] = ("基础攻击伤害增加4。", "Increase base attack damage by 4."),
            ["CLAIM"] = ("领取强化", "Claim Upgrade"),
            ["CLAIMED"] = ("已领取", "Claimed"),
            ["LOCKED"] = ("未解锁", "Locked"),
            ["INCOMPLETE"] = ("未完成", "Incomplete"),
            ["CLAIMABLE"] = ("待领取", "Ready to Claim"),
            ["SELECTED_ONLY"] = ("本局选择的兵器可领取强化；其他兵器可查看。", "Claim upgrades for your chosen weapon. Other weapons can be previewed."),
            ["RANDOM_ALL"] = ("百武皆通：每件兵器均可分别领取强化。", "Random forging: claim upgrades for each weapon separately."),
            ["CURRENT_WEAPON"] = ("本局兵器", "Chosen Weapon"),
            ["UNSELECTED"] = ("本局未选用此兵器", "This weapon is not chosen for this run"),
            ["STEP_1"] = ("淬炼一", "Tempering I"),
            ["STEP_2"] = ("淬炼二", "Tempering II"),
            ["STEP_3"] = ("淬玉", "Jade Tempering"),
            ["HINT"] = ("强化领取后生效，仅持续本局。", "Claimed upgrades last for this run."),
            ["NEEDS_FIRST"] = ("先领取淬炼一", "Claim Tempering I first"),
            ["CLOSE"] = ("关闭", "Close"),
            ["ENTRY"] = ("兵器淬炼：查看任务与领取强化", "Weapon Tempering: view tasks and claim upgrades"),
            ["EMPTY"] = ("尚未选择兵器", "No weapon chosen yet"),
            ["BATTLE_CLAIM_BLOCKED"] = ("战斗结束后可领取强化", "Claim upgrades after combat ends"),
            ["REWARD.SovereignBludgeon.1"] = ("打出时，额外给予目标1层缩小。", "Apply 1 Shrink to the target when played."),
            ["REWARD.SovereignBludgeon.2"] = ("基础伤害增加10。", "Increase base damage by 10."),
            ["REWARD.ApocalypseLongbow.1"] = ("基础天启增加1。", "Gain 1 additional base Apocalypse."),
            ["REWARD.ApocalypseLongbow.2"] = ("打出时给予目标1层命锁：获得的负面效果层数增加2。", "Apply 1 Fate Lock: debuffs received gain 2 additional stacks."),
            ["REWARD.SovereignCrystalBlade.1"] = ("碎裂增加1。", "Gain 1 additional Shatter."),
            ["REWARD.SovereignCrystalBlade.2"] = ("额外造成一次伤害。", "Deal damage one additional time."),
            ["REWARD.SovereignShield.1"] = ("打出后不再获得晶态。", "No longer gain Crystalline when played."),
            ["REWARD.SovereignShield.2"] = ("每场战斗首次打出时，获得的格挡翻倍。", "Double Block on this card's first play each combat."),
            ["REWARD.SovereignSpear.1"] = ("额外铸造增加2。", "Gain 2 additional Forge."),
            ["REWARD.SovereignSpear.2"] = ("目标意图为攻击时，造成伤害前给予其1层易伤。", "Apply 1 Vulnerable before damage if the target intends to attack."),
            ["REWARD.SovereignAxe.1"] = ("流血增加2%最大生命值。", "Bleed deals an additional 2% of maximum HP."),
            ["REWARD.SovereignAxe.2"] = ("打出时给予目标2层撕裂：攻击时损失10%最大生命值。", "Apply 2 Rend: lose 10% of maximum HP when attacking."),
            ["REWARD.SovereignGun.1"] = ("基础费用减少1。", "Reduce base Energy cost by 1."),
            ["REWARD.SovereignGun.2"] = ("王之枪消耗子弹时，额外造成一次伤害。", "King's Gun deals damage again when it Exhausts a Bullet."),
            ["REWARD.SovereignScythe.1"] = ("施加的灾厄增加50%。", "Apply 50% more Doom."),
            ["REWARD.SovereignScythe.2"] = ("斩杀时，获得4点最大生命值。", "Gain 4 maximum HP on a kill."),
            ["REWARD.NeptuneTrident.1"] = ("每场战斗首次打出时，获得10层雷霆。", "Gain 10 Thunder on this card's first play each combat."),
            ["REWARD.NeptuneTrident.2"] = ("先给予目标1层海啸，使其格挡降低50%，再造成伤害。", "Apply 1 Tsunami, reducing Block by 50%, before dealing damage."),
            ["REWARD.SovereignKatana.1"] = ("每场战斗首次打出时，额外造成50%伤害。", "Deal 50% more damage on this card's first play each combat."),
            ["REWARD.SovereignKatana.2"] = ("处决门槛提升至50%生命值。", "Raise the execution threshold to 50% HP."),
            ["REWARD.SovereignWings.1"] = ("打出时额外获得1层居高临下。", "Gain 1 additional High Ground when played."),
            ["REWARD.SovereignWings.2"] = ("居高临下的减伤与增伤均提升至75%。", "High Ground reduces damage by 75% and increases damage by 75%."),
            ["REWARD.SovereignDagger.1"] = ("斩杀获得的金币翻三倍。", "Triple Gold gained on a kill."),
            ["REWARD.SovereignDagger.2"] = ("目标无攻击意图或金币超过300时，造成三倍伤害。", "Deal triple damage if the target does not intend to attack or you have more than 300 Gold."),
            ["REWARD.SeaCalmingStaff.1"] = ("每场战斗首次打出后，返回手牌。", "Return to your hand after this card's first play each combat."),
            ["REWARD.SeaCalmingStaff.2"] = ("每完成3次架势切换，获得3点能量。", "Gain 3 Energy for every 3 stance changes."),
            ["REWARD.GalaxyTrajectoryCannon.1"] = ("基础费用减少1。", "Reduce base Energy cost by 1."),
            ["REWARD.GalaxyTrajectoryCannon.2"] = ("所需充能次数减少2。", "Require 2 fewer Charges."),
            ["REWARD.HolyCodex.1"] = ("基础赐福增加1。", "Gain 1 additional base Blessing."),
            ["REWARD.HolyCodex.2"] = ("每场战斗首次打出时，恢复6点生命值。", "Heal 6 HP on this card's first play each combat."),
            ["REWARD.RoyalBrassKnuckles.1"] = ("突破格挡时的铸造增加2。", "Gain 2 additional Forge when damage breaks through Block."),
            ["REWARD.RoyalBrassKnuckles.2"] = ("适应最大层数增加2。", "Increase maximum Adaptation stacks by 2."),
            ["REWARD.SovereignBlade.1"] = ("基础费用减少1。", "Reduce base Energy cost by 1."),
            ["REWARD.SovereignBlade.2"] = ("斩杀时，本局君王之剑的基础伤害增加10。", "On a kill, increase Sovereign Blade's base damage by 10 for this run.")
        };

    public static bool English => LocManager.Instance?.Language == "eng";

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
