using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using LOR_DiceSystem;
using LOR_XML;
using Sound;
using UnityEngine;

// Token: 0x020002AC RID: 684
public class FarAreaeffect_LinusArea : FarAreaEffect
{
	// Token: 0x17000150 RID: 336
	// (get) Token: 0x06001179 RID: 4473 RVA: 0x000894B0 File Offset: 0x000876B0
	public override bool HasIndependentAction
	{
		get
		{
			return true;
		}
	}

	// Token: 0x0600117A RID: 4474 RVA: 0x0008DE90 File Offset: 0x0008C090
	public override void Init(BattleUnitModel self, params object[] args)
	{
		base.Init(self, args);
		_victimList = new List<BattleFarAreaPlayManager.VictimInfo>();
		_elapsedEndAtk = 0f;
		_elapsedAtkOneTarget = 0f;
		OnEffectStart();
		_trailObject = Util.LoadPrefab("Battle/SpecialEffect/ArgaliaSpecialAreaEffect", transform);
		_trailObject.transform.localPosition = Vector3.zero;
		_self.view.charAppearance.ChangeMotion(ActionDetail.Default);
		List<BattleUnitModel> list = new List<BattleUnitModel>();
		list.AddRange(BattleObjectManager.instance.GetAliveList((self.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy));
		SingletonBehavior<BattleCamManager>.Instance.FollowUnits(false, list);
		_sign = ((UnityEngine.Random.Range(0f, 1f) > 0.5f) ? 1 : -1);
		_dstPosAtkOneTarget = Vector3.zero;
		_srcPosAtkOneTarget = Vector3.zero;
	}

	// Token: 0x0600117B RID: 4475 RVA: 0x0008DF74 File Offset: 0x0008C174
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
				if (ranged == CardRange.FarArea)
				{
					Util.DebugEditorLog("?");
				}
				else if (ranged == CardRange.FarAreaEach)
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
							attacker.view.charAppearance.ChangeMotion((_motionCount == 0) ? ActionDetail.Slash : ActionDetail.S2);
							//Alternate which side of the target the attacker ends up on
							_sign = ((_sign == 1) ? -1 : 1);
							Vector3 positionOffset = new Vector3(SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 4f * _self.view.transform.localScale.x / 1.5f, 0f, 0f) * _sign;
							_srcPosAtkOneTarget = targetInfo.unitModel.view.WorldPosition + positionOffset;
							_dstPosAtkOneTarget = targetInfo.unitModel.view.WorldPosition - positionOffset;
							attacker.view.WorldPosition = _srcPosAtkOneTarget;
							attacker.UpdateDirection(targetInfo.unitModel.view.WorldPosition);
							string resource = "WarpCrew_J";//(_motionCount == 0) ? "FX_Mon_Argalia_Slash_Up" : "FX_Mon_Argalia_Slash_Down_Small";
							DiceAttackEffect diceAttackEffect = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource, 1f, attacker.view, targetInfo.unitModel.view, 1f);
							if (diceAttackEffect != null)
							{
								diceAttackEffect.SetLayer("Effect");
							}
							BattlePlayingCardDataInUnitModel playingCard = targetInfo.playingCard;
							if ((playingCard?.currentBehavior) != null)
							{
								if (attacker.currentDiceAction.currentBehavior.DiceResultValue > targetInfo.playingCard.currentBehavior.DiceResultValue)
								{
									attacker.currentDiceAction.currentBehavior.GiveDamage(targetInfo.unitModel);
									if (_motionCount == 0)
									{
										SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Blue_Argalria_Far_Atk1", false, 1f, null);
									}
									else
									{
										SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Blue_Argalria_Far_Atk2", false, 1f, null);
									}
									if (targetInfo.unitModel.IsDead())
									{
										List<BattleUnitModel> list2 = new List<BattleUnitModel>();
										list2.Add(_self);
										targetInfo.unitModel.view.DisplayDlg(DialogType.DEATH, list2);
									}
									targetInfo.unitModel.view.charAppearance.ChangeMotion(ActionDetail.Damaged);
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
								attacker.currentDiceAction.currentBehavior.GiveDamage(targetInfo.unitModel);
								if (FarAreaeffect_LinusArea._motionCount == 0)
								{
									SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Blue_Argalria_Far_Atk1", false, 1f, null);
								}
								else
								{
									SingletonBehavior<SoundEffectManager>.Instance.PlayClip("Battle/Blue_Argalria_Far_Atk2", false, 1f, null);
								}
								if (targetInfo.unitModel.IsDead())
								{
									List<BattleUnitModel> list3 = new List<BattleUnitModel>();
									list3.Add(_self);
									targetInfo.unitModel.view.DisplayDlg(DialogType.DEATH, list3);
								}
								targetInfo.unitModel.view.charAppearance.ChangeMotion(ActionDetail.Damaged);
							}
							SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(targetInfo.unitModel, targetInfo.unitModel.faction, targetInfo.unitModel.hp, targetInfo.unitModel.breakDetail.breakGauge, null);
							SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(attacker, attacker.faction, attacker.hp, attacker.breakDetail.breakGauge, null);
							_victimList.Remove(targetInfo);
						}
					}
				}
			}
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
					state = FarAreaEffect.EffectState.End;
				}
				else if (!_victimList.Exists((BattleFarAreaPlayManager.VictimInfo x) => !x.unitModel.IsDead()))
				{
					_victimList.Clear();
					state = FarAreaEffect.EffectState.End;
				}
			}
		}
		else if (state == FarAreaEffect.EffectState.End)
		{
			_elapsedEndAtk += deltaTime;
			if (_elapsedEndAtk > 0.35f)
			{
				_self.view.charAppearance.ChangeMotion(ActionDetail.Default);
				state = FarAreaEffect.EffectState.None;
				_elapsedEndAtk = 0f;
			}
		}
		else if (_self.moveDetail.isArrived)
		{
			SingletonBehavior<BattleCamManager>.Instance.FollowUnits(false, BattleObjectManager.instance.GetAliveList(false));
			result = true;
			if (_trailObject != null)
			{
				UnityEngine.Object.Destroy(_trailObject);
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}
		return result;
	}

	// Token: 0x0600117C RID: 4476 RVA: 0x0008D210 File Offset: 0x0008B410
	protected override void Update()
	{
		if (isRunning && _self.moveDetail.isArrived)
		{
			isRunning = false;
		}
	}

	// Token: 0x0600117D RID: 4477 RVA: 0x000021A4 File Offset: 0x000003A4
	private void OnDestroy()
	{
	}

	// Token: 0x0400175F RID: 5983
	private const string _TRAIL_PREFAB_PATH = "Battle/SpecialEffect/ArgaliaSpecialAreaEffect";

	// Token: 0x04001760 RID: 5984
	private static int _motionCount;

	// Token: 0x04001761 RID: 5985
	private List<BattleFarAreaPlayManager.VictimInfo> _victimList;

	// Token: 0x04001762 RID: 5986
	private float _elapsedEndAtk;

	// Token: 0x04001763 RID: 5987
	private float _elapsedAtkOneTarget;

	// Token: 0x04001764 RID: 5988
	private GameObject _trailObject;

	// Token: 0x04001765 RID: 5989
	private int _sign;

	// Token: 0x04001766 RID: 5990
	private Vector3 _srcPosAtkOneTarget;

	// Token: 0x04001767 RID: 5991
	private Vector3 _dstPosAtkOneTarget;
}
