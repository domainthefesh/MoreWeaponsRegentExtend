using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MoreWeaponsRegentExtend.Scripts.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Powers;

[RegisterPower]
public sealed class SovereignShieldPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://MoreWeaponsRegentExtend/images/powers/SovereignShieldPower.png",
        BigIconPath: "res://MoreWeaponsRegentExtend/images/powers/SovereignShieldPower.png"
    );

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? source,
        CardModel? card)
    {
        if (target == base.Owner && result.UnblockedDamage > 0)
        {
            var player = base.Owner.Player;
            if (player != null)
            {
                var shield = player.PlayerCombatState!.AllCards
                    .FirstOrDefault(c => c is SovereignShield && c.Pile!.Type != PileType.Exhaust);

                if (shield != null)
                {
                    await CardCmd.Exhaust(choiceContext, shield);
                }
            }

            await PowerCmd.Remove(this);
            Flash();
        }
    }
}
