using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MoreWeaponsRegentExtend.Scripts.Cards;
using MoreWeaponsRegentExtend.Scripts.Progression;

namespace MoreWeaponsRegentExtend.Scripts;

// A slot belongs to one generated cannon. Cloning/replaying a Charge keeps its slot,
// while cloning the cannon creates a fresh set of three slots.
public sealed class CannonChargeLink
{
    private sealed class NumberSequence { public int LastNumber; }
    private static readonly ConditionalWeakTable<PlayerCombatState, NumberSequence> Numbers = new();
    public const int RequiredCharges = 3;

    private readonly bool[] _completed = new bool[RequiredCharges];

    public GalaxyTrajectoryCannon Cannon { get; }
    public int CompletedCharges { get; private set; }
    public int RequiredCount => WeaponRewardRuntime.Has(Cannon, 2) ? 1 : RequiredCharges;
    public bool IsHot => CompletedCharges >= RequiredCount;
    public bool ChargesCreated { get; set; }
    private int _number;
    public int Number
    {
        get
        {
            if (_number == 0 && !Cannon.IsDupe && Cannon.Pile?.IsCombatPile == true
                && Cannon.Owner?.PlayerCombatState is { } combat)
                _number = ++Numbers.GetValue(combat, static _ => new NumberSequence()).LastNumber;
            return _number;
        }
    }

    public CannonChargeLink(GalaxyTrajectoryCannon cannon) => Cannon = cannon;

    public bool CompleteSlot(int slot)
    {
        if (slot < 0 || slot >= RequiredCharges || _completed[slot])
            return false;
        _completed[slot] = true;
        CompletedCharges++;
        Cannon.RefreshThermalState();
        return true;
    }
}
