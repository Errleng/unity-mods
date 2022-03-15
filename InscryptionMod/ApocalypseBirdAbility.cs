using DiskCardGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static InscryptionMod.Plugin;

namespace InscryptionMod
{
    internal class ApocalypseBirdAbility : AbilityBehaviour
    {
        public static Ability ability;

        public override Ability Ability => ability;

        public override IEnumerator OnDie(bool wasSacrifice, PlayableCard killer)
        {
            var playerGraveyard = CustomGameManager.encounterGraveyard.Where(card => card.OpponentCard).Select(card => card.Info.defaultEvolutionName).ToList();

            var smallBirdDied = playerGraveyard.Contains(Constants.SMALL_BIRD_ID) || playerGraveyard.Contains(Constants.SMALL_BIRD_2_ID) || playerGraveyard.Contains(Constants.SMALL_BIRD_3_ID);
            if (smallBirdDied &&
                playerGraveyard.Contains(Constants.BIG_BIRD_ID) &&
                playerGraveyard.Contains(Constants.JUDGEMENT_BIRD_ID))
            {
                Plugin.logger.LogDebug($"Summoning Apocalypse Bird");
                yield return PreSuccessfulTriggerSequence();
                yield return new WaitForSeconds(1f);
                yield return Singleton<BoardManager>.Instance.CreateCardInSlot(CardLoader.GetCardByName(Constants.APOCALYPSE_BIRD_NAME), Card.Slot, 0.15f, true);
                yield return LearnAbility(0.5f);
            }
            yield break;
        }

        public override IEnumerator OnResolveOnBoard()
        {
            Plugin.logger.LogDebug("Apocalypse Bird has arrived!");
            return base.OnResolveOnBoard();
        }
    }
}
