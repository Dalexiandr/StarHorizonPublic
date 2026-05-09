using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Horizon.Markers;

public sealed class SpawnInRangeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private const int MaxSpawnAttempts = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnInRangeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SpawnInRangeComponent component, MapInitEvent args)
    {
        var spawned = new List<EntityCoordinates>();
        var fails = 0;

        foreach (var (protoId, minMax) in component.Spawns)
        {
            var toSpawn = _random.Next(minMax.Min, minMax.Max);

            for (var i = 0; i < toSpawn; i++)
            {
                var x = _random.Next(component.MinRange, component.MaxRange);
                var y = _random.Next(component.MinRange, component.MaxRange);
                if (_random.Prob(0.5f))
                    x = -y;
                if (_random.Prob(0.5f))
                    y = -y;

                var spawnPos = new EntityCoordinates(uid, x, y);

                // ensure we aren't too close to other spawns
                var success = true;
                foreach (var other in spawned)
                {
                    if (other.TryDistance(EntityManager, spawnPos, out var distance) && distance < component.MinDistanceBetweenSpawns)
                    {
                        if (fails < MaxSpawnAttempts)
                        {
                            i--;
                            fails++;

                            success = false;
                            break;
                        }
                        else
                        {
                            // прекращаем спавнить, если слишком много неудачных попыток
                            return;
                        }
                    }
                }

                if (!success)
                    continue;

                Spawn(protoId, spawnPos);
                spawned.Add(spawnPos);
            }
        }
    }
}
