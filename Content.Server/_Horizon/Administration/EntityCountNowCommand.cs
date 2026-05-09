using System;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Server._Horizon.Administration;

[AdminCommand(AdminFlags.Admin)]
public sealed class EntityCountNowCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private static readonly ISawmill _sawmill = Logger.GetSawmill("entityCountNow");

    public string Command => "entityCountNow";
    public string Description => "Counts entities for all existing tags or searches tags by partial match. Outputs to console and log.";
    public string Help => "Usage: entityCountNow [searchTerm] - if searchTerm provided, shows only tags containing it";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Словарь для подсчета количества сущностей по каждому тегу
        var tagCounts = new Dictionary<string, int>();
        var query = _entityManager.EntityQueryEnumerator<TagComponent>();

        while (query.MoveNext(out var uid, out var tagComponent))
        {
            foreach (var tag in tagComponent.Tags)
            {
                var tagString = tag.ToString();
                tagCounts.TryGetValue(tagString, out var currentCount);
                tagCounts[tagString] = currentCount + 1;
            }
        }

        // Определяем, есть ли параметр поиска
        var searchTerm = args.Length > 0 ? args[0] : null;

        // Фильтруем теги: сначала по количеству > 0, затем по поисковому запросу (если есть)
        var filteredTags = tagCounts
            .Where(kvp => kvp.Value > 0)
            .Where(kvp => searchTerm == null || kvp.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .ToList();

        string message;
        if (filteredTags.Count == 0)
        {
            if (searchTerm != null)
            {
                message = $"Не найдено тегов, содержащих '{searchTerm}'.";
            }
            else
            {
                message = "Не найдено сущностей с тегами.";
            }
        }
        else
        {
            var messageBuilder = new System.Text.StringBuilder();
            if (searchTerm != null)
            {
                messageBuilder.AppendLine($"Найдено тегов, содержащих '{searchTerm}':");
            }
            else
            {
                messageBuilder.AppendLine("Количество сущностей по тегам:");
            }

            foreach (var (tag, count) in filteredTags)
            {
                messageBuilder.AppendLine($"  {tag}: {count}");
            }

            message = messageBuilder.ToString().TrimEnd();
        }

        // Отправляем результат в консоль
        shell.WriteLine(message);

        // Отправляем результат в лог
        _sawmill.Info($"{message.Replace(Environment.NewLine, " | ")}");
    }
}
