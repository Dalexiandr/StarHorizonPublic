using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Cytology.Console.UI;

[UsedImplicitly]
public sealed partial class CytologySampleCombinatorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CytologySampleCombinatorWindow? _window;

    public CytologySampleCombinatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<CytologySampleCombinatorWindow>();

        _window.OnSaveAll += OnSaveAllData;
        _window.OnProfileChanged += OnProfileChanged;
        _window.OnSampleDelete += OnSampleDelete;
    }

    /// <summary>
    /// Handles the deletion of a cell sample
    /// Sends a network message to the server to remove the item
    /// </summary>
    private void OnSampleDelete(int sampleIndex)
    {
        SendMessage(new CytologySampleCombinatorDeleteSampleMessage(sampleIndex));
    }

    /// <summary>
    /// Handles the "Save" button press
    /// Sends both profile changes and disk configuration changes to the server
    /// </summary>
    private void OnSaveAllData(int sampleIndex, HumanoidCharacterProfile? profile, List<string> selectedDisks)
    {
        // Batch updates: Send profile data first, then disk configuration
        SendMessage(new CytologySampleCombinatorUpdateProfileMessage(sampleIndex, profile));
        SendMessage(new CytologySampleCombinatorUpdateDisksMessage(sampleIndex, selectedDisks));
    }

    /// <summary>
    /// Handles live profile updates (e.g. while editing appearance)
    /// Used if we want to sync state without closing/saving the full form
    /// </summary>
    private void OnProfileChanged(int sampleIndex, HumanoidCharacterProfile? profile)
    {
        SendMessage(new CytologySampleCombinatorUpdateProfileMessage(sampleIndex, profile));
    }

    /// <summary>
    /// Called when the server sends new data (BUI State) to the client
    /// Updates the window with the latest sample list and disk availability
    /// </summary>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CytologySampleCombinatorBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }

    /// <summary>
    /// Cleanup when the UI is closed or the component is removed
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
