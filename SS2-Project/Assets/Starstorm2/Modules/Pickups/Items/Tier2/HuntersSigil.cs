﻿using Moonstorm.Starstorm2.Components;
using R2API;
using RoR2;
using RoR2.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace Moonstorm.Starstorm2.Items
{
    public sealed class HuntersSigil : ItemBase
    {
        private const string token = "SS2_ITEM_HUNTERSSIGIL_DESC";
        public override ItemDef ItemDef { get; } = SS2Assets.LoadAsset<ItemDef>("HuntersSigil", SS2Bundle.Items);
        public static GameObject effect;

        public static GameObject SigilObject { get; set; } = SS2Assets.LoadAsset<GameObject>("SigilWard", SS2Bundle.Items);

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Base amount of extra armor added.")]
        [TokenModifier(token, StatTypes.Default, 0)]
        public static float baseArmor = 20;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Amount of extra armor added per stack.")]
        [TokenModifier(token, StatTypes.Default, 1)]
        public static float stackArmor = 10;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Base amount of extra damage added. (1 = 100%)")]
        [TokenModifier(token, StatTypes.Percentage, 2)]
        public static float baseDamage = .2f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Amount of extra damage added per stack. (1 = 100%)")]
        [TokenModifier(token, StatTypes.Percentage, 3)]
        public static float stackDamage = .10f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Radius the effect is applied in.")]
        [TokenModifier(token, StatTypes.Default, 0)]
        public static float radius = 8f;

        /*[RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Base time the buff lingers for after moving, in seconds.")]
        [TokenModifier(token, StatTypes.Default, 4)]
        public static float baseLinger = 2f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Amount of extra lingering time added per stack, in seconds.")]
        [TokenModifier(token, StatTypes.Default, 5)]
        public static float stackLinger = 1f;*/

        public override void Initialize()
        {
            base.Initialize();
            effect = SS2Assets.LoadAsset<GameObject>("SigilEffect", SS2Bundle.Items);
        }

        public sealed class Behavior : BaseItemBodyBehavior//, IBodyStatArgModifier
        {
            [ItemDefAssociation]
            private static ItemDef GetItemDef() => SS2Content.Items.HuntersSigil;
            private bool sigilActive = false;
            private GameObject sigilInstance;

            public void FixedUpdate()
            {
                if (!NetworkServer.active) return;
                if (body.notMovingStopwatch > 1f)
                {
                    if (!sigilActive)
                    {
                        EffectManager.SimpleEffect(effect, body.aimOrigin + new Vector3(0, 0f), Quaternion.identity, true);
                        Vector3 position = body.corePosition;
                        //float radius = 13f;

                        if (sigilInstance != null)
                            NetworkServer.Destroy(sigilInstance);

                        sigilInstance = Object.Instantiate(SigilObject, position, Quaternion.identity);
                        sigilInstance.GetComponent<TeamFilter>().teamIndex = body.teamComponent.teamIndex;
                        sigilInstance.GetComponent<BuffWard>().radius = radius;
                        WardUtils wu = sigilInstance.GetComponent<WardUtils>();
                        wu.body = body;
                        wu.radius = radius;
                        wu.buffCount = stack;
                        wu.amplifiedBuff = SS2Content.Buffs.BuffSigilHidden;

                        NetworkServer.Spawn(sigilInstance);

                        sigilActive = true;
                    }
                    //body.AddTimedBuff(SS2Content.Buffs.BuffSigil, baseLinger + stackLinger * (stack - 1f));
                }
                else
                    sigilActive = false;
            }
            /*public void ModifyStatArguments(RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body.HasBuff(SS2Content.Buffs.BuffSigil))
                {
                    //the base amounts are added by the buff itself in case the buff is gained from another source such as Aetherium's Accursed Potion
                    args.armorAdd += stackArmor * (stack - 1);
                    args.damageMultAdd += (stackDamage * (stack - 1));
                }
            }*/
            public void OnDestroy()
            {
                if (sigilInstance != null)
                    Destroy(sigilInstance);
            }

            public void OnDisable()
            {
                if (sigilInstance != null)
                    Destroy(sigilInstance);
            }
        }
    }
}
