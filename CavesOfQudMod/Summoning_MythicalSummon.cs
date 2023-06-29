using System;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_MythicalSummon : Summoning_GenericSummon
    {
        public override int cooldown => 400;

        public override int turnCost => 1000;

        public override string name => "Mythical Summon";

        public override string command => "CommandMythicalSummon";

        public override string icon => "S";

        public override string description => $"{base.description}\nYou conjure a friendly creature that follows you. It will be around your character level + 10.";

        public override int GetTargetLevel()
        {
            return Math.Max(1, ParentObject.Level + 10);
        }
    }
}
