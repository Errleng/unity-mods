using System;
using XRL.Rules;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_ArmyOfQud : Summoning_GenericSummon
    {
        public override int cooldown => 1000;

        public override int turnCost => 1000;

        public override int numSummons => 4;

        public override string name => "Army of Qud";

        public override string command => "CommandArmyOfQud";

        public override string icon => "A";

        public override string description => $"{base.description}\nYou mass-conjure {numSummons} friendly creatures that follow you. They will be at or below your character level.";

        public override string sound => "rotmg";

        public override int GetTargetLevel()
        {
            int level = Stat.Random(ParentObject.Level / 2, ParentObject.Level);
            return Math.Max(1, level);
        }
    }
}
