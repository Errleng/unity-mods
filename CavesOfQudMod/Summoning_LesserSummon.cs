using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_LesserSummon : Summoning_GenericSummon
    {
        public readonly int levelDecrease = 10;

        public override int cooldown => 200;

        public override int turnCost => 1000;

        public override string name => "Lesser Summon";

        public override string command => "CommandLesserSummon";

        public override string icon => "S";

        public override string description => $"{base.description}\nYou conjure a friendly creature that follows you. It will be around your character level - {levelDecrease}.";

        public override int GetTargetLevel()
        {
            return Math.Max(1, ParentObject.Level - levelDecrease);
        }
    }
}
