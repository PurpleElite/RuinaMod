
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
        public static string Desc = "[On Hit] If target is Staggered deal 60 damage";

        public override void OnSucceedAttack()
        {
            var target = card.target;
            Debug.Log("execute OnSuceedAttack target " + target.view.name);
            if (target != null && (target.IsBreakLifeZero() || target.breakDetail.breakGauge == 0))
            {
                Debug.Log("Target break life is zero");
                target.TakeDamage(60, DamageType.Card_Ability, owner);
                BattleCardTotalResult battleCardResultLog = owner.battleCardResultLog;
                if (battleCardResultLog == null)
                {
                    return;
                }
                battleCardResultLog.SetPrintDamagedEffectEvent(new BattleCardBehaviourResult.BehaviourEvent(EarthQuake));
            }
            base.OnSucceedAttack();
        }

        private void EarthQuake()
        {
            FilterUtil.ShowWarpBloodFilter();
            BattleCamManager instance = SingletonBehavior<BattleCamManager>.Instance;
            CameraFilterPack_FX_EarthQuake cameraFilterPack_FX_EarthQuake = ((instance != null) ? instance.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>() : null) ?? null;
            if (cameraFilterPack_FX_EarthQuake != null)
            {
                cameraFilterPack_FX_EarthQuake.X = 0.075f;
                cameraFilterPack_FX_EarthQuake.Y = 0.01f;
                cameraFilterPack_FX_EarthQuake.Speed = 50f;
            }
            BattleCamManager instance2 = SingletonBehavior<BattleCamManager>.Instance;
            AutoScriptDestruct autoScriptDestruct = ((instance2 != null) ? instance2.EffectCam.gameObject.AddComponent<AutoScriptDestruct>() : null) ?? null;
            if (autoScriptDestruct != null)
            {
                autoScriptDestruct.targetScript = cameraFilterPack_FX_EarthQuake;
                autoScriptDestruct.time = 0.4f;
            }
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
            _breakState = BreakState.noBreak;
            _die = false;
        }

        public override bool IsImmuneDmg()
        {
            return _breakState == BreakState.noBreak;
        }


        public override void AfterTakeDamage(BattleUnitModel attacker, int dmg)
        {
            if (!IsImmuneDmg() && owner.hp == 1 && !_die)
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
                owner.battleCardResultLog.SetBreakState(false);
                owner.view.SetCriticals(true, false);
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
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || card.GetID() == new LorId(WorkshopId, 8))
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
            if (card.GetBehaviourList().Any(x => x.Script == "execute") || card.GetID() == new LorId(WorkshopId, 8))
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
        /// <summary>
        /// Description
        /// </summary>
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

    public class DiceCardSelfAbility_bonds_protection : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Combat Start] If target is an ally, give 1 stack of The Bonds That Bind Us. If target is an enemy, spend 1 stack of The Bonds That Bind Us to redirect all target's cards to the dice with this card instead.";
        public override void OnStartBattle()
        {
            if (card.target == null)
            {
                return;
            }
            if (card.target.faction == owner.faction)
            {
                card.target.bufListDetail.AddBuf(new BattleUnitBuf_bonds());
            }
            else
            {
                var bondsBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_bonds);
                if (bondsBuff == null && card.target.IsTauntable())
                {
                    return;
                }
                bondsBuff.stack--;
                foreach (var enemyCard in card.target.cardSlotDetail.cardAry)
                {
                    enemyCard.target = owner;
                    enemyCard.targetSlotOrder = card.slotOrder;
                }
            }
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

    public class BattleUnitBuf_bonds : BattleUnitBuf
    {
        protected override string keywordId
        {
            get
            {
                return "BondsThatBindUs";
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            stack = 0;
        }
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
            var movingAction1 = new RencounterManager.MovingAction(ActionDetail.Move, CharMoveState.Stop, 0f, true, 0.5f, 1f);
            var movingAction2 = new RencounterManager.MovingAction(ActionDetail.Fire, CharMoveState.Stop);
            movingAction2.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
            movingAction2.customEffectRes = "TwistedElena_H";
            moveList.Add(movingAction1);
            moveList.Add(movingAction2);

            if (opponent.infoList.Count > 0)
            {
                opponent.infoList.Clear();
            }
            opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Stop, 0f, true, 2f, 1f));
            opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Knockback, 2f, true, 0.5f, 1f));

            return moveList;
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