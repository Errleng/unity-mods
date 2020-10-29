using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnterTheGungeonMod
{
    class Utilities
    {
        private PlayerController player;

        public bool healthBars;
        public bool autoBlank;

        public Utilities(PlayerController player)
        {
            this.player = player;
        }

        public void ToggleHealthBars()
        {
            ToggleHealthBars(!healthBars);
        }

        public void ToggleHealthBars(bool on)
        {
            if (on)
            {
                healthBars = true;
                player.OnAnyEnemyReceivedDamage =
                    (Action<float, bool, HealthHaver>)
                    Delegate.Combine(player.OnAnyEnemyReceivedDamage,
                    new Action<float, bool, HealthHaver>(this.HealthbarEvent));
            }
            else
            {
                healthBars = false;
                player.OnAnyEnemyReceivedDamage =
                    (Action<float, bool, HealthHaver>)
                    Delegate.Remove(player.OnAnyEnemyReceivedDamage,
                    new Action<float, bool, HealthHaver>(this.HealthbarEvent));
            }
        }
        public void ToggleAutoBlank()
        {
            ToggleAutoBlank(!autoBlank);
        }

        public void ToggleAutoBlank(bool on)
        {
            if (on)
            {
                autoBlank = true;
                player.healthHaver.ModifyDamage =
                    (Action<HealthHaver, HealthHaver.ModifyDamageEventArgs>)
                    Delegate.Combine(player.healthHaver.ModifyDamage,
                    new Action<HealthHaver, HealthHaver.ModifyDamageEventArgs>(this.AutoBlankEvent));
            }
            else
            {
                autoBlank = false;
                player.healthHaver.ModifyDamage =
                    (Action<HealthHaver, HealthHaver.ModifyDamageEventArgs>)
                    Delegate.Remove(player.healthHaver.ModifyDamage,
                    new Action<HealthHaver, HealthHaver.ModifyDamageEventArgs>(this.AutoBlankEvent));
            }
        }

        private void HealthbarEvent(float damageAmount, bool fatal, HealthHaver target)
        {
            int num = Mathf.RoundToInt(damageAmount);
            Vector3 worldPosition = target.transform.position;
            float heightOffGround = 1f;
            SpeculativeRigidbody component = target.GetComponent<SpeculativeRigidbody>();
            if (component)
            {
                worldPosition = component.UnitCenter.ToVector3ZisY(0f);
                heightOffGround = worldPosition.y - component.UnitBottomCenter.y;
                if (component.healthHaver && !component.healthHaver.HasHealthBar && !component.healthHaver.HasRatchetHealthBar && !component.healthHaver.IsBoss)
                {
                    GameObject VFXHealthBarPrefab = PickupObjectDatabase.GetByName("scouter").GetComponent<RatchetScouterItem>().VFXHealthBar;
                    component.healthHaver.HasRatchetHealthBar = true;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(VFXHealthBarPrefab);
                    SimpleHealthBarController component2 = gameObject.GetComponent<SimpleHealthBarController>();
                    component2.Initialize(component, component.healthHaver);
                }
            }
            else
            {
                AIActor component3 = target.GetComponent<AIActor>();
                if (component3)
                {
                    worldPosition = component3.CenterPosition.ToVector3ZisY(0f);
                    if (component3.sprite)
                    {
                        heightOffGround = worldPosition.y - component3.sprite.WorldBottomCenter.y;
                    }
                }
            }
            num = Mathf.Max(num, 1);
            GameUIRoot.Instance.DoDamageNumber(worldPosition, heightOffGround, num);
        }

        private void AutoBlankEvent(HealthHaver source, HealthHaver.ModifyDamageEventArgs args)
        {
            // stop if there is no damage event, or damage is 0, or player is invulnerable
            if ((args == EventArgs.Empty) || (args.ModifiedDamage <= 0f) || (!source.IsVulnerable))
            {
                return;
            }

            int elderBlankID = 499;
            PlayerItem elderBlank = player.activeItems.Find((PlayerItem a) => a.PickupObjectId == elderBlankID);
            if (elderBlank != null && !elderBlank.IsOnCooldown)
            {
                // use Elder Blank if player has it
                source.TriggerInvulnerabilityPeriod(-1f);
                player.ForceBlank(25f, 0.5f, false, true, null, true, -1f);
                elderBlank.ForceApplyCooldown(player);
                args.ModifiedDamage = 0f;
            }
            else if (player.Blanks > 0 && !player.IsFalling)
            {
                // use a blank automatically
                source.TriggerInvulnerabilityPeriod(-1f);
                player.ForceConsumableBlank();
                args.ModifiedDamage = 0f;
            }
        }
    }
}
