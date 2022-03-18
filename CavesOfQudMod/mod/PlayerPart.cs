using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRL.World;
using XRL.World.Parts;

namespace CavesOfQudMod
{
    [Serializable]
    internal class PlayerPart : IPart
    {
        private readonly List<GameObject> followersInZone;

        public PlayerPart()
        {
            followersInZone = new List<GameObject>();
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == EndTurnEvent.ID || ID == BeforeRenderEvent.ID;
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            //Mod.Log($"PlayerPart.EndTurnEvent");

            var zone = ParentObject.CurrentCell.ParentZone;
            var brain = ParentObject.pBrain;
            var party = brain.PartyMembers;

            followersInZone.Clear();
            foreach (var gameObject in zone.GetObjects())
            {
                if (party.ContainsKey(gameObject.id))
                {
                    followersInZone.Add(gameObject);
                }
            }
            //Mod.Log($"There are {followersInZone.Count} followers in the same zone as the player");
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeRenderEvent evt)
        {
            //Mod.Log($"PlayerPart.BeforeRenderEvent");

            foreach (var follower in followersInZone)
            {
                var cell = follower?.CurrentCell;
                // same radius as a torch
                cell?.ParentZone?.AddLight(cell.X, cell.Y, 1, LightLevel.Omniscient);
                cell?.ParentZone?.AddLight(cell.X, cell.Y, 5);

                follower?.pRender?.SetForegroundColor("C");
            }

            return base.HandleEvent(evt);
        }
    }
}
