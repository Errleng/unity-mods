using CavesOfQudMod;
using Qud.API;
using System;

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
            if (!creature.ApplyEffect(new SummonedEffect(ParentObject)))
            {
                return false;
            }
            cell.AddObject(creature);

            CooldownMyActivatedAbility(activatedAbilityId, cooldown);
            ParentObject.UseEnergy(turnCost, $"Skill {name}");
            PlayWorldSound(sound, 0.5f, 0f, true, null);

            Mod.Log($"Activated ability {name} ({activatedAbilityId})");
            //if (activatedAbilityId == Guid.Empty)
            //{
            //    var ability = ActivatedAbilities.ThePlayer.ActivatedAbilities.GetAbilityByCommand(command);
            //    Mod.Log($"Re-adding ability {ability.DisplayName} ({ability.ID})");
            //    ActivatedAbilities.ThePlayer.RemoveActivatedAbility(ref ability.ID);
            //    AddSkill(ParentObject);
            //}

            return true;
        }
    }
}
