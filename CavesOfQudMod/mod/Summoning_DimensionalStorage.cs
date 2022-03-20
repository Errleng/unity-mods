using CavesOfQudMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XRL.World.Parts.Skill
{
    [Serializable]
    internal class Summoning_DimensionalStorage : BaseSkill
    {
        public Guid activatedAbilityId;

        // can't serialize GameObject automatically since Cell is not serializable
        [NonSerialized]
        public List<GameObject> storage = new List<GameObject>();

        private bool isEmpty() => storage.Count == 0;

        public int cooldown => 1;
        public int turnCost => 1000;
        public string name => $"Dimensional Storage";
        public string command => "CommandDimensionalStorage";
        public string icon => "S";
        public string description => $"Cooldown: {cooldown}. Store summoned creatures in another dimension, to be unleashed later to great effect.";

        public override void SaveData(SerializationWriter Writer)
        {
            base.SaveData(Writer);
            Writer.WriteGameObjectList(storage);
        }

        public override void LoadData(SerializationReader Reader)
        {
            base.LoadData(Reader);
            Reader.ReadGameObjectList(storage);
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, command);
            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == command)
            {
                if (isEmpty())
                {
                    Store();
                }
                else
                {
                    Release();
                }
            }
            return base.FireEvent(E);
        }

        public override bool AddSkill(GameObject GO)
        {
            activatedAbilityId = AddMyActivatedAbility(
                name,
                command,
                "Skill",
                description,
                icon,
                null,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                false,
                true,
                false,
                cooldown,
                null
                );
            Mod.Log($"Added ability {name}, {command}, {activatedAbilityId}");
            return base.AddSkill(GO);
        }

        public override bool RemoveSkill(GameObject GO)
        {
            RemoveMyActivatedAbility(ref activatedAbilityId);
            return base.RemoveSkill(GO);
        }

        public bool Store()
        {
            var zone = ParentObject.CurrentZone;
            foreach (var obj in zone.GetObjects())
            {
                if (obj.IsPlayerLed() || obj.HasEffect<SummonedEffect>())
                {
                    obj.CurrentCell.RemoveObject(obj);
                    obj.MakeInactive();
                    storage.Add(obj);
                }
            }

            CooldownMyActivatedAbility(activatedAbilityId, cooldown);
            ParentObject.UseEnergy(turnCost, $"Skill {name}");

            Mod.Debug($"Stored {storage.Count}: {string.Join(",", storage.Select(x => x.DebugName))}");

            return true;
        }

        public bool Release()
        {
            int numSummons = storage.Count;
            // add extra to be safe
            int area = numSummons * 2 + 1;
            // PI*r^2 = a => r = sqrt(a/PI)
            int radius = (int)Math.Ceiling(Math.Sqrt(area / Math.PI));
            var cells = ParentObject.pPhysics.CurrentCell.GetEmptyConnectedAdjacentCells(radius);

            int successfulSpawns = 0;
            foreach (var cell in cells)
            {
                if (successfulSpawns == numSummons)
                {
                    break;
                }
                var summon = storage[0];
                if (cell == null)
                {
                    Mod.Debug($"Could not find a valid cell");
                    continue;
                }
                if (!cell.IsEmpty())
                {
                    Mod.Debug($"Cell {cell.DebugName} already contains objects: {string.Join(",", cell.Objects.Select(x => x.DebugName))}");
                    continue;
                }
                if (cell.HasObjectWithTag("ExcavatoryTerrainFeature"))
                {
                    Mod.Debug($"Cell {cell.DebugName} is excavatory terrain");
                    continue;
                }
                summon.MakeActive();
                cell.AddObject(summon);
                storage.Remove(summon);
                successfulSpawns++;
            }
            Mod.Debug($"Released {successfulSpawns}/{numSummons} creatures in {cells.Count} cells");
            CooldownMyActivatedAbility(activatedAbilityId, cooldown);
            ParentObject.UseEnergy(turnCost, $"Skill {name}");
            return true;
        }
    }
}
