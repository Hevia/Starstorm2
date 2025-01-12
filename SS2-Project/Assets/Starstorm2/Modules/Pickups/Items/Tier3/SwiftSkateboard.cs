﻿using R2API;
using RoR2;
using RoR2.Items;
using UnityEngine;

namespace Moonstorm.Starstorm2.Items
{

    public sealed class SwiftSkateboard : ItemBase
    {
        public const string token = "SS2_ITEM_SKATEBOARD_DESC";

        public override ItemDef ItemDef { get; } = SS2Assets.LoadAsset<ItemDef>("SwiftSkateboard", SS2Bundle.Items);

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Movement speed bonus for each skateboard push. (1 = 100%)")]
        [TokenModifier(token, StatTypes.MultiplyByN, 0, "100")]
        public static float moveSpeedBonus = 0.2f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Movement speed bonus for each skateboard push for each item stack past the first. (1 = 100%)")]
        [TokenModifier(token, StatTypes.MultiplyByN, 1, "100")]
        public static float moveSpeedBonusPerStack = 0.15f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Initial maximum stacks for the Swift Skateboard's movement speed buff.")]
        [TokenModifier(token, StatTypes.Default, 2)]
        public static int maxStacks = 4;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Additional maximum stacks for each item stack of Swift Skateboard past the first.")]
        [TokenModifier(token, StatTypes.Default, 3)]
        public static int maxStacksPerStack = 0;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Duration of Swift Skateboard's movement speed buff.")]
        [TokenModifier(token, StatTypes.Default, 4)]
        public static float buffDuration = 6f;

        [RooConfigurableField(SS2Config.IDItem, ConfigDesc = "Whether Swift Skateboard should allow all-directional sprinting.")]
        public static bool omniSprint = true;

        public static GameObject effectPrefab = SS2Assets.LoadAsset<GameObject>("SkateboardActivate", SS2Bundle.Items);
        public sealed class Behavior : BaseItemBodyBehavior, IBodyStatArgModifier
        {
            [ItemDefAssociation]
            private static ItemDef GetItemDef() => SS2Content.Items.SwiftSkateboard;

            private bool hadOmniSprint;

            public void Start()
            {
                body.onSkillActivatedServer += Kickflip;

                if (omniSprint)
                {
                    //check if body prefab had sprintanydirection
                    GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(this.body.bodyIndex);
                    if(bodyPrefab)
                    {
                        CharacterBody body = bodyPrefab.GetComponent<CharacterBody>();
                        hadOmniSprint = body.bodyFlags.HasFlag(CharacterBody.BodyFlags.SprintAnyDirection);
                    }
                    this.body.bodyFlags |= CharacterBody.BodyFlags.SprintAnyDirection;
                }
                
            }

            private void Kickflip(GenericSkill skill)
            {
                // no buff on primary use. increased duration to compensate
                if (this.body.skillLocator.primary == skill) return;

                if (!body.HasBuff(SS2Content.Buffs.BuffKickflip))
                {
                    Util.PlaySound("SwiftSkateboard", body.gameObject);
                }
                if (stack > 0)
                {
                    int buffCount = body.GetBuffCount(SS2Content.Buffs.BuffKickflip);
                    int maxBuffStack = maxStacks + (maxStacksPerStack * (stack - 1));
                    //body.AddTimedBuff(SS2Content.Buffs.BuffKickflip, buffDuration, maxStacks + ((stack - 1) * maxStacksPerStack));
                    if (buffCount < maxBuffStack)
                    {
                        body.AddTimedBuff(SS2Content.Buffs.BuffKickflip.buffIndex, buffDuration); 
                    }
                    else if (buffCount == maxBuffStack)
                    {
                        body.RemoveOldestTimedBuff(SS2Content.Buffs.BuffKickflip.buffIndex);
                        body.AddTimedBuff(SS2Content.Buffs.BuffKickflip.buffIndex, buffDuration);
                    }

                    RefreshBuff();


                    // NO SOUND :((((((((((((((((((((((((((((((
                    EffectData effectData = new EffectData();
                    effectData.origin = base.body.corePosition;
                    CharacterDirection characterDirection = base.body.characterDirection;
                    effectData.rotation = characterDirection && characterDirection.moveVector != Vector3.zero ? Util.QuaternionSafeLookRotation(characterDirection.moveVector) : base.body.transform.rotation;
                    EffectManager.SpawnEffect(effectPrefab, effectData, true);
                }
            }

            private void RefreshBuff()
            {
                for (int i = 0; i < body.timedBuffs.Count; i++)
                {
                    if (body.timedBuffs[i].buffIndex == SS2Content.Buffs.BuffKickflip.buffIndex)
                    {
                        body.timedBuffs[i].timer = buffDuration;
                    }
                }
            }

            private void OnDestroy()
            {
                body.onSkillActivatedServer -= Kickflip;

                if (omniSprint && !this.hadOmniSprint)
                {
                    this.body.bodyFlags &= ~CharacterBody.BodyFlags.SprintAnyDirection;
                }
            }

            public void ModifyStatArguments(RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body.HasBuff(SS2Content.Buffs.BuffKickflip) && stack > 0)
                {
                    //args.baseMoveSpeedAdd += (body.baseMoveSpeed + body.levelMoveSpeed * (body.level - 1)) * (moveSpeedBonus * body.GetBuffCount(Starstorm2Content.Buffs.BuffKickflip));
                    args.moveSpeedMultAdd += (moveSpeedBonus + ((stack - 1) * moveSpeedBonusPerStack)) * body.GetBuffCount(SS2Content.Buffs.BuffKickflip);
                }
            }
        }
    }
}