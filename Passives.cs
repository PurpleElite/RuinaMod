﻿
using CustomDLLs;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomDLLs
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
        public static string Desc = "Allies are knocked out instead of dying while this character is alive. Gain a unique card that revives an ally and weakens the user for the turn at the cost of a stack of The Bonds that Bind Us.";
        private const int _cardId = 13;
        public override void OnWaveStart()
        {
            owner.allyCardDetail.AddNewCard(new LorId(ModData.WorkshopId, _cardId));
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            allies.Remove(owner);
            foreach (var ally in allies)
            {
                ally.SetKnockoutInsteadOfDeath(true);
                ally.passiveDetail.AddPassive(new PassiveAbility_no_clash_allies(_cardId));
            }
        }

        public override void OnDie()
        {
            var allies = BattleObjectManager.instance.GetFriendlyAllList(owner.faction);
            foreach (var ally in allies)
            {
                ally.SetKnockoutInsteadOfDeath(false);
            }
        }

        public override int GetPriorityAdder(BattleDiceCardModel card, int speed)
        {
            if (card.GetID() == new LorId(ModData.WorkshopId, _cardId))
            {
                var allies = BattleObjectManager.instance.GetFriendlyAllList(owner.faction);
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
        public override void RoundStartEffect()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 2);
        }
    }

    public class PassiveAbility_bonds_that_bind_us_restore : PassiveAbility_bonds_base
    {
        public override int CardId { get => 10; }
        public override void RoundStartEffect()
        {
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction).ToList();
            for (int i = 0; i < 2; i++)
            {
                if (allies.Count() > 0)
                {
                    var allyToHeal = RandomUtil.SelectOne(allies);
                    allyToHeal.RecoverHP(10);
                    if (!allyToHeal.IsBreakLifeZero() && allyToHeal.breakDetail.breakGauge > 0)
                    {
                        allyToHeal.breakDetail.RecoverBreak(10);
                    }
                    allies.Remove(allyToHeal);
                }
            }
        }
    }

    public class PassiveAbility_bonds_that_bind_us_drive : PassiveAbility_bonds_base
    {
        public override int CardId { get => 11; }

        public override void RoundStartEffect()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, 1);
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            allies.Remove(owner);
            var allyToBuff = RandomUtil.SelectOne(allies);
            allyToBuff.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, 1);
        }
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

        public override void OnRoundStart()
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
            if (bondsBuff != null && bondsBuff.stack > 0)
            {
                RoundStartEffect();
            }
        }

        public virtual void RoundStartEffect() { }
    }

    public class PassiveAbility_find_the_opening : PassiveAbilityBase
    {
        private int _count;
        public override void OnRoundStart()
        {
            _count = 2;
        }
        public override void OnEndOneSideVictim(BattlePlayingCardDataInUnitModel attackerCard)
        {
            if (_count > 0)
            {
                _count--;
                owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Strength, 1);
            }    
        }
    }

    public class PassiveAbility_the_nymane_key : PassiveAbilityBase
    {
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            if (RandomUtil.valueForProb < 0.5f)
            {
                BattleCardTotalResult battleCardResultLog = owner.battleCardResultLog;
                if (battleCardResultLog != null)
                {
                    battleCardResultLog.SetPassiveAbility(this);
                }
                BattleUnitModel target = behavior.card.target;
                if (target == null)
                {
                    return;
                }
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Decay, 1, owner);
                if (target.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay decay)
                {
                    decay.ChangeToYanDecay();
                }
            }
        }
    }

    public class PassiveAbility_keep_them_busy : PassiveAbilityBase
    {
        private int _count;

        public override void OnRoundStart()
        {
            _count = 0;
        }
        public override void OnWinParrying(BattleDiceBehavior behavior)
        {
            if (behavior.Detail == BehaviourDetail.Evasion || behavior.Detail == BehaviourDetail.Guard)
            {
                _count++;
                if (_count % 3 == 0)
                {
                    var target = behavior.card.target;
                    target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Binding, 1);
                    target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Weak, 1);
                }
            }
        }
    }

    public class PassiveAbility_command_the_wind : PassiveAbilityBase
    {
        public override void OnWaveStart()
        {
            owner.breakDetail.blockRecoverBreakByEvaision = true;
        }
        public override void OnWinParrying(BattleDiceBehavior behavior)
        {
            if (behavior.Type == BehaviourType.Def
                && behavior.Detail == BehaviourDetail.Evasion
                && (behavior.TargetDice.Detail == BehaviourDetail.Guard || behavior.TargetDice.Type == BehaviourType.Atk))
            {
                var recoveryAmount = behavior.DiceResultValue / 2;
                owner.breakDetail.RecoverBreak(recoveryAmount);
                var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
                allies.Remove(owner);
                for (int i = 0; i < 2; i++)
                {
                    if (allies.Count() > 0)
                    {
                        var allyToHeal = RandomUtil.SelectOne(allies);
                        allyToHeal.breakDetail.RecoverBreak(recoveryAmount);
                        allies.Remove(allyToHeal);
                    }
                }
            }
        }
    }

    public class PassiveAbility_sting_like_a_bee : PassiveAbilityBase
    {
        public override void OnWinParrying(BattleDiceBehavior behavior)
        {
            if (behavior?.Detail == BehaviourDetail.Evasion && behavior?.DiceVanillaValue == behavior?.GetDiceMax())
            {
                var diceBehavior = new BattleDiceBehavior
                {
                    behaviourInCard =
                    {
                        Min = 3,
                        Dice = 4,
                        Type = BehaviourType.Atk,
                        Detail = BehaviourDetail.Penetrate,
                        MotionDetail = MotionDetail.Z,
                        EffectRes = "Bayyard_Z"
                    }
                };
                behavior.card.AddDice(diceBehavior);
            }
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
}