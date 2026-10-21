using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MoreWeaponsRegentExtend.Scripts.Enchantments;

/// <summary>
/// The native card has one enchantment slot. This container retains the native
/// enchantment and all quest enchantments without replacing their mechanics.
/// Child properties are saved with the native SavedProperties serializer.
/// </summary>
[RegisterEnchantment]
public sealed class TemperedEnchantments : ModEnchantmentTemplate
{
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
    private List<EnchantmentModel> _children = [];
    private bool _skipInitialModification;

    public sealed class ChildSave
    {
        public SerializableEnchantment Enchantment { get; set; } = new();
        public EnchantmentStatus Status { get; set; }
    }

    [SavedProperty]
    public string ChildEnchantments
    {
        get => JsonSerializer.Serialize(_children.Select(child => new ChildSave
        {
            Enchantment = child.ToSerializable(),
            Status = child.Status
        }).ToArray(), JsonOptions);
        set
        {
            AssertMutable();
            _children = (JsonSerializer.Deserialize<ChildSave[]>(value, JsonOptions) ?? [])
                .Select(save =>
                {
                    var child = EnchantmentModel.FromSerializable(save.Enchantment);
                    child.Status = save.Status;
                    return child;
                }).ToList();
        }
    }

    public IReadOnlyList<EnchantmentModel> Children
    {
        get
        {
            BindChildren();
            return _children;
        }
    }

    public override bool HasExtraCardText => true;
    public override bool ShowAmount => false;
    public override bool ShouldStartAtBottomOfDrawPile => Children.Any(child => child.ShouldStartAtBottomOfDrawPile);
    public override bool ShouldGlowGold => Children.Any(child => child.ShouldGlowGold);
    public override bool ShouldGlowRed => Children.Any(child => child.ShouldGlowRed);
    public override EnchantmentAssetProfile AssetProfile => new(
        _children.FirstOrDefault()?.IconPath ?? "res://MoreWeaponsRegentExtend/images/powers/LifeLockPower.svg");

    public override bool CanEnchant(CardModel card)
        => base.CanEnchant(card) && WeaponTaskEnchantments.IsPrimaryWeapon(card);

    public void SetAlreadyAppliedChildren(IEnumerable<EnchantmentModel> children)
    {
        AssertMutable();
        _children = children.ToList();
        _skipInitialModification = true;
    }

    public void AddChild(EnchantmentModel child)
    {
        AssertMutable();
        if (!HasCard)
            throw new InvalidOperationException("Bind the combined enchantment before adding a child.");
        child.ApplyInternal(Card, child.Amount);
        _children.Add(child);
        child.ModifyCard();
        Card.FinalizeUpgradeInternal();
        Amount = _children.Count;
        // Preserve child state while emitting the native enchantment change event.
        var card = Card;
        card.ClearEnchantmentInternal();
        card.EnchantInternal(this, Amount);
    }

    private void BindChildren()
    {
        if (!IsMutable || !HasCard)
            return;
        foreach (var child in _children)
            if (!child.HasCard)
                child.ApplyInternal(Card, child.Amount);
    }

    protected override void OnEnchant()
    {
        BindChildren();
        if (_skipInitialModification)
        {
            _skipInitialModification = false;
            return;
        }
        foreach (var child in _children)
            child.ModifyCard();
    }

    protected override void DeepCloneFields()
    {
        // Native cloning also preserves private, unsaved turn/combat fields.
        // A serialization roundtrip here would discard those for old enchants.
        var children = _children;
        base.DeepCloneFields();
        _children = children.Select(child => (EnchantmentModel)child.MutableClone()).ToList();
        _skipInitialModification = false;
    }

    public override void RecalculateValues()
    {
        BindChildren();
        foreach (var child in _children)
            child.RecalculateValues();
    }

    public override async Task OnPlay(PlayerChoiceContext context, CardPlay? play)
    {
        foreach (var child in Children)
        {
            await child.OnPlay(context, play);
            child.InvokeExecutionFinished();
        }
    }

    public override decimal EnchantDamageAdditive(decimal damage, ValueProp props)
        => Children.Sum(child => child.EnchantDamageAdditive(damage, props));

    public override decimal EnchantDamageMultiplicative(decimal damage, ValueProp props)
        => Children.Aggregate(1m, (product, child) => product * child.EnchantDamageMultiplicative(damage, props));

    public override decimal EnchantBlockAdditive(decimal block)
        => Children.Sum(child => child.EnchantBlockAdditive(block));

    public override decimal EnchantBlockMultiplicative(decimal block)
        => Children.Aggregate(1m, (product, child) => product * child.EnchantBlockMultiplicative(block));

    public override int EnchantPlayCount(int originalPlayCount)
        => Children.Aggregate(originalPlayCount, (count, child) => child.EnchantPlayCount(count));
}
