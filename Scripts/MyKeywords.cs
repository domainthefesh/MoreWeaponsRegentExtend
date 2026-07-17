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
public class MyKeywords
{
    public static readonly CardKeyword Shatter =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Shatter)).GetModCardKeyword();

    public static readonly CardKeyword Apocalypse =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Apocalypse)).GetModCardKeyword();

    public static readonly CardKeyword Crystalline =
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Crystalline)).GetModCardKeyword();
}
