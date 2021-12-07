
using CustomDLLs;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Passives
{
    public class PassiveAbility_radiant_perseverance : PassiveAbilityBase
    {
        public static string Desc = "Cannot be damaged unless staggered and can still act while staggered. Will not die until the end of the round after taking damage that would otherwise be fatal. Gain 3 strength and endurance while staggered and even more when at death's door";

        private BreakState _breakState = BreakState.noBreak;
        private bool _die;
        private float _hpBeforeDamage;
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
            _breakState = BreakState.noBreak;
            _die = false;
            _hpBeforeDamage = owner.MaxHp;
        }

        public override bool IsImmuneDmg()
        {
            return _breakState == BreakState.noBreak;
        }

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            _hpBeforeDamage = owner.hp;
            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override void AfterTakeDamage(BattleUnitModel attacker, int dmg)
        {
            if (!IsImmuneDmg() && (_hpBeforeDamage - dmg <= 0)  && !_die)
            {
                attacker.OnKill(owner);
                _die = true;
                Debug.Log("about to die, adding 10 strength and endurance");
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 10, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 10, owner);
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
            if (_breakState == BreakState.brokeThisRound)
            {
                owner.breakDetail.nextTurnBreak = false;
                owner.breakDetail.breakLife = owner.MaxBreakLife;
                owner.currentDiceAction = _diceActionPreBreak;
                owner.turnState = _turnStatePreBreak;
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 3);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Endurance, 3);
                owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 3);
                owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, 3);
            }
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

        public override void OnRoundEnd()
        {
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
                owner.battleCardResultLog.SetBreakState(false);
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
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || card.GetID() == new LorId(ModData.WorkshopId, 8))
            {
                if (GetStaggeredTargets().Count() > 0)
                {
                    return 200;
                }
                else
                {
                    return -50;
                }
            }
            return base.GetPriorityAdder(card, speed);
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            //If card has execute effect, prioritize staggered targets
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || card.GetID() == new LorId(ModData.WorkshopId, 8))
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
            return list.Where(x => x.IsBreakLifeZero() || x.breakDetail.breakGauge == 0).OrderBy(x => x.hp);
        }
    }

    public class PassiveAbility_second_wind : PassiveAbilityBase
    {
        public static string Desc = "Restore all Light at end of turn after being Staggered";

        private bool _staggered;
        public override void OnWaveStart()
        {
            _staggered = false;
        }

        public override void OnBreakState()
        {
            _staggered = true;
        }

        public override void OnRoundEnd()
        {
            if (_staggered)
            {
                owner.cardSlotDetail.ResetPlayPoint();
            }
            _staggered = false; 
        }
    }

    public class PassiveAbility_together_to_the_end : PassiveAbilityBase
    {
        public static string Desc = "Allies are only knocked out while this character is alive. Gain a unique card that revives an ally and weakens the user for the turn at the cost of a stack of The Bonds that Bind Us.";
        private const int _cardId = 12;
        public override void OnWaveStart()
        {
            owner.allyCardDetail.AddNewCard(new LorId(ModData.WorkshopId, _cardId));
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                ally.SetKnockoutInsteadOfDeath(true);
                ally.passiveDetail.AddPassive(new PassiveAbility_no_clash_allies(_cardId));
            }
        }

        public override int GetPriorityAdder(BattleDiceCardModel card, int speed)
        {
            //If there are staggered targets and the card has execute effect, prioritize using it
            if (card.GetID() == new LorId(ModData.WorkshopId, 12))
            {
                var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
                if (allies.Any(x => x.IsKnockout()))
                {
                    return 200;
                }
                else
                {
                    return -50;
                }
            }
            return base.GetPriorityAdder(card, speed);
        }
    }

    public class PassiveAbility_bonds_that_bind_us_defend : PassiveAbility_bonds_base
    {
        public override int CardId { get => 9; }
    }

    public class PassiveAbility_bonds_that_bind_us_restore : PassiveAbility_bonds_base
    {
         public override int CardId { get => 10; }
    }

    public class PassiveAbility_bonds_that_bind_us_drive : PassiveAbility_bonds_base
    {
        public override int CardId { get => 11; }
    }

    public class PassiveAbility_bonds_base : PassiveAbilityBase
    {
        public static string Desc = "At the start of the act gain one stack of The Bonds that Bind Us and add a unique combat page to hand.";
        public virtual int CardId { get => 0; }
        public override void OnWaveStart()
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_bonds());
            owner.allyCardDetail.AddNewCard(new LorId(ModData.WorkshopId, CardId));
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                ally.passiveDetail.AddPassive(new PassiveAbility_no_clash_allies(CardId));
            }
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            if (card.GetID() == new LorId(ModData.WorkshopId, CardId))
            {
                var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
                if (bondsBuff?.stack >= 2)
                {
                    Debug.Log("Targeting enemies with bond card");
                    return base.ChangeAttackTarget(card, idx);
                }
                //TODO: prioritize allies with less stacks
                var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
                allies.Remove(owner);
                return allies.Any() ? RandomUtil.SelectOne(allies) : base.ChangeAttackTarget(card, idx);
            }
            return base.ChangeAttackTarget(card, idx);
        }
    }

    public class PassiveAbility_no_clash_allies : PassiveAbilityBase
    {
        private readonly int _cardId = -1;
        public PassiveAbility_no_clash_allies(int cardId = -1)
        {
            _cardId = cardId;
        }

        public override bool AllowTargetChanging(BattleUnitModel attacker, int myCardSlotIdx)
        {
            if (attacker.faction == owner.faction && attacker.IsControlable())
            {
                if (_cardId == -1)
                {
                    return false;
                }
                if (attacker.cardSlotDetail.cardAry.Exists(x => x?.target == owner
                    && x?.targetSlotOrder == myCardSlotIdx
                    && x?.card?.GetID() == new LorId(ModData.WorkshopId, _cardId)))
                {
                    return false;
                }
            }
            return base.AllowTargetChanging(attacker, myCardSlotIdx);
        }

        public override bool isHide => true;
    }

    public class BehaviourAction_johannaexecute : BehaviourActionBase
    {
        public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        {
            Debug.Log("execute animation called");
            bool opponentBroken = false;
            if (opponent.behaviourResultData != null)
            {
                opponentBroken = !opponent.behaviourResultData.isBreaked;
            }
            Debug.Log("opponentBroken: " + opponentBroken);
            if (self.result != Result.Win || !opponentBroken)
            {
                return base.GetMovingAction(ref self, ref opponent);
            }
            var moveList = new List<RencounterManager.MovingAction>();
            var movingAction1 = new RencounterManager.MovingAction(ActionDetail.Move, CharMoveState.Stop, 0f, true, 1f);
            var movingAction2 = new RencounterManager.MovingAction(ActionDetail.Fire, CharMoveState.Stop);
            movingAction2.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
            movingAction2.customEffectRes = "TwistedElena_H";
            moveList.Add(movingAction1);
            moveList.Add(movingAction2);

            if (opponent.infoList.Count > 0)
            {
                opponent.infoList.Clear();
            }
            opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Stop, 0f, true, 1f));
            opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Knockback, 2f, true));

            return moveList;
        }
    }

    public class BehaviourAction_linus_area : BehaviourActionBase
    {
        // Token: 0x06001177 RID: 4471 RVA: 0x0008DE71 File Offset: 0x0008C071
        public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
        {
            Debug.Log("linus behavior action called");
            _self = self;
            FarAreaeffect_LinusArea farAreaeffect_LinusArea = new GameObject().AddComponent<FarAreaeffect_LinusArea>();
            farAreaeffect_LinusArea.Init(self, Array.Empty<object>());
            return farAreaeffect_LinusArea;
        }
    }

}