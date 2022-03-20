using System;
using XRL.Rules;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_TheWorldOpposes : Summoning_GenericSummon
    {
        private readonly int SUMMON_DURATION = 100;

        public override int cooldown => 3000;

        public override int turnCost => 1000;

        public override int numSummons => 30;

        public override string name => "The World Opposes";

        public override string command => "CommandTheWorldOpposes";

        public override string icon => "W";

        public override string description => $"{base.description}\nYou mass-conjure {numSummons} temporary friendly creatures that follow you. They will be at or below your character level.";

        public override string sound => "monster-house";

        public override int GetTargetLevel()
        {
            int level = Stat.Random(Math.Max(ParentObject.Level / 4, 2), ParentObject.Level);
            return Math.Max(1, level);
        }

        public override void AfterSummon(GameObject summoned)
        {
            Temporary.AddHierarchically(summoned, SUMMON_DURATION);
            base.AfterSummon(summoned);
        }
    }
}
