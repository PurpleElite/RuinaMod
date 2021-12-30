using LOR_DiceSystem;
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
        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                    "Vulnerable_Keyword"
                };
            }
        }

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
        public static string Desc = "[On Combat Start] Spend 1 stack of The Bonds that Bind Us and inflict 2 Feeble and Disarm on self [End of Scene] Revive all Incapacitated allies at half health and max stagger resist.";
        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                    "Weak_Keyword",
                    "Disarm_Keyword"
                };
            }
        }

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
        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                    "Quickness_Keyword"
                };
            }
        }

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

        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                    "bstart_Keyword",
                    "BreakProtection_Keyword"
                };
            }
        }

        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 3, owner);
        }
    }

    public class DiceCardSelfAbility_borrowed_time_bind : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Inflict self with 2 Bind next Scene";

        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                    "Binding_Keyword"
                };
            }
        }

        public override void OnUseCard()
        {
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 2, owner);
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
