using LOR_DiceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomDLLs
{
    public class DiceCardSelfAbility_extra_clash_dice : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Clash] Inflict 2 Fragile and add two Slash dice (Roll: 5-8) to the dice queue";

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
            firstDiceCopy.ActionScript = null;
            firstDiceCopy.Script = null;
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
        public static string Desc = "Cannot be redirected. [On Combat Start][Target Ally] Spend 1 stack of The Bonds that Bind Us and inflict 2 Feeble and Disarm on self to revive the target at half health and max stagger resist at the end of the round.";
        public override void OnStartBattle()
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
            if (bondsBuff?.stack > 0)
            {
                card.target.bufListDetail.AddBuf(new BattleUnitBuf_revive_at_turn_end());
                bondsBuff.stack--;
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Weak, 2);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 2);
            }
        }

        public override bool IsOnlyAllyUnit()
        {
            return true;
        }

        public override bool IsValidTarget(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
            return bondsBuff?.stack > 0 && targetUnit.IsKnockout();
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
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
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
                var targetBondsBuff = card.target.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
                if (targetBondsBuff == null)
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
                        BattlePlayingCardDataInUnitModel.SubTarget subTarget = new BattlePlayingCardDataInUnitModel.SubTarget();
                        subTarget.target = battleUnitModel;
                        subTarget.targetSlotOrder = UnityEngine.Random.Range(0, battleUnitModel.speedDiceResult.Count);
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
    public class DiceCardSelfAbility_fastforward : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Grant all allies 2 Haste next Scene and draw 1 page.";

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
}
