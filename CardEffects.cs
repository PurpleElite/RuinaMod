﻿using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomDLLs
{
    public class DiceCardSelfAbility_extra_clash_dice : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Clash] Inflict 2 Fragile and add two Slash dice (Roll: 5-8) to the dice queue";
        public override string[] Keywords =>  new string[] { "Vulnerable_Keyword" };

        private const BehaviourDetail _diceType = BehaviourDetail.Slash;
        private const MotionDetail _motionType = MotionDetail.H;
        private readonly int min = 5;
        private readonly int max = 8;
        private const int diceCount = 2;
        public override void OnStartParrying()
        {
            card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Vulnerable, 2, owner);
            var diceOnCard = card.GetDiceBehaviourXmlList();
            var firstDiceCopy = diceOnCard.FirstOrDefault(x => x.Detail == _diceType)?.Copy();
            firstDiceCopy = firstDiceCopy ?? diceOnCard.FirstOrDefault(x => x.Type == BehaviourType.Atk)?.Copy();
            firstDiceCopy = firstDiceCopy ?? diceOnCard.First().Copy();
            firstDiceCopy.Min = min;
            firstDiceCopy.Dice = max;
            firstDiceCopy.Detail = _diceType;
            firstDiceCopy.MotionDetail = _motionType;
            firstDiceCopy.Type = BehaviourType.Atk;
            firstDiceCopy.ActionScript = string.Empty;
            firstDiceCopy.Script = string.Empty;
            for (int i = 0; i < diceCount; i++)
            {
                var battleDiceBehavior = new BattleDiceBehavior();
                battleDiceBehavior.behaviourInCard = firstDiceCopy;
                battleDiceBehavior.SetIndex(i + diceOnCard.Count());
                card.AddDice(battleDiceBehavior);
            }
        }
    }

    public class DiceCardSelfAbility_together_to_the_end : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Combat Start] Spend 1 stack of The Bonds that Bind Us and inflict 2 Feeble and Disarm on self. Reduce max Stagger Resist by 20% (Up to 40%) [End of Scene] Revive all Incapacitated allies at half health and max stagger resist.";
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword" };

        const int maxStaggerReductionPercent = 20;
        const int maxStaggerMinimum = 40;

        public override bool OnChooseCard(BattleUnitModel owner)
        {
            return owner.bufListDetail.GetActivatedBufList().Any(x => x is BattleUnitBuf_bonds bondBuff && bondBuff.stack > 0);
        }

        public override void OnStartBattle()
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_bonds);
            if (bondsBuff?.stack > 0)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_revive_all_round_end());
                bondsBuff.stack--;
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Weak, 2);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 2);

                BattleUnitBuf activatedBuf = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_reduce_max_bp);
                if (activatedBuf != null)
                {
                    activatedBuf.stack++;
                }
                else
                {
                    owner.bufListDetail.AddBuf(new BattleUnitBuf_reduce_max_bp());
                }
            }
        }
        class BattleUnitBuf_reduce_max_bp : BattleUnitBuf
        {
            
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    breakRate = -Math.Min(maxStaggerMinimum, stack * maxStaggerReductionPercent)
                };
            }
        }

        class BattleUnitBuf_revive_all_round_end : BattleUnitBuf
        {
            public override bool Hide => true;
            public override void OnRoundEnd()
            {
                var targets = BattleObjectManager.instance.GetList(_owner.faction).Where(x => x.IsKnockout());
                Debug.Log("[SERAPH] revive targets count: " + targets?.Count());
                var knockoutField = typeof(BattleUnitBaseModel).GetField("_isKnockout", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var target in targets ?? Array.Empty<BattleUnitModel>())
                {
                    target.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_knockout)?.Destroy();
                    knockoutField.SetValue(target, false);
                    target.Revive(target.MaxHp / 2);
                    target.breakDetail.nextTurnBreak = false;
                    target.breakDetail.RecoverBreakLife(_owner.MaxBreakLife);
                    SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(target, target.faction, target.hp, target.breakDetail.breakGauge);
                }
                Destroy();
            }
        }
    }

    public class DiceCardSelfAbility_bonds_base : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Combat Start] If target is an ally, give and gain 1 stack of The Bonds That Bind Us.";
        public override void OnStartBattle()
        {
            if (card.target == null)
            {
                return;
            }
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_bonds);
            if (card.target.faction == owner.faction)
            {
                Debug.Log("bondsBuff targeting ally");
                if (bondsBuff == null)
                {
                    owner.bufListDetail.AddBuf(new BattleUnitBuf_bonds());
                }
                else
                {
                    bondsBuff.stack++;
                }
                var targetBondsBuff = card.target.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_bonds);
                if (targetBondsBuff == null || targetBondsBuff.IsDestroyed())
                {
                    card.target.bufListDetail.AddBuf(new BattleUnitBuf_bonds());
                }
                else
                {
                    targetBondsBuff.stack++;
                }
            }
            else
            {
                TargetEnemy(bondsBuff);
            }
            if (bondsBuff?.stack <= 0)
            {
                bondsBuff.Destroy();
            }
        }

        protected virtual void TargetEnemy(BattleUnitBuf bondsBuff)
        {
            return;
        }


        public override bool IsTargetableAllUnit()
        {
            return true;
        }

        public override bool IsValidTarget(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            return true;
        }
    }

    public class DiceCardSelfAbility_bonds_protection : DiceCardSelfAbility_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_bonds_base.Desc + " If target is an enemy, spend 1 stack of The Bonds That Bind Us to redirect all target's cards to the dice with this card instead.";

        protected override void TargetEnemy(BattleUnitBuf bondsBuff)
        {
            if (bondsBuff == null || bondsBuff.stack < 1 || !card.target.IsTauntable())
            {
                return;
            }
            bondsBuff.stack--;
            foreach (var enemyCard in card.target.cardSlotDetail.cardAry.Where(x => x != null))
            {
                enemyCard.target = owner;
                enemyCard.targetSlotOrder = card.slotOrder;
            }
        }
    }

    public class DiceCardSelfAbility_bonds_drive : DiceCardSelfAbility_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_bonds_base.Desc + " If target is an enemy, spend 1 stack of The Bonds That Bind Us to turn this into a Mass-Individual page.";
        private BattleDiceCardModel originalCard;
        protected override void TargetEnemy(BattleUnitBuf bondsBuff)
        {
            if (bondsBuff == null || bondsBuff.stack < 1)
            {
                return;
            }
            //TODO: give the mass attack a proper animation
            bondsBuff.stack--;

            originalCard = card.card;
            var cardXml = ItemXmlDataList.instance.GetCardItem(new LorId(ModData.WorkshopId, 12));
            var cardModel = BattleDiceCardModel.CreatePlayingCard(cardXml);
            cardModel.temporary = true;
            card.card = cardModel;
            card.ResetCardQueue();
            card.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
            var targetList = BattleObjectManager.instance.GetAliveList((card.owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy);
            targetList.Remove(card.target);
            foreach (BattleUnitModel battleUnitModel in targetList)
            {
                if (battleUnitModel.IsTargetable(card.owner))
                {
                    BattlePlayingCardSlotDetail cardSlotDetail = battleUnitModel.cardSlotDetail;
                    bool flag = false;
                    if (cardSlotDetail != null)
                    {
                        List<BattlePlayingCardDataInUnitModel> targetDice = cardSlotDetail.cardAry;
                        int? numDice = targetDice?.Count;
                        flag = (numDice != null && numDice.GetValueOrDefault() > 0);
                    }
                    if (flag)
                    {
                        BattlePlayingCardDataInUnitModel.SubTarget subTarget = new BattlePlayingCardDataInUnitModel.SubTarget
                        {
                            target = battleUnitModel,
                            targetSlotOrder = UnityEngine.Random.Range(0, battleUnitModel.speedDiceResult.Count)
                        };
                        card.subTargets.Add(subTarget);
                        Debug.Log("Added subTarget: " + subTarget.target?.view.name);
                    }
                }
            }
        }

        public override void OnUseCard()
        {
            if (originalCard != null)
            {
                owner.allyCardDetail.SpendCard(originalCard);
                originalCard = null;
            }
        }
    }

    public class DiceCardSelfAbility_bonds_restore : DiceCardSelfAbility_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_bonds_base.Desc + " If target is an enemy, spend 1 stack of The Bonds That Bind Us to grant 1 Light and 1 Positive Emotion Point to allies and purge their ailments";

        protected override void TargetEnemy(BattleUnitBuf bondsBuff)
        {
            if (bondsBuff == null || bondsBuff.stack < 1)
            {
                return;
            }
            bondsBuff.stack--;
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                ally.cardSlotDetail.RecoverPlayPoint(1);
                ally.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive);
                var debuffs = ally.bufListDetail.GetActivatedBufList().Where(x => x.positiveType == BufPositiveType.Negative);
                foreach (var debuff in debuffs)
                {
                    debuff.Destroy();
                }
            }
        }
    }

    public class DiceCardSelfAbility_fastforward : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Grant all allies 2 Haste next Scene and draw 1 page.";
        public override string[] Keywords => new string[] { "Quickness_Keyword" };

        public override void OnUseCard()
        {
            Debug.Log("fastforward OnUseCard()");
            owner.allyCardDetail.DrawCards(1);
            var team = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in team)
            {
                ally.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
            }
        }
    }

    public class DiceCardSelfAbility_eyes_on_me : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Draw 1 page. [On Clash] Change the dice on this page to evasion dice.";

        public override void OnUseCard()
        {
            owner.allyCardDetail.DrawCards(1);
        }

        public override void OnStartParrying()
        {
            foreach (var die in card.cardBehaviorQueue)
            {
                var behavior = die.behaviourInCard.Copy();
                behavior.Detail = BehaviourDetail.Evasion;
                behavior.Type = BehaviourType.Def;
                behavior.EffectRes = "ch2_e";
                behavior.MotionDetail = MotionDetail.E;
                die.behaviourInCard = behavior;
            }
        }
    }

    public class DiceCardSelfAbility_willpower : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Gain Positive Emotion Points equal to current Emotion Level. If at Emotion Level 5 gain 4 Light";

        public override void OnUseCard()
        {
            var emotionDetail = owner.emotionDetail;
            emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, emotionDetail.EmotionLevel);
            if (emotionDetail.EmotionLevel >= 5)
            {
                owner.cardSlotDetail.RecoverPlayPoint(4);
            }
        }
    }

    public class DiceCardSelfAbility_staggerProtection3start : DiceCardSelfAbilityBase
    {
        public static string Desc = "[Combat Start] Gain 3 Stagger Protection";

        public override string[] Keywords => new string[] { "bstart_Keyword", "BreakProtection_Keyword" };

        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 3, owner);
        }
    }

    public class DiceCardSelfAbility_borrowed_time_bind : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Inflict self with 2 Bind next Scene";

        public override string[] Keywords => new string[] { "Binding_Keyword" };

        public override void OnUseCard()
        {
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 2, owner);
        }
    }

    public class DiceCardSelfAbility_force_aggro : DiceCardSelfAbilityBase
    {
        public static string Desc = "The targeted dice becomes untargetable and is forced to clash with this page";

        private BattleUnitBuf_forceAggro _aggroBuf;
        private PassiveAbility_forceAggro _aggroPassive;
        
        public override void OnApplyCard()
        {
            var targetCardsDetail = card.target.cardSlotDetail;
            var targetCard = targetCardsDetail.cardAry[card.targetSlotOrder];
            if (targetCard != null)
            {
                targetCard.target = owner;
                targetCard.targetSlotOrder = card.slotOrder;
            }
            
            foreach (var otherCard in BattleObjectManager.instance.GetAliveList().SelectMany(x => x.cardSlotDetail.cardAry).Where(x => x != null && x != card))
            {
                Debug.Log("PCT OnApplyCard() otherCardOwner: " + otherCard.owner.view.name);
                Debug.Log("otherCardName: " + otherCard.card.GetName());
                Debug.Log("otherCardTarget: " + otherCard.target.view.name);
                Debug.Log("otherCardTargetSlot: " + otherCard.targetSlotOrder);
                if (otherCard.target == card.target && otherCard.targetSlotOrder == card.targetSlotOrder)
                {
                    Debug.Log("otherCard is targeting the same dice as PCT");
                    var indices = Enumerable.Range(0, targetCardsDetail.cardAry.Count()).Where(x => x != card.targetSlotOrder).ToArray();
                    otherCard.targetSlotOrder = RandomUtil.SelectOne(indices);
                    Debug.Log("otherCard targetSlotOrder changed to: " + otherCard.targetSlotOrder);
                }
            }
            _aggroBuf = new BattleUnitBuf_forceAggro(card);
            card.target.bufListDetail.AddBuf(_aggroBuf);
            //_aggroPassive = new PassiveAbility_forceAggro(card);
            //target.owner.passiveDetail.AddPassive(_aggroPassive);
        }

        public override void OnReleaseCard()
        {
            _aggroBuf?.Destroy();
        }

        public override bool IsTargetChangable(BattleUnitModel attacker)
        {
            return false;
        }
        
        private class PassiveAbility_forceAggro : PassiveAbilityBase
        {
            readonly BattlePlayingCardDataInUnitModel _aggroSource;
            public PassiveAbility_forceAggro(BattlePlayingCardDataInUnitModel aggro)
            {
                _aggroSource = aggro;
            }

            public override bool AllowTargetChanging(BattleUnitModel attacker, int myCardSlotIdx)
            {
                if (myCardSlotIdx == _aggroSource.targetSlotOrder)
                {
                    return false;
                }
                return base.AllowTargetChanging(attacker, myCardSlotIdx);
            }
        }

        private class BattleUnitBuf_forceAggro : BattleUnitBuf
        {

            readonly BattlePlayingCardDataInUnitModel _aggroSource;
            readonly FieldInfo _speedDiceField = typeof(LOR_BattleUnit_UI.SpeedDiceUI).GetField("_speedDiceIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            public override bool Hide => true;
            public BattleUnitBuf_forceAggro(BattlePlayingCardDataInUnitModel aggro)
            {
                _aggroSource = aggro;
            }

            public override List<BattleUnitModel> GetFixedTarget()
            {
                var selectedDiceIndex = (int) _speedDiceField.GetValue(Singleton<LOR_BattleUnit_UI.SpeedDiceUI>.Instance);
                Debug.Log("GetFixedTarget() selectedDiceIndex: " + selectedDiceIndex);
                if (selectedDiceIndex == _aggroSource.targetSlotOrder)
                {
                    return new List<BattleUnitModel> { _aggroSource.owner };
                }
                return base.GetFixedTarget();
            }

            public override bool IsTargetable()
            {
                var selectedDiceIndex = (int)_speedDiceField.GetValue(Singleton<LOR_BattleUnit_UI.SpeedDiceUI>.Instance);
                Debug.Log("IsTargetable() selectedDiceIndex: " + selectedDiceIndex);
                return selectedDiceIndex == _aggroSource.targetSlotOrder || base.IsTargetable();
            }

            //public override bool DirectAttack()
            //{
            //    var aggroCard = _owner.cardSlotDetail.cardAry[_aggroSource.targetSlotOrder];
            //    if (aggroCard != null)
            //    {
            //        aggroCard.target = _aggroSource.owner;
            //        aggroCard.targetSlotOrder = _aggroSource.slotOrder;
            //    }
            //    return base.DirectAttack();
            //}

            public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int currentSlot)
            {
                if (currentSlot == _aggroSource.targetSlotOrder)
                {
                    return _aggroSource.owner;
                }
                return base.ChangeAttackTarget(card, currentSlot);
            }

            public override int ChangeTargetSlot(BattleDiceCardModel card, BattleUnitModel target, int currentSlot, int targetSlot, bool teamkill)
            {
                if (currentSlot == _aggroSource.targetSlotOrder)
                {
                    return _aggroSource.slotOrder;
                }
                return base.ChangeTargetSlot(card, target, currentSlot, targetSlot, teamkill);
            }

            public override void OnEndBattlePhase()
            {
                Destroy();
            }
        }
    }

    //public class DiceCardSelfAbility_keep_them_distracted : DiceCardSelfAbilityBase
    //{
    //    public static string Desc = "[On Play] Spend one stack of The Bonds that Bind Us to hide the targets of all of Linus's pages this Scene";

    //    public override bool OnChooseCard(BattleUnitModel owner)
    //    {
    //        return base.OnChooseCard(owner);
    //    }

    //    public override 
    //}
}
