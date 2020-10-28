using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EnterTheGungeonMod
{
    class GUI
    {
        private GameObject VFXHealthBarPrefab;

        public GUI()
        {
            VFXHealthBarPrefab = PickupObjectDatabase.GetByEncounterName("scouter").GetComponent<RatchetScouterItem>().VFXHealthBar;
        }

        public void ToggleHealthBars(PlayerController player, bool on)
        {
            if (on)
            {
                player.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Combine(player.OnAnyEnemyReceivedDamage, new Action<float, bool, HealthHaver>(this.AnyDamageDealt));
            }
            else
            {
                player.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Remove(player.OnAnyEnemyReceivedDamage, new Action<float, bool, HealthHaver>(this.AnyDamageDealt));
            }
        }

        private void AnyDamageDealt(float damageAmount, bool fatal, HealthHaver target)
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
    }
}
