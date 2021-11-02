
using LOR_DiceSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Passives
{
    public class DiceCardAbility_multi : DiceCardAbilityBase
    {
        /// <summary>
        /// Description
        /// </summary>
        public static string Desc = "This die is rolled up to 5 times at the cost of one light per extra roll.";

        private int rerollCount = 0;
        public override void AfterAction()
        {
            Debug.Log("AfterAction multislash, light: " + owner.cardSlotDetail.PlayPoint + ", rerollCount: " + rerollCount);
            if (rerollCount < 4 && owner.cardSlotDetail.PlayPoint > 0)
            {
                Debug.Log("Light count: " + owner.cardSlotDetail.PlayPoint);
                owner.cardSlotDetail.LosePlayPoint(1);
                ActivateBonusAttackDice();
                rerollCount++;
            }
        }
    }

    public class DiceCardAbility_execute : DiceCardAbilityBase
    {
        /// <summary>
        /// Description
        /// </summary>
        public static string Desc = "[On Hit] If target is Staggered deal 50 damage";

        public override void OnSucceedAttack()
        {
            var target = card.target;
            Debug.Log("execute OnSuceedAttack target " + target.view.name);
            if (target != null && target.IsBreakLifeZero())
            {
                Debug.Log("Target break life is zero");
                target.TakeDamage(50, DamageType.Card_Ability, owner);
            }
            base.OnSucceedAttack();
        }
    }

    public class PassiveAbility_radiant_perseverance : PassiveAbilityBase
    {
        /// <summary>
        /// Description
        /// </summary>
        public static string Desc = "Cannot be damaged unless staggered and can still act while staggered. Will not die until the end of the round after taking damage that would otherwise be fatal. Gains strength and endurance while staggered and even more when at death's door";
        private const string WorkshopId = "SeraphOffice";

        private BreakState _breakState = BreakState.noBreak;
        private bool _die;
        private BattlePlayingCardDataInUnitModel _diceActionPreBreak = new BattlePlayingCardDataInUnitModel();
        private BattleUnitTurnState _turnStatePreBreak = BattleUnitTurnState.WAIT_TURN;
        private enum BreakState { noBreak, brokeThisRound, broken };

        public override bool isImmortal
        {
            get
            {
                return true;
            }
        }

        public override void OnWaveStart()
        {
            Debug.Log("Debug.Log is working");
            _breakState = BreakState.noBreak;
            _die = false;
        }

        public override bool IsImmuneDmg()
        {
            return _breakState == BreakState.noBreak;
        }


        public override void AfterTakeDamage(BattleUnitModel attacker, int dmg)
        {
            if ((_breakState != BreakState.noBreak) && dmg >= owner.hp && !_die)
            {
                attacker.OnKill(owner);
                _die = true;
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 8, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 8, owner);
            }
        }

        public override bool OnBreakGageZero()
        {
            if (_breakState == BreakState.noBreak)
            {
                _breakState = BreakState.brokeThisRound;
                owner.breakDetail.blockRecoverBreakByEvaision = true;
                _diceActionPreBreak = owner.currentDiceAction;
                _turnStatePreBreak = owner.turnState;
                SetStaggeredMotion(true);
                return false;
            }
            return true;
        }

        public override void OnBreakState()
        {
            owner.breakDetail.nextTurnBreak = false;
            owner.breakDetail.breakLife = owner.MaxBreakLife;
            owner.currentDiceAction = _diceActionPreBreak;
            owner.turnState = _turnStatePreBreak;
            owner.battleCardResultLog.SetBreakState(false);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 3, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 3, owner);
            owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 3, owner);
            owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, 3, owner);
        }

        public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
        {
            if (_breakState != BreakState.noBreak)
            {
                return AtkResist.Weak;
            }
            return base.GetResistHP(origin, detail);
        }

        public override AtkResist GetResistBP(AtkResist origin, BehaviourDetail detail)
        {
            if (_breakState != BreakState.noBreak)
            {
                return AtkResist.Weak;
            }
            return base.GetResistBP(origin, detail);
        }

        public override void OnRoundEnd_before()
        {
            Debug.Log("Round End");
            if (_die)
            {
                owner.Die();
            }
            else if (_breakState == BreakState.brokeThisRound)
            {
                _breakState = BreakState.broken;
                foreach (var card in owner.allyCardDetail.GetAllDeck())
                {
                    var dice = card.GetBehaviourList();
                    var counterDiceAverages = dice.Where(x => x.Type == BehaviourType.Standby).Select(x => (x.Min + x.Dice) / 2);
                    var counterDiceScore = counterDiceAverages.Sum() + counterDiceAverages.Count() * 10;
                    card.SetPriorityAdder(card.GetPriorityAdder() + counterDiceScore);
                }
            }
            else if (_breakState == BreakState.broken)
            {
                _breakState = BreakState.noBreak;
                owner.breakDetail.blockRecoverBreakByEvaision = false;
                owner.breakDetail.ResetBreakDefault();
                owner.OnReleaseBreak();
                SetStaggeredMotion(false);
                foreach (var card in owner.allyCardDetail.GetAllDeck())
                {
                    var dice = card.GetBehaviourList();
                    var counterDiceAverages = dice.Where(x => x.Type == BehaviourType.Standby).Select(x => (x.Min + x.Dice) / 2);
                    var counterDiceScore = counterDiceAverages.Sum() * 3 + counterDiceAverages.Count() * 10;
                    card.SetPriorityAdder(card.GetPriorityAdder() - counterDiceScore);
                }
            }
            base.OnRoundEnd_before();
        }

        private void SetStaggeredMotion(bool isStaggered)
        {
            var charAppearance = owner.view.charAppearance;
            if (isStaggered)
            {
                charAppearance.SetAltMotion(ActionDetail.Default, ActionDetail.Damaged);
                charAppearance.SetAltMotion(ActionDetail.Standing, ActionDetail.Damaged);
            }
            else
            {
                charAppearance.RemoveAltMotion(ActionDetail.Default);
                charAppearance.RemoveAltMotion(ActionDetail.Standing);
            }
        }

        //General AI behavior stuff for Johanna
        public override int GetPriorityAdder(BattleDiceCardModel card, int speed)
        {
            //If there are staggered targets and the card has execute effect, prioritize using it
            Debug.Log($"Card in hand: {card.GetName()}, Scripts: {string.Join(",", card.GetBehaviourList().Select(x => x.Script).Where(x => !string.IsNullOrEmpty(x)))}, Package ID: {card.GetID().packageId}, Text ID: {card.GetTextId().id}");
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || (card.GetID().packageId == WorkshopId && card.GetTextId().id == 80085))
            {
                if (GetStaggeredTargets().Count() > 0)
                {
                    Debug.Log("Off With Their Heads in hand and at least one staggered enemy");
                    return 200;
                }
                else
                {
                    Debug.Log("Off With Their Heads in hand but no staggered enemy");
                    return -50;
                }
            }
            return base.GetPriorityAdder(card, speed);
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            //If card has execute effect, prioritize staggered targets
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || (card.GetID().packageId == WorkshopId && card.GetTextId().id == 80085))
            {
                return GetStaggeredTargets().LastOrDefault();
            }
            return base.ChangeAttackTarget(card, idx);
        }

        private IEnumerable<BattleUnitModel> GetStaggeredTargets()
        {
            var aliveList = BattleObjectManager.instance.GetAliveList((owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy);
            var list = new List<BattleUnitModel>();
            var fixedTargets = owner.GetFixedTargets();
            if (fixedTargets != null && fixedTargets.Count > 0)
            {
                foreach (BattleUnitModel battleUnitModel in fixedTargets)
                {
                    if (battleUnitModel.IsTargetable(owner))
                    {
                        list.Add(battleUnitModel);
                    }
                }
            }
            if (list.Count <= 0)
            {
                foreach (BattleUnitModel battleUnitModel in aliveList)
                {
                    if (battleUnitModel.IsTargetable(owner))
                    {
                        list.Add(battleUnitModel);
                    }
                }
            }
            return list.Where(x => x.IsBreakLifeZero()).OrderBy(x => x.hp);
        }
    }

    public class PassiveAbility_second_wind : PassiveAbilityBase
    {
        /// <summary>
        /// Description
        /// </summary>
        public static string Desc = "Restore all light when staggered";

        public override void OnBreakState()
        {
            owner.cardSlotDetail.ResetPlayPoint();
        }
    };

    public class DiceCardSelfAbility_extra_clash_dice : DiceCardSelfAbilityBase
    {
        /// <summary>
        /// Description
        /// </summary>
        public static string Desc = "[On Clash] Inflict 2 Fragile and add two Slash dice (Roll: 5-8) to the dice queue";

        private const BehaviourDetail _diceType = BehaviourDetail.Slash;
        private const MotionDetail _motionType = MotionDetail.H;
        private readonly int min = 5;
        private readonly int max = 8;
        private const int diceCount = 2;
        public override void OnStartParrying()
        {
            Debug.Log("OnStartParrying extra_clash_dice");
            card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Vulnerable, 2, owner);
            var diceOnCard = card.GetDiceBehaviourXmlList();
            var firstDice = diceOnCard.FirstOrDefault(x => x.Detail == _diceType)?.Copy();
            firstDice = firstDice ?? diceOnCard.FirstOrDefault(x => x.Type == BehaviourType.Atk)?.Copy();
            firstDice = firstDice ?? diceOnCard.First().Copy();
            firstDice.Min = min;
            firstDice.Dice = max;
            firstDice.Detail = _diceType;
            firstDice.MotionDetail = _motionType;
            firstDice.Type = BehaviourType.Atk;
            Debug.Log($"new diceBehavior created: {firstDice.Min}-{firstDice.Dice} {firstDice.Detail}");
            for (int i = 0; i < diceCount; i++)
            {
                var battleDiceBehavior = new BattleDiceBehavior();
                battleDiceBehavior.behaviourInCard = firstDice;
                Debug.Log($"new BattleDiceBehavior {battleDiceBehavior.GetDiceMin()}-{battleDiceBehavior.GetDiceMax()} {battleDiceBehavior.Detail}");
                battleDiceBehavior.SetIndex(i + diceOnCard.Count());
                card.AddDice(battleDiceBehavior);
                Debug.Log("Added dice to index " + (i + diceOnCard.Count()));
            }
        }
    }

    //public class BuffUnitBuf_inverted : BattleUnitBuf
    //{
    //    /// <summary>
    //    /// Description
    //    /// </summary>
    //    public static string Desc = "Inverts effects that alter power, damage, and damage resistance";
    //    public override KeywordBuf bufType => KeywordBuf.None;
    //    protected override string keywordId => "Inverted";
    //    public override BufPositiveType positiveType => BufPositiveType.None;
    //    public override void OnRollDice(BattleDiceBehavior behavior)
    //    {
    //        behavior.ApplyDiceStatBonus(new DiceStatBonus
    //        {
    //            power = behavior.PowerAdder * -2,
    //            face = behavior.DiceFaceAdder * -2,
    //            dmg = behavior.DamageAdder * -2,
    //            breakDmg = behavior.BreakAdder * -2,
    //            guardBreakAdder = behavior.GuardBreakAdder * -2,
    //        });

    //    }
    //    //BattleUnitBuf_protection
    //    public override StatBonus GetStatBonus()
    //    {
    //        var buffList = _owner.bufListDetail;
    //        return new StatBonus
    //        {
    //            dmgAdder = buffList.GetDamageIncreaseRate() * -1,
    //            breakAdder = buffList.GetBreakDamageIncreaseRate() * -1,
    //        };
    //    }
    //    public override void BeforeTakeDamage(BattleUnitModel attacker, int dmg)
    //    {
    //        _owner.bufListDetail.GetDamageReduction(attacker.currentDiceAction.currentBehavior);
    //    }
    //}
}