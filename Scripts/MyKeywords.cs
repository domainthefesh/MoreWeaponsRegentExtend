using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace MoreWeaponsRegentExtend.Scripts;

[RegisterOwnedCardKeyword(nameof(Shatter),
    IconPath = "",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Apocalypse),
    IconPath = "",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Crystalline),
    IconPath = "",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Stance))]
[RegisterOwnedCardKeyword(nameof(HeatDeath), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(HotStart), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Blessing))]
[RegisterOwnedCardKeyword(nameof(MartialFanatic), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public class MyKeywords
{
    public static readonly CardKeyword Stance = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Stance)).GetModCardKeyword();
    public static readonly CardKeyword HeatDeath = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(HeatDeath)).GetModCardKeyword();
    public static readonly CardKeyword HotStart = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(HotStart)).GetModCardKeyword();
    public static readonly CardKeyword Blessing = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Blessing)).GetModCardKeyword();
    public static readonly CardKeyword MartialFanatic = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(MartialFanatic)).GetModCardKeyword();
    public static readonly CardKeyword Shatter =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Shatter)).GetModCardKeyword();

    public static readonly CardKeyword Apocalypse =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Apocalypse)).GetModCardKeyword();

    public static readonly CardKeyword Crystalline =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Crystalline)).GetModCardKeyword();
}
