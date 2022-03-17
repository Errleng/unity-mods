using Qud.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRL.Rules;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_ArmyOfQud : Summoning_GenericSummon
    {
        public override int cooldown => 1000;

        public override int turnCost => 1000;

        public override string name => "Army of Qud";

        public override string command => "CommandArmyOfQud";

        public override string icon => "A";

        public override string description => $"{base.description}\nYou mass-conjure several friendly creatures that follow you. They will be at or below your character level.";

        public override int GetTargetLevel()
        {
            int level = Stat.Random(ParentObject.Level / 2, ParentObject.Level);
            return Math.Max(1, level);
        }

        public override bool Summon()
        {
            for (int i = 0; i < 4; i++)
            {
                var cell = ParentObject.pPhysics.PickDestinationCell(8, AllowVis.OnlyVisible, false, false, false, true, UI.PickTarget.PickStyle.EmptyCell, null, false);
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
            }

            ParentObject.UseEnergy(turnCost, $"Skill {name}");
            CooldownMyActivatedAbility(activatedAbilityId, cooldown + 1);
            return true;
        }
    }
}
