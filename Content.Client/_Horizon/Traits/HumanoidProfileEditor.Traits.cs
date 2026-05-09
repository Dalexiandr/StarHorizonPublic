using System.Linq;
using Content.Client._Horizon.Traits;
using Content.Client.Message;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private string _selectedQuirkCategory = "HorizonPositive";

    private RichTextLabel? _quirksPointsLabel;
    private List<TraitPrototype> _cachedQuirks = new();
    private List<string> _quirksCategories = new()
    {
        "HorizonPositive",
        "HorizonNeutral",
        "HorizonNegative"
    };

    private void StartupQuirks()
    {
        TraitsTab.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-quirks-tab"));
        TraitsTab.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-minor-traits-tab"));

        ButtonGroup categoryGroup = new(false);

        QuirkCategories.RemoveAllChildren();

        for (var i = 0; i < _quirksCategories.Count; i++)
        {
            var styleClasses = "ButtonSquare";
            if (i == _quirksCategories.Count - 1)
                styleClasses = "OpenLeft";
            else if (i == 0)
                styleClasses = "OpenRight";

            var category = _quirksCategories[i];

            var button = new Button()
            {
                Text = Loc.GetString(_prototypeManager.Index<TraitCategoryPrototype>(category).Name),
                HorizontalExpand = true,
                StyleClasses = { styleClasses },
                Margin = new(4),
                Pressed = i == 0,
                Group = categoryGroup
            };

            button.OnPressed += _ =>
            {
                _selectedQuirkCategory = category;
                RefreshQuirks();
            };

            QuirkCategories.AddChild(button);
        }

        _quirksPointsLabel = new()
        {
            Margin = new(4, 10, 4, 4),
            VerticalAlignment = VAlignment.Top
        };
        QuirkCategories.AddChild(_quirksPointsLabel);
    }

    private void RefreshQuirks()
    {
        QuirksList.Children.Clear();

        if (Profile is null)
            return;

        var count = 0;
        foreach (var trait in Profile.TraitPreferences)
        {
            // If trait not found or another category don't count its points.
            if (!_prototypeManager.TryIndex<TraitPrototype>(trait, out var otherProto) ||
                !_quirksCategories.Contains(otherProto.Category ?? ""))
            {
                continue;
            }

            count += otherProto.Cost;
        }

        _quirksPointsLabel?.SetMarkup(Loc.GetString("humanoid-profile-editor-quirks-points-label", ("points", -count)));

        var quirks = _prototypeManager
            .EnumeratePrototypes<TraitPrototype>()
            .Where(q => _quirksCategories.Contains(q.Category ?? ""))
            .OrderBy(q => MathF.Abs(q.Cost))
            .ThenBy(q => Loc.GetString(q.Name))
            .ToList();

        if (_cachedQuirks.Equals(quirks))
        {
            foreach (var item in QuirksList.Children)
            {
                if (item is not QuirkEntry entry)
                    continue;

                var quirk = _prototypeManager.Index<TraitPrototype>(entry.ProtoId);

                bool hasTrait = Profile.TraitPreferences.Contains(quirk.ID);
                bool canApply = count + (hasTrait ? -quirk.Cost : quirk.Cost) <= 0;

                entry.UpdateEntry(hasTrait, canApply);
            }
        }
        else
        {
            _cachedQuirks = quirks;

            foreach (var quirk in quirks)
            {
                if (!quirk.RequirmentsMet(Profile, _entManager))
                {
                    Profile = Profile.WithoutTraitPreference(quirk.ID, _prototypeManager);

                    SetDirty();
                    UpdateSaveButton();
                    continue;
                }

                var cost = -quirk.Cost;
                var coloration = cost switch
                {
                    > 0 => QuirkColoration.Negative,
                    < 0 => QuirkColoration.Positive,
                    _ => QuirkColoration.Neutral
                };

                if (quirk.Category != _selectedQuirkCategory)
                    continue;

                bool hasTrait = Profile.TraitPreferences.Contains(quirk.ID);

                var quirkButton = new QuirkEntry(quirk.ID, quirk.Name, quirk.Description ?? "", cost, coloration, hasTrait, CanApplyQuirk(quirk, count))
                {
                    Margin = new Thickness(0, 2)
                };
                quirkButton.OnTraitToggled += isSelected =>
                {
                    Profile = isSelected ? Profile.WithTraitPreference(quirk.ID, _prototypeManager) : Profile.WithoutTraitPreference(quirk.ID, _prototypeManager);

                    SetDirty();
                    RefreshQuirks();
                    UpdateSaveButton();
                };
                QuirksList.AddChild(quirkButton);
            }
        }
    }

    private string? CanApplyQuirk(TraitPrototype trait, int points)
    {
        if (Profile == null)
            return null;

        string? reason = null;

        bool canApply = points + (Profile.TraitPreferences.Contains(trait.ID) ? -trait.Cost : trait.Cost) <= 0;

        if (!canApply)
        {
            reason = Profile.TraitPreferences.Contains(trait.ID) ? Loc.GetString("humanoid-profile-editor-quirks-cannot-remove") :
                                                                   Loc.GetString("humanoid-profile-editor-quirks-cannot-add");
        }

        if (trait.Group != null && !Profile.TraitPreferences.Contains(trait.ID))
        {
            foreach (var item in Profile.TraitPreferences)
            {
                var proto = _prototypeManager.Index(item);
                if (proto.Group == null)
                    continue;

                if (proto.Group == trait.Group)
                    reason = Loc.GetString($"humanoid-profile-editor-quirks-cannot-add-group-{proto.Group}");
            }
        }

        return reason;
    }

    public enum QuirkColoration
    {
        Positive,
        Negative,
        Neutral
    }
}
