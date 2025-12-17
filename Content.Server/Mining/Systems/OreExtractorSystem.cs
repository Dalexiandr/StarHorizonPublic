using Content.Server.Mining.Components;
using Content.Server.Ore;
using Content.Shared.Audio;
using Content.Server.Power.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.Server.Ore;

public sealed class OreExtractorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityManager.EntityQueryEnumerator<OreExtractorComponent>();
        while (query.MoveNext(out var uid, out var extractor))
        {
            if (!HasPower(uid))
            {
                StopExtractingSound(extractor);
                continue;
            }
                
            if (!IsAnchored(uid))
            {
                StopExtractingSound(extractor);
                continue;
            }

            var deposit = FindDepositUnderExtractor(uid);
            
            bool hasOre = deposit != null && deposit.OreCounts.Values.Sum() > 0;
            
            if (hasOre)
            {
                StartExtractingSound(uid, extractor);
            }
            else
            {
                StopExtractingSound(extractor);
                
                if (deposit != null && deposit.OreCounts.Values.Sum() <= 0)
                {
                    EntityManager.DeleteEntity(deposit.Owner);
                }
            }

            var speedModifier = 1.0f;
            if (deposit != null)
            {
                speedModifier = 1.0f / Math.Max(deposit.Hardness, 0.1f);
            }

            extractor.CurrentTimer -= frameTime * extractor.Efficiency * speedModifier;

            if (extractor.CurrentTimer <= 0f && hasOre)
            {
                TryExtractOre(uid, extractor);
                extractor.CurrentTimer = 3.0f;
            }
        }
    }

    private void StartExtractingSound(EntityUid uid, OreExtractorComponent extractor)
    {
        if (extractor.PlayingSoundEntity != null && EntityManager.EntityExists(extractor.PlayingSoundEntity.Value))
            return;
            
        if (string.IsNullOrEmpty(extractor.ExtractingSound))
            return;

        var audioParams = AudioParams.Default.WithLoop(true);
        var audioEntity = _audio.PlayPvs(
            new SoundPathSpecifier(extractor.ExtractingSound, audioParams), 
            uid
        );
        
        extractor.PlayingSoundEntity = audioEntity?.Entity;
    }

    private void StopExtractingSound(OreExtractorComponent extractor)
    {
        // Останавливаем звук если он играет
        if (extractor.PlayingSoundEntity != null && EntityManager.EntityExists(extractor.PlayingSoundEntity.Value))
        {
            EntityManager.DeleteEntity(extractor.PlayingSoundEntity.Value);
        }
        extractor.PlayingSoundEntity = null;
    }

    private bool HasPower(EntityUid uid)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            return false;
        return true;
    }
    
    private bool IsAnchored(EntityUid uid)
    {
        if (TryComp<TransformComponent>(uid, out var transform))
            return transform.Anchored;
        return false;
    }

    private void TryExtractOre(EntityUid extractorUid, OreExtractorComponent extractor)
    {
        var deposit = FindDepositUnderExtractor(extractorUid);
        if (deposit == null)
            return;

        ExtractOre(extractorUid, deposit.Owner, extractor, deposit);

        if (deposit.OreCounts.Values.Sum() <= 0)
        {
            EntityManager.QueueDeleteEntity(deposit.Owner);
        }
    }

    private OreDepositComponent? FindDepositUnderExtractor(EntityUid extractorUid)
    {
        if (!EntityManager.TryGetComponent(extractorUid, out TransformComponent? extractorXform))
            return null;

        var gridUid = extractorXform.GridUid;
        if (gridUid == null) return null;

        var extractorCoords = extractorXform.Coordinates;
        var searchRadius = 0.5f;

        var deposits = EntityManager.EntityQuery<OreDepositComponent, TransformComponent>();
        foreach (var (deposit, depositXform) in deposits)
        {
            if (depositXform.GridUid != gridUid) continue;

            if (depositXform.Coordinates == extractorCoords)
            {
                return deposit;
            }
        }

        return null;
    }

    private void ExtractOre(EntityUid extractorUid, EntityUid depositUid, 
        OreExtractorComponent extractor, OreDepositComponent deposit)
    {
        var oreCounts = new Dictionary<string, int>();
        float totalWeight = 0f;

        foreach (var kvp in deposit.OreCounts)
        {
            totalWeight += kvp.Value;
        }

        if (totalWeight <= 0f) return;

        var luck = extractor.Luck;
        var random = new Random();
        
        foreach (var kvp in deposit.OreCounts)
        {
            var oreType = kvp.Key;
            var weight = kvp.Value;
            
            if (random.NextSingle() < (weight / totalWeight) * luck)
            {
                var amount = 1;
                oreCounts[oreType] = oreCounts.GetValueOrDefault(oreType, 0) + amount;
            }
        }

        foreach (var kvp in oreCounts)
        {
            var oreType = kvp.Key;
            var amount = kvp.Value;
            SpawnAndMergeOre(extractorUid, oreType, amount);
        }

        foreach (var kvp in deposit.OreCounts.ToList())
        {
            var oreType = kvp.Key;
            var weight = kvp.Value;
            
            if (oreCounts.ContainsKey(oreType))
            {
                var amount = oreCounts[oreType];
                deposit.OreCounts[oreType] = Math.Max(0, weight - amount);
            }
        }
    }

    private void SpawnAndMergeOre(EntityUid extractorUid, string oreType, int amount)
    {
        if (!EntityManager.TryGetComponent(extractorUid, out TransformComponent? extractorXform))
            return;

        var random = new Random();
        var offset = new Vector2(
            (float)(random.NextDouble() - 0.5) * 0.3f,
            (float)(random.NextDouble() - 0.5) * 0.3f
        );
        
        var spawnPosition = extractorXform.Coordinates.Offset(offset);
        
        for (int i = 0; i < amount; i++)
        {
            var oreEntity = EntityManager.SpawnEntity($"{oreType}", spawnPosition);
            
            _stack.TryMergeToContacts(oreEntity);
        }
    }
}
