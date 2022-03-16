using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRL;
using XRL.Core;

namespace CavesOfQudMod
{
    [HasCallAfterGameLoadedAttribute]
    internal class LoadGameHandler
    {
        [CallAfterGameLoaded]
        public static void OnLoadGame()
        {
            Mod.Log("OnLoadGame");
            var player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                player.RequirePart<PlayerPart>();
                Mod.Log($"Added part to player: {player.GetPart<PlayerPart>()}");
            }
            else
            {
                Mod.Log("Player is null when loading game!");
            }
        }
    }
}
