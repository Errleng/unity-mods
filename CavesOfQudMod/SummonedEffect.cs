using CavesOfQudMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRL.World;
using XRL.World.Parts;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class SummonedEffect : Effect
    {
        public GameObject master;

        public SummonedEffect()
        {
            DisplayName = "{{G|summoned}}";
            Duration = 1;
        }

        public SummonedEffect(GameObject summoner) : this()
        {
            master = summoner;
        }

        public override string GetDetails()
        {
            return $"Summoned by {master.DisplayName}.";
        }

        public override string GetDescription()
        {
            return "{{G|summoned}}";
        }

        public override int GetEffectType()
        {
            return 9;
        }

        public override bool Apply(GameObject Object)
        {
            if (!GameObject.validate(ref master))
            {
                return false;
            }
            if (Object.pBrain == null)
            {
                return false;
            }

            XDidYToZ(master, "summon", Object, null, "!", null, master);
            Object.Heartspray();

            Object.RemovePart<GivesRep>();
            Object.pBrain.AdjustFeeling(master, 100);
            Object.pBrain.BecomeCompanionOf(master);

            var party = master.pBrain.PartyMembers;
            if (!party.ContainsKey(Object.id))
            {
                master.pBrain.PartyMembers[Object.id] = 2;
            }
            else
            {
                Mod.Log($"Party already contains summoned thing with ID: {Object.id}");
            }

            return base.Apply(Object);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == EndTurnEvent.ID;
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            if (Object.HasPart(typeof(LeaveTrailWhileHasEffect)))
            {
                Mod.Log($"Killing follower with trail effect");
                Object.Die();
            }
            return base.HandleEvent(E);
        }
    }
}
