﻿using System;
using System.Collections.Generic;
using System.Linq;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using LOR_XML;
using Sound;
using UnityEngine;

namespace SeraphDLL
{
	public class BehaviourAction_seraph_johannaexecute : BehaviourActionBase
	{
		public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			var target = self.behaviourResultData.playingCard.target;
			if (self.result == Result.Win && (target.IsBreakLifeZero() || target.breakDetail.breakGauge == 0))
			{
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
			else
            {
				return base.GetMovingAction(ref self, ref opponent);
			}
		}
	}

	public class BehaviourAction_seraph_borrowed_time : BehaviourActionBase
	{
		public override bool IsMovable()
		{
			return _movable;
		}
		private bool _movable = true;
		private int _count = 0;
		private ActionDetail previousAction;
		private static readonly (ActionDetail, string)[] _possibleActions = new[] { 
			(ActionDetail.Slash, "WarpCrew_J"), 
			(ActionDetail.Penetrate, "WarpCrew_Z"), 
			(ActionDetail.Hit, "WarpCrew_H"),
			(ActionDetail.Move, "WarpCrew_J") };
		private const int totalCount = 3;

		public override List<RencounterManager.MovingAction> GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
		{
			var moveActions = new List<RencounterManager.MovingAction>();
			if (self.result == Result.Win && self.data.actionType == ActionType.Atk && !opponent.behaviourResultData.IsFarAtk())
			{
				var finalAttack = _count == totalCount;
				_count++;
				(ActionDetail nextAction, string nextEffectRes) = NextAction();
				var movingAction = new RencounterManager.MovingAction(nextAction, _count == 0 ? CharMoveState.MoveOpponent : CharMoveState.Stop, 0f, false, 0.2f);
				movingAction.SetEffectTiming(EffectTiming.PRE, EffectTiming.NONE, EffectTiming.NONE);
				movingAction.customEffectRes = nextEffectRes;
				moveActions.Add(movingAction);
				(var nextAction2, var nextEffectRes2) = NextAction();
				var movingAction2 = new RencounterManager.MovingAction(nextAction2, CharMoveState.Stop, 3f, false, finalAttack ? 1f : 0.2f);
				movingAction2.SetEffectTiming(EffectTiming.PRE, EffectTiming.PRE, EffectTiming.PRE);
				movingAction2.customEffectRes = nextEffectRes2;
				moveActions.Add(movingAction2);
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, CharMoveState.Stop, 0f, false, 0.2f));
				opponent.infoList.Add(new RencounterManager.MovingAction(ActionDetail.Damaged, finalAttack ? CharMoveState.Knockback : CharMoveState.Stop, 3f, false, finalAttack ? 1f : 0.2f));
				_movable = finalAttack;
			}
			else
			{
				moveActions = base.GetMovingAction(ref self, ref opponent);
				_movable = true;
			}
			return moveActions;
		}

		private (ActionDetail, string) NextAction()
        {
			var nextAction = RandomUtil.SelectOne(_possibleActions.Where(x => x.Item1 != previousAction).ToArray());
			previousAction = nextAction.Item1;
			return nextAction;
        }
	}

	public class BehaviourAction_seraph_linus_area : BehaviourActionBase
	{
		public override FarAreaEffect SetFarAreaAtkEffect(BattleUnitModel self)
		{
			_self = self;
			FarAreaeffect_seraph_LinusArea farAreaeffect_LinusArea = new GameObject().AddComponent<FarAreaeffect_seraph_LinusArea>();
			farAreaeffect_LinusArea.Init(self, Array.Empty<object>());
			return farAreaeffect_LinusArea;
		}
	}

	public class FarAreaeffect_seraph_LinusArea : FarAreaEffect
	{
		public override bool HasIndependentAction
		{
			get
			{
				return true;
			}
		}

		public override void Init(BattleUnitModel self, params object[] args)
		{
			base.Init(self, args);
			_victimList = new List<BattleFarAreaPlayManager.VictimInfo>();
			_elapsedEndAtk = 0f;
			_elapsedAtkOneTarget = 0f;
			OnEffectStart();
			_trailObject = Util.LoadPrefab(_TRAIL_PREFAB_PATH, transform);
			_trailObject.transform.localPosition = Vector3.zero;
			_self.view.charAppearance.ChangeMotion(ActionDetail.Default);
			List<BattleUnitModel> list = new List<BattleUnitModel>();
			list.AddRange(BattleObjectManager.instance.GetAliveList((self.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy));
			SingletonBehavior<BattleCamManager>.Instance.FollowUnits(false, list);
			_sign = ((UnityEngine.Random.Range(0f, 1f) > 0.5f) ? 1 : -1);
			_dstPosAtkOneTarget = Vector3.zero;
			_srcPosAtkOneTarget = Vector3.zero;
		}

		public override bool ActionPhase(float deltaTime, BattleUnitModel attacker, List<BattleFarAreaPlayManager.VictimInfo> victims, ref List<BattleFarAreaPlayManager.VictimInfo> defenseVictims)
		{
			bool result = false;
			if (_trailObject != null)
			{
				transform.position = _self.view.atkEffectRoot.position;
			}
			if (state == EffectState.Start)
			{
				if (_self.moveDetail.isArrived)
				{
					state = EffectState.GiveDamage;
					_victimList = new List<BattleFarAreaPlayManager.VictimInfo>(victims);
				}
			}
			else if (state == EffectState.GiveDamage)
			{
				if (_elapsedAtkOneTarget < Mathf.Epsilon)
				{
					CardRange ranged = attacker.currentDiceAction.card.GetSpec().Ranged;
					if (ranged == CardRange.FarAreaEach)
					{
						List<BattleFarAreaPlayManager.VictimInfo> victimList = _victimList;
						if (victimList != null && victimList.Count > 0)
						{
							if (_victimList.Exists((BattleFarAreaPlayManager.VictimInfo x) => !x.unitModel.IsDead()))
							{
								List<BattleFarAreaPlayManager.VictimInfo> list = new List<BattleFarAreaPlayManager.VictimInfo>();
								foreach (BattleFarAreaPlayManager.VictimInfo victimInfo in _victimList)
								{
									if (!victimInfo.unitModel.IsDead())
									{
										list.Add(victimInfo);
									}
								}
								//Get target
								BattleFarAreaPlayManager.VictimInfo targetInfo = list[UnityEngine.Random.Range(0, list.Count)];
								attacker.view.WorldPosition = targetInfo.unitModel.view.WorldPosition;
								//Alternate slashing motion
								_motionCount = (_motionCount + 1) % 2;
								attacker.view.charAppearance.ChangeMotion((_motionCount == 0) ? ActionDetail.Hit : ActionDetail.Penetrate);
								//Alternate which side of the target the attacker ends up on
								_sign = ((_sign == 1) ? -1 : 1);
								Vector3 positionOffset = new Vector3(SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 4f * _self.view.transform.localScale.x / 1.5f, 0f, 0f) * _sign;
								_srcPosAtkOneTarget = targetInfo.unitModel.view.WorldPosition + positionOffset;
								_dstPosAtkOneTarget = targetInfo.unitModel.view.WorldPosition - positionOffset;
								attacker.view.WorldPosition = _srcPosAtkOneTarget;
								attacker.UpdateDirection(targetInfo.unitModel.view.WorldPosition);
								string resource = (_motionCount == 0) ? "WarpCrew_H" : "WarpCrew_Z";
								DiceAttackEffect diceAttackEffect = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource, 1f, attacker.view, targetInfo.unitModel.view, 1f);
								if (diceAttackEffect != null)
								{
									diceAttackEffect.SetLayer("Effect");
								}
								void ApplyDamagePlaySoundAndCheckKill()
								{
									attacker.currentDiceAction.currentBehavior.GiveDamage(targetInfo.unitModel);
									if (_motionCount == 0)
									{
										SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Dawn_Yuna_Hori", false, 1f, null);
									}
									else
									{
										SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Warp_Mass_Hori", false, 1f, null);
									}
									if (targetInfo.unitModel.IsDead())
									{
										targetInfo.unitModel.view.DisplayDlg(DialogType.DEATH, new List<BattleUnitModel> { _self });
									}
									targetInfo.unitModel.view.charAppearance.ChangeMotion(ActionDetail.Damaged);
								}
								BattlePlayingCardDataInUnitModel playingCard = targetInfo.playingCard;
								if ((playingCard?.currentBehavior) != null)
								{
									if (attacker.currentDiceAction.currentBehavior.DiceResultValue > targetInfo.playingCard.currentBehavior.DiceResultValue)
									{
										ApplyDamagePlaySoundAndCheckKill();
										targetInfo.destroyedDicesIndex.Add(targetInfo.playingCard.currentBehavior.Index);
									}
									else
									{
										targetInfo.unitModel.view.charAppearance.ChangeMotion(ActionDetail.Guard);
										targetInfo.unitModel.UpdateDirection(attacker.view.WorldPosition);
										if (!defenseVictims.Contains(targetInfo))
										{
											defenseVictims.Add(targetInfo);
										}
									}
								}
								else
								{
									ApplyDamagePlaySoundAndCheckKill();
								}
								SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(targetInfo.unitModel, targetInfo.unitModel.faction, targetInfo.unitModel.hp, targetInfo.unitModel.breakDetail.breakGauge, null);
								SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(attacker, attacker.faction, attacker.hp, attacker.breakDetail.breakGauge, null);
								_victimList.Remove(targetInfo);
							}
						}
					}
				}
				//Interpolate motion towards target, reaches destination when _elapsedAtkOneTarget = 0.1f
				_elapsedAtkOneTarget += deltaTime;
				if (Vector3.SqrMagnitude(_dstPosAtkOneTarget - _srcPosAtkOneTarget) > Mathf.Epsilon)
				{
					attacker.view.WorldPosition = Vector3.Lerp(_srcPosAtkOneTarget, _dstPosAtkOneTarget, _elapsedAtkOneTarget * 10f);
				}
				if (_elapsedAtkOneTarget > 0.25f)
				{
					_elapsedAtkOneTarget = 0f;
					_srcPosAtkOneTarget = Vector3.zero;
					_dstPosAtkOneTarget = Vector3.zero;
					if (_victimList == null || _victimList.Count == 0)
					{
						state = EffectState.End;
					}
					else if (!_victimList.Exists((BattleFarAreaPlayManager.VictimInfo x) => !x.unitModel.IsDead()))
					{
						_victimList.Clear();
						state = EffectState.End;
					}
				}
			}
			else if (state == EffectState.End)
			{
				_elapsedEndAtk += deltaTime;
				if (_elapsedEndAtk > 0.35f)
				{
					_self.view.charAppearance.ChangeMotion(ActionDetail.Default);
					state = EffectState.None;
					_elapsedEndAtk = 0f;
				}
			}
			else if (_self.moveDetail.isArrived)
			{
				SingletonBehavior<BattleCamManager>.Instance.FollowUnits(false, BattleObjectManager.instance.GetAliveList(false));
				result = true;
				if (_trailObject != null)
				{
					Destroy(_trailObject);
				}
				Destroy(gameObject);
			}
			return result;
		}

		protected override void Update()
		{
			if (isRunning && _self.moveDetail.isArrived)
			{
				isRunning = false;
			}
		}

		private const string _TRAIL_PREFAB_PATH = "Battle/SpecialEffect/ArgaliaSpecialAreaEffect";

		private static int _motionCount;

		private List<BattleFarAreaPlayManager.VictimInfo> _victimList;

		private float _elapsedEndAtk;

		private float _elapsedAtkOneTarget;

		private GameObject _trailObject;

		private int _sign;

		private Vector3 _srcPosAtkOneTarget;

		private Vector3 _dstPosAtkOneTarget;
	}
}
