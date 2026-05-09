using Content.Client.UserInterface.Systems.Chat;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using System.Numerics;

namespace Content.Client._Horizon.UserInterface.Systems.Chat;

/// <summary>
/// Команда основанная на achatwindow.<br/>
/// Также открывает админ чат, но в отдельном окне вне основного окна игры.
/// </summary>
[UsedImplicitly]
public sealed class HorizonAdminChatWindowCommand : LocalizedCommands
{
    public override string Command => "achatwindowpopout";

    public override string Description => "Открывает внешнее окно административного чата.";

    public override string Help => "Использование: achatwindowpopout";

    public override void Execute(IConsoleShell shell, string argsStr, string[] args)
    {
        var chat = new ChatWindow();
        var window = new OSWindow
        {
            Title = "Окно чата",
            SetSize = new Vector2(500, 800),
        };
        var contents = chat.ContentsContainer;

        contents.Parent?.RemoveChild(contents);

        var backgroundPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#25252a") },
            HorizontalExpand = true,
            VerticalExpand = true
        };

        contents.HorizontalExpand = true;
        contents.VerticalExpand = true;

        backgroundPanel.AddChild(contents);
        window.AddChild(backgroundPanel);
        chat.ConfigureForAdminChat();
        window.Show();
    }
}
