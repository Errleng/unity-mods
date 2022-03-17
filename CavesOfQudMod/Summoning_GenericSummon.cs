using CavesOfQudMod;
using Qud.API;
using System;
using XRL.World;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal abstract class Summoning_GenericSummon : BaseSkill
    {
        protected Guid activatedAbilityId = Guid.Empty;

        public abstract int cooldown { get; }
        public abstract int turnCost { get; }
        public abstract string name { get; }
        public abstract string command { get; }
        public abstract string icon { get; }
        public virtual string description => $"Cooldown: {cooldown}.";

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

        public virtual bool Summon()
        {
            var cell = PickDirection();
            if (cell == null)
            {
                return false;
            }
            if (!cell.IsEmpty() || cell.HasObjectWithTag("ExcavatoryTerrainFeature"))
            {
                return false;
            }

            var creature = EncountersAPI.GetNonLegendaryCreatureAroundLevel(GetTargetLevel());
            creature.pBrain.Hostile = false;
            creature.ApplyEffect(new SummonedEffect(ParentObject));
            cell.AddObject(creature);

            CooldownMyActivatedAbility(activatedAbilityId, cooldown + 1);
            ParentObject.UseEnergy(turnCost, $"Skill {name}");
            Mod.Log($"Activated ability: {activatedAbilityId}");
            return true;
        }
    }
}
