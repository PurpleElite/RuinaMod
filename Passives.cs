
using CustomDLLs;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomDLLs
{
    public class PassiveAbility_seraph_radiant_perseverance : PassiveAbilityBase
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
            //TODO: Remove this before publishing
            foreach (var unit in BattleObjectManager.instance.GetAliveList())
            {
                unit.passiveDetail.AddPassive(new PassiveAbility_seraph_analytics_data());
            }
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

    public class PassiveAbility_seraph_second_wind : PassiveAbilityBase
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

    public class PassiveAbility_seraph_together_to_the_end : PassiveAbilityBase
    {
        public static string Desc = "Allies are knocked out instead of dying while this character is alive. Gain a unique card that revives an ally and weakens the user for the turn at the cost of a stack of The Bonds that Bind Us.";
        private const int _cardId = 13;
        bool _cardAdded = false;
        public override void OnWaveStart()
        {
            
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            allies.Remove(owner);
            foreach (var ally in allies)
            {
                ally.SetKnockoutInsteadOfDeath(true);
                ally.passiveDetail.AddPassive(new PassiveAbility_seraph_no_clash_allies(_cardId));
            }
        }
        public override void OnRoundStart()
        {
            var allies = BattleObjectManager.instance.GetFriendlyAllList(owner.faction);
            if (allies.Any(x => x.IsKnockout()))
            {
                if (!_cardAdded)
                {
                    owner.allyCardDetail.AddNewCard(new LorId(ModData.WorkshopId, _cardId));
                    _cardAdded = true;
                }
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

    public class PassiveAbility_seraph_bonds_that_bind_us_defend : PassiveAbility_seraph_bonds_base
    {
        public override int CardId { get => 9; }

        protected override int DesiredStacks { get => 2; }

        public override void RoundStartEffect(int stacks)
        {
            var buffAmount = Math.Min(stacks, 3);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, buffAmount);
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            allies.Remove(owner);
            var allyToBuff = RandomUtil.SelectOne(allies);
            allyToBuff.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, buffAmount);
        }
    }

    public class PassiveAbility_seraph_bonds_that_bind_us_restore : PassiveAbility_seraph_bonds_base
    {
        public override int CardId { get => 10; }
        public override void RoundStartEffect(int stacks)
        {
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction).ToList();
            var restoreAmount = Math.Min(stacks * 5, 15);
            for (int i = 0; i < 2; i++)
            {
                if (allies.Count() > 0)
                {
                    var allyToHeal = RandomUtil.SelectOne(allies);
                    allyToHeal.RecoverHP(restoreAmount);
                    if (!allyToHeal.IsBreakLifeZero() && allyToHeal.breakDetail.breakGauge > 0)
                    {
                        allyToHeal.breakDetail.RecoverBreak(restoreAmount);
                    }
                    allies.Remove(allyToHeal);
                }
            }
        }
    }

    public class PassiveAbility_seraph_bonds_that_bind_us_drive : PassiveAbility_seraph_bonds_base
    {
        public override int CardId { get => 11; }

        public override void RoundStartEffect(int stacks)
        {
            var buffStacks = Math.Max(3, (int)Math.Ceiling(stacks / 2f));
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, buffStacks);
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            allies.Remove(owner);
            var allyToBuff = RandomUtil.SelectOne(allies);
            allyToBuff.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, buffStacks);
        }
    }

    public class PassiveAbility_seraph_bonds_base : PassiveAbilityBase
    {
        public static string Desc = "At the start of the act gain one stack of The Bonds that Bind Us and add a unique combat page to hand.";
        public virtual int CardId { get => 0; }

        protected virtual int DesiredStacks { get => 1; }

        public override void OnWaveStart()
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_seraph_bonds());
            owner.allyCardDetail.AddNewCard(new LorId(ModData.WorkshopId, CardId));
            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
            foreach (var ally in allies)
            {
                ally.passiveDetail.AddPassive(new PassiveAbility_seraph_no_clash_allies(CardId));
            }
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            if (card.GetID() == new LorId(ModData.WorkshopId, CardId))
            {
                var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
                if (bondsBuff?.stack > DesiredStacks)
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

        public override int GetPriorityAdder(BattleDiceCardModel card, int speed)
        {
            if (card.GetID() == new LorId(ModData.WorkshopId, CardId))
            {
                var allies = BattleObjectManager.instance.GetAliveList();
                var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
                if (allies.Count <= 1 && bondsBuff?.stack <= DesiredStacks)
                {
                    return -50;
                }
                else if (bondsBuff?.stack > DesiredStacks)
                {
                    return (speed - 8 * 10);
                }
            }
            return base.GetPriorityAdder(card, speed);
        }

        public override void OnRoundStart()
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
            if (bondsBuff != null && bondsBuff.stack > 0)
            {
                RoundStartEffect(bondsBuff.stack);
            }
        }

        public virtual void RoundStartEffect(int stacks) { }
    }

    public class PassiveAbility_seraph_fueled_by_light : PassiveAbilityBase
    {
        public override void OnRoundStart()
        {
            if (owner.emotionDetail.EmotionLevel >= 3)
            {
                owner.cardSlotDetail.RecoverPlayPoint(2);
                owner.allyCardDetail.DrawCards(1);
            }
        }
    }

    public class PassiveAbility_seraph_find_the_opening : PassiveAbilityBase
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

    public class PassiveAbility_seraph_the_nymane_key : PassiveAbilityBase
    {
        public override void OnRoundStart()
        {
            var stacks = Owner.bufListDetail.GetKewordBufStack(KeywordBuf.Decay);
            Owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, stacks);
        }

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

        public override double ChangeDamage(BattleUnitModel attacker, double dmg)
        {
            return dmg - 2;
        }

        //Linus prioritizes targets based on which ones have unstable entropy, followed by which one has the most erosion 
        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            var potentialTargets = BattleObjectManager.instance.GetAliveList(owner.faction == Faction.Player ? Faction.Enemy : Faction.Player)
                .OrderBy(x => UnityEngine.Random.value)
                .OrderBy(x => x.bufListDetail.GetActivatedBuf(KeywordBuf.Decay)?.stack);
            var targetsWithEntropy = potentialTargets.Where(x => x.bufListDetail.GetActivatedBufList().Any(y => y is BattleUnitBuf_unstable_entropy buff && buff?.stack > 1));
            if (card.GetID() == new LorId(ModData.WorkshopId, 14))
            {
                var targetWithoutEntropy = potentialTargets.Except(targetsWithEntropy).FirstOrDefault();
                if (targetWithoutEntropy != null)
                {
                    return targetWithoutEntropy;
                }
            }
            return targetsWithEntropy.FirstOrDefault() ?? potentialTargets.First() ?? base.ChangeAttackTarget(card, idx);
        }
    }

    public class PassiveAbility_seraph_keep_them_busy : PassiveAbilityBase
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

    public class PassiveAbility_seraph_command_the_wind : PassiveAbilityBase
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

    public class PassiveAbility_seraph_sting_like_a_bee : PassiveAbilityBase
    {
        public override void OnWinParrying(BattleDiceBehavior behavior)
        {
            if (behavior != null)
            {
                if (behavior.Type == BehaviourType.Def && behavior.Detail == BehaviourDetail.Evasion && behavior?.DiceVanillaValue == behavior.GetDiceMax())
                {
                    Debug.Log("Sting like a bee triggered");
                    var diceBehavior = new BattleDiceBehavior
                    {
                        behaviourInCard = new DiceBehaviour
                        {
                            Min = 3,
                            Dice = 4,
                            Type = BehaviourType.Atk,
                            Detail = BehaviourDetail.Penetrate,
                            MotionDetail = MotionDetail.Z,
                            EffectRes = "Bayyard_Z"
                        }
                    };
                    diceBehavior.SetIndex(behavior.card.currentBehavior?.Index + 1 ?? 0);
                    behavior.card.AddDice(diceBehavior);
                }
            }
        }
    }

    public class PassiveAbility_seraph_twelve_fixers_evade : PassiveAbilityBase
    {
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            if (behavior.Detail == BehaviourDetail.Evasion)
            {
                int min = 2;
                int max = owner.emotionDetail.EmotionLevel >= 3 ? 1 : 0;
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    min = min,
                    max = max
                });
            }
        }
    }

    public class PassiveAbility_seraph_deflect_assaultJo : PassiveAbilityBase
    {
        public override void OnStartBattle()
        {
            DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(ModData.WorkshopId, 20));
            List<BattleDiceBehavior> counterDice = new List<BattleDiceBehavior>();
            int index = 0;
            foreach (DiceBehaviour diceBehaviour in card.DiceBehaviourList)
            {
                BattleDiceBehavior battleDiceBehavior = new BattleDiceBehavior();
                battleDiceBehavior.behaviourInCard = diceBehaviour.Copy();
                battleDiceBehavior.SetIndex(index++);
                counterDice.Add(battleDiceBehavior);
            }
            owner.cardSlotDetail.keepCard.AddBehaviours(card, counterDice);
        }
    }

    public class PassiveAbility_seraph_no_clash_allies : PassiveAbilityBase
    {
        private readonly int _cardId = -1;
        public PassiveAbility_seraph_no_clash_allies(int cardId = -1)
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

    public class PassiveAbility_seraph_analytics_data : PassiveAbilityBase
    {
        public int DamageTaken = 0;
        public int DamageTakenFromAtacks = 0;
        public int DamageDealtWithAttacks = 0;
        public int DamageTakenFromErode = 0;

        public override void AfterTakeDamage(BattleUnitModel attacker, int dmg)
        {
            DamageTaken += dmg;
        }
        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            DamageTakenFromAtacks += dmg;
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay buf)
            {
                DamageTakenFromErode += buf.stack;
            }
        }
        public override void AfterGiveDamage(int damage)
        {
            DamageDealtWithAttacks += damage;
        }
        public override void OnRoundEnd()
        {
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay buf)
            {
                DamageTakenFromErode += buf.stack;
            }
        }
        public override void OnBattleEnd()
        {
            Debug.Log($"{owner.UnitData.unitData.name} stats:\n" +
                $"Damage Taken: {DamageTaken}\n" +
                $"Damage Taken From Attacks: {DamageTakenFromAtacks}\n" +
                $"Damage Taken From Erode: {DamageTakenFromErode}\n" +
                $"Damage Dealt With Attacks: {DamageDealtWithAttacks}");
        }
    }
}