using System.Linq;
using Content.Client._Horizon.Languages.UI;
using Content.Shared._Horizon.Language;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    public void RefreshLanguages()
    {
        LanguagesList.DisposeAllChildren();
        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-languages-tab"));
        SetDefaultLanguagesButton.OnPressed += _ => SetDefaultLanguages();

        if (Profile == null)
            return;
        var species = _prototypeManager.Index(Profile.Species);

        LanguagesCountLabel.Text = Loc.GetString("humanoid-profile-editor-languages-count",
                                                ("current", Profile.Languages.Count),
                                                ("max", species.MaxLanguages));

        var list = _prototypeManager.EnumeratePrototypes<LanguagePrototype>()
                                    .Where(x => x.Roundstart && !species.DefaultLanguages.Contains(x) && !species.UniqueLanguages.Contains(x))
                                    .OrderBy(x => x.LocalizedName);

        foreach (var item in species.DefaultLanguages)
            AddLanguageEntry(_prototypeManager.Index(item), species);

        foreach (var item in species.UniqueLanguages.Where(x => !species.DefaultLanguages.Contains(x)))
            AddLanguageEntry(_prototypeManager.Index(item), species);

        foreach (var item in list)
            AddLanguageEntry(item, species);
    }

    private void AddLanguageEntry(LanguagePrototype proto, SpeciesPrototype species)
    {
        if (Profile == null)
            return;
        var entry = new LanguageEntry()
        {
            Margin = new(7),
            HorizontalExpand = true
        };
        entry.SelectButton.Text = Loc.GetString(!Profile.Languages.Contains(proto) ? "language-lobby-add-button" : "language-lobby-remove-button");
        entry.SelectButton.ToolTip = null;
        entry.SelectButton.Disabled = Profile.Languages.Count >= species.MaxLanguages && !Profile.Languages.Contains(proto);
        entry.OnLanguageSelected += SelectLanguage;
        LanguagesList.AddChild(entry);

        entry.Populate(proto, false);
    }

    public void SelectLanguage(string protoId)
    {
        Profile = (Profile?.Languages.Contains(protoId) ?? false) ? Profile?.WithoutLanguage(protoId) : Profile?.WithLanguage(protoId);
        SetDirty();
        RefreshLanguages();
    }

    public void SetDefaultLanguages()
    {
        if (Profile == null)
            return;
        var species = _prototypeManager.Index(Profile.Species);
        foreach (var item in Profile.Languages)
        {
            Profile = Profile?.WithoutLanguage(item);
        }
        foreach (var item in species.DefaultLanguages)
        {
            Profile = Profile?.WithLanguage(item);
        }

        SetDirty();
        RefreshLanguages();
    }
}
