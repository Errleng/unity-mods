using System;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_GreaterSummon : Summoning_GenericSummon
    {
        public override int cooldown => 200;

        public override int turnCost => 1000;

        public override string name => "Greater Summon";

        public override string command => "CommandGreaterSummon";

        public override string icon => "S";

        public override string description => $"{base.description}\nYou conjure a friendly creature that follows you. It will be around your character level.";

        public override int GetTargetLevel()
        {
            return Math.Max(1, ParentObject.Level);
        }
    }
}
