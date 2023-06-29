using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.World;
using XRL.World.Parts.Skill;

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

        private void HighlightHostiles()
        {
            var zone = ParentObject.CurrentCell.ParentZone;
            foreach (var gameObject in zone.GetObjects())
            {
                if (gameObject.IsHostileTowards(ParentObject))
                {
                    gameObject.pRender.SetBackgroundColor('o');
                }
            }
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
                if (gameObject.IsPlayerLed() || gameObject.HasEffect<SummonedEffect>())
                {
                    followersInZone.Add(gameObject);
                }
            }
            //Mod.Log($"There are {followersInZone.Count} followers in the same zone as the player");
            HighlightHostiles();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeRenderEvent evt)
        {
            //Mod.Log($"PlayerPart.BeforeRenderEvent");

            var frame = XRLCore.CurrentFrame % 60;

            foreach (var follower in followersInZone)
            {
                if (follower == null)
                {
                    Mod.Debug($"Follower is null");
                    continue;
                }

                var cell = follower.CurrentCell;

                if (cell == null)
                {
                    Mod.Debug($"Follower {follower.DebugName} has a null CurrentCell");
                    continue;
                }

                if (cell.ParentZone == null)
                {
                    Mod.Debug($"Follower {follower.DebugName} has a null ParentZone");
                    continue;
                }
                cell.ParentZone.AddLight(cell.X, cell.Y, 2, LightLevel.Omniscient);
                cell.ParentZone.AddLight(cell.X, cell.Y, 5);

                if (follower.pRender == null)
                {
                    Mod.Debug($"Follower {follower.DebugName} has a null pRender");
                    continue;
                }
            }

            return base.HandleEvent(evt);
        }
    }
}
