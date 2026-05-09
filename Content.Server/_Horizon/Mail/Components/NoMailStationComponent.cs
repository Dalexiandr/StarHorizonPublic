namespace Content.Server._Horizon.Mail.Components;

/// <summary>
/// When added to a station entity, players spawning on this station
/// will automatically receive MailDisabledComponent, preventing them from receiving mail.
/// </summary>
[RegisterComponent]
public sealed partial class NoMailStationComponent : Component
{
}
