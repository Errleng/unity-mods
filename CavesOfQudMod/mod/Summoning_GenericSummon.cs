using CavesOfQudMod;
using Qud.API;
using System;
using System.Linq;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal abstract class Summoning_GenericSummon : BaseSkill
    {
        public Guid activatedAbilityId;

        public abstract int cooldown { get; }
        public abstract int turnCost { get; }
        public abstract string name { get; }
        public abstract string command { get; }
        public abstract string icon { get; }

        public virtual int numSummons => 1;
        public virtual string description => $"Cooldown: {cooldown}.";
        public virtual string sound => "summon";

        public abstract int GetTargetLevel();

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, command);
            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == command)
            {
                if (!ParentObject.CanMoveExtremities(null, true, false, false))
                {
                    return false;
                }
                if (!Summon())
                {
                    return false;
                }
            }
            return base.FireEvent(E);
        }

        public override bool AddSkill(GameObject GO)
        {
            activatedAbilityId = AddMyActivatedAbility(
                name,
                command,
                "Skill",
                description,
                icon,
                null,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                false,
                cooldown,
                null
                );
            Mod.Log($"Added ability {name}, {command}, {activatedAbilityId}");
            return base.AddSkill(GO);
        }

        public override bool RemoveSkill(GameObject GO)
        {
            RemoveMyActivatedAbility(ref activatedAbilityId);
            return base.RemoveSkill(GO);
        }

        public virtual void AfterSummon(GameObject summoned) { }

        public virtual bool Summon()
        {
            // add extra to be safe
            int area = numSummons + 2;
            // PI*r^2 = a => r = sqrt(a/PI)
            int radius = (int)Math.Ceiling(Math.Sqrt(area / Math.PI));
            var cells = ParentObject.pPhysics.CurrentCell.GetEmptyConnectedAdjacentCells(radius);
            int successfulSpawns = 0;
            foreach (var cell in cells)
            {
                if (successfulSpawns == numSummons)
                {
                    break;
                }
                if (cell == null)
                {
                    Mod.Debug($"Could not find a valid cell");
                    continue;
                }
                if (!cell.IsEmpty())
                {
                    Mod.Debug($"Cell {cell.DebugName} already contains objects: {string.Join(",", cell.Objects.Select(x => x.DebugName))}");
                    continue;
                }
                if (cell.HasObjectWithTag("ExcavatoryTerrainFeature"))
                {
                    Mod.Debug($"Cell {cell.DebugName} is excavatory terrain");
                    continue;
                }

                var creature = EncountersAPI.GetNonLegendaryCreatureAroundLevel(GetTargetLevel());
                creature.pBrain.Hostile = false;
                if (!creature.ApplyEffect(new SummonedEffect(ParentObject)))
                {
                    Mod.Debug($"Could not apply SummonedEffect to {creature.DebugName}");
                    continue;
                }
                AfterSummon(creature);
                cell.AddObject(creature);
                successfulSpawns++;
            }
            Mod.Debug($"Summoned {successfulSpawns}/{numSummons} creatures in {cells.Count} cells");

            CooldownMyActivatedAbility(activatedAbilityId, cooldown);
            ParentObject.UseEnergy(turnCost, $"Skill {name}");
            PlayWorldSound(sound, 0.5f, 0f, true, null);

            return true;
        }
    }
}
