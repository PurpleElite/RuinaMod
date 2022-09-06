using LOR_DiceSystem;
using LOR_BattleUnit_UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SeraphDLL
{
    public class DiceCardSelfAbility_seraph_extra_clash_dice : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Clash] Add a Slash dice (Roll: 5-8) to the dice queue";

        private const BehaviourDetail _diceType = BehaviourDetail.Slash;
        private const MotionDetail _motionType = MotionDetail.H;
        private readonly int min = 5;
        private readonly int max = 8;
        private const int diceCount = 1;
        public override void OnStartParrying()
        {
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

    public class DiceCardSelfAbility_seraph_together_to_the_end : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Combat Start] Spend 1 stack of The Bonds that Bind Us and inflict 2 Feeble and Disarm on self. Reduce max Stagger Resist by 20% (Up to 40%) [End of Scene] Revive all Incapacitated allies at half health and max stagger resist.";
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword" };

        const int maxStaggerReductionPercent = 20;
        const int maxStaggerMinimum = 40;

        public override bool OnChooseCard(BattleUnitModel owner)
        {
            var allyKOd = BattleObjectManager.instance.GetFriendlyAllList(owner.faction).Any(x => x.IsKnockout());
            var hasBonds = owner.bufListDetail.GetActivatedBufList().Any(x => x is BattleUnitBuf_seraph_bonds bondBuff && bondBuff.stack > 0);
            return allyKOd && hasBonds;
        }

        public override void OnStartBattle()
        {
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
            if (bondsBuff?.stack > 0)
            {
                owner.bufListDetail.AddBuf(new BattleUnitBuf_seraph_revive_all_round_end());
                bondsBuff.stack--;
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Weak, 2);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 2);

                BattleUnitBuf activatedBuf = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_reduce_max_bp);
                if (activatedBuf != null)
                {
                    activatedBuf.stack++;
                }
                else
                {
                    owner.bufListDetail.AddBuf(new BattleUnitBuf_seraph_reduce_max_bp());
                }
            }
        }
        class BattleUnitBuf_seraph_reduce_max_bp : BattleUnitBuf
        {
            
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    breakRate = -Math.Min(maxStaggerMinimum, stack * maxStaggerReductionPercent)
                };
            }
        }

        class BattleUnitBuf_seraph_revive_all_round_end : BattleUnitBuf
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

    public class DiceCardSelfAbility_seraph_bonds_base : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Combat Start] If target is an ally, give and gain 1 stack of The Bonds That Bind Us.";
        public override void OnStartBattle()
        {
            if (card.target == null)
            {
                return;
            }
            var bondsBuff = owner.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
            if (card.target.faction == owner.faction)
            {
                Debug.Log("bondsBuff targeting ally");
                if (bondsBuff == null)
                {
                    owner.bufListDetail.AddBuf(new BattleUnitBuf_seraph_bonds());
                }
                else
                {
                    bondsBuff.stack++;
                }
                var targetBondsBuff = card.target.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_bonds);
                if (targetBondsBuff == null || targetBondsBuff.IsDestroyed())
                {
                    card.target.bufListDetail.AddBuf(new BattleUnitBuf_seraph_bonds());
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

    public class DiceCardSelfAbility_seraph_bonds_protection : DiceCardSelfAbility_seraph_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_seraph_bonds_base.Desc + " If target is an enemy, spend 1 stack of Bonds to redirect all target's cards to the dice with this card instead.";

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

    public class DiceCardSelfAbility_seraph_bonds_drive : DiceCardSelfAbility_seraph_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_seraph_bonds_base.Desc + " If target is an enemy, spend 1 stack of Bonds to turn this into a Mass-Individual page.";
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

    public class DiceCardSelfAbility_seraph_bonds_restore : DiceCardSelfAbility_seraph_bonds_base
    {
        public static new string Desc = DiceCardSelfAbility_seraph_bonds_base.Desc + " If target is an enemy, spend 1 stack of Bonds to make all damage dealt by friendly combat pages this scene restore user's hp.";

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
                ally.bufListDetail.AddBuf(new BattleUnitBuf_seraph_bonds_restore_buff());
                //ally.cardSlotDetail.RecoverPlayPoint(1);
                //ally.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, 2);
                //var debuffs = ally.bufListDetail.GetActivatedBufList().Where(x => x.positiveType == BufPositiveType.Negative);
                //foreach (var debuff in debuffs)
                //{
                //    debuff.Destroy();
                //}
            }
        }

        public class BattleUnitBuf_seraph_bonds_restore_buff : BattleUnitBuf
        {
            public override void OnSuccessAttack(BattleDiceBehavior behavior)
            {
                _owner.RecoverHP(behavior.DiceResultDamage);
                base.OnSuccessAttack(behavior);
            }

            public override void OnRoundEnd()
            {
                Destroy();
                base.OnRoundEnd();
            }
        }
    }

    public class DiceCardSelfAbility_seraph_fastforward : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Grant three random allies 1 Haste next Scene and draw 1 page.";
        public override string[] Keywords => new string[] { "Quickness_Keyword" };

        public override void OnUseCard()
        {
            Debug.Log("fastforward OnUseCard()");
            owner.allyCardDetail.DrawCards(1);
            var team = BattleObjectManager.instance.GetAliveList(owner.faction);
            for (int i = 0; i < 3 && team.Count > 0; i++)
            {
                var ally = RandomUtil.SelectOne(team);
                team.Remove(ally);
                ally.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
            }
        }
    }

    public class DiceCardSelfAbility_seraph_eyes_on_me : DiceCardSelfAbilityBase
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

    public class DiceCardSelfAbility_seraph_willpower : DiceCardSelfAbilityBase
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

    public class DiceCardSelfAbility_seraph_staggerProtection1start : DiceCardSelfAbilityBase
    {
        public static string Desc = "[Combat Start] Gain 1 Stagger Protection";

        public override string[] Keywords => new string[] { "bstart_Keyword", "BreakProtection_Keyword" };

        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 1, owner);
        }
    }

    public class DiceCardSelfAbility_seraph_staggerProtection3start : DiceCardSelfAbilityBase
    {
        public static string Desc = "[Combat Start] Gain 3 Stagger Protection";

        public override string[] Keywords => new string[] { "bstart_Keyword", "BreakProtection_Keyword" };

        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 3, owner);
        }
    }

    public class DiceCardSelfAbility_seraph_borrowed_time_bind : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Inflict self with 2 Bind next Scene";

        public override string[] Keywords => new string[] { "Binding_Keyword" };

        public override void OnUseCard()
        {
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 2, owner);
        }
    }

    public class DiceCardSelfAbility_seraph_borrowed_time_light : DiceCardSelfAbilityBase
    {
        public static string Desc = "[On Use] Lose 3 Light next Scene";

        public override void OnUseCard()
        {
            owner.cardSlotDetail.LoseWhenStartRound(3);
        }
    }

    public class DiceCardSelfAbility_seraph_force_clash : DiceCardSelfAbilityBase
    {
        public static string Desc = "The targeted dice becomes untargetable and is forced to clash with this page";

        private BattleUnitBuf_seraph_forceClash _aggroBuf;

        private class BattleUnitBuf_seraph_OnApplyCard_called : BattleUnitBuf { public override bool Hide => true; }

        public static void RedirectToNewTarget(Faction userFaction, BattlePlayingCardDataInUnitModel cardToRedirect)
        {
            //This isn't working right
            Debug.Log("otherCard is targeting the same dice as PCT");
            var targetsOfThisScript = BattleObjectManager.instance.GetAliveList(userFaction)
                .SelectMany(x => x.cardSlotDetail.cardAry)
                .Where(x => x?.card.XmlData.Script == "seraph_force_clash")
                .Select(x => (x?.target, x.targetSlotOrder));
            var potentialNewTargets = BattleObjectManager.instance.GetAliveList(userFaction == Faction.Player ? Faction.Enemy : Faction.Player)
                .SelectMany(x => Enumerable.Range(0, x.view.speedDiceSetterUI.SpeedDicesCount).Select(y => (x, y)))
                .Except(targetsOfThisScript);
            var potentialNewSlotsOriginalTarget = potentialNewTargets.Where(x => x.Item1 == cardToRedirect.target);
            if (potentialNewSlotsOriginalTarget.Any())
            {
                cardToRedirect.targetSlotOrder = RandomUtil.SelectOne(potentialNewSlotsOriginalTarget.Select(x => x.Item2).ToArray());
            }
            else
            {
                var newTarget = RandomUtil.SelectOne(potentialNewTargets.ToArray());
                cardToRedirect.target = newTarget.Item1;
                cardToRedirect.targetSlotOrder = newTarget.Item2;
                Debug.Log("otherCard target changed to: " + cardToRedirect.target.UnitData.unitData.name);
            }
            Debug.Log("otherCard targetSlotOrder changed to: " + cardToRedirect.targetSlotOrder);
        }

        public override void OnApplyCard()
        {
            owner.bufListDetail.AddBuf(new BattleUnitBuf_seraph_OnApplyCard_called());
        }

        //Using GetCostAdder() instead of OnApplyCard() because OnApplyCard() is called before card.targetSlotOrder is set lmao
        public override int GetCostAdder(BattleUnitModel unit, BattleDiceCardModel self)
        {
            var onApplyCardCalledFlag = unit?.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_seraph_OnApplyCard_called);
            if (onApplyCardCalledFlag != null && !onApplyCardCalledFlag.IsDestroyed())
            {
                onApplyCardCalledFlag.Destroy();
                unit.bufListDetail.RemoveBuf(onApplyCardCalledFlag);
                var card = unit.cardSlotDetail.cardAry.FirstOrDefault(x => x?.card == self);
                if (card == null)
                {
                    Debug.Log("Something has gone wrong!");
                }
                Debug.Log($"PCT targeting {card.target.UnitData.unitData.name}, slot {card.targetSlotOrder}");
                var targetCardsDetail = card.target.cardSlotDetail;
                var targetCard = targetCardsDetail.cardAry[card.targetSlotOrder];
                if (targetCard != null)
                {
                    targetCard.target = unit;
                    targetCard.targetSlotOrder = card.slotOrder;
                }
            
                foreach (var otherCard in BattleObjectManager.instance.GetAliveList().SelectMany(x => x.cardSlotDetail.cardAry).Where(x => x != null && x != card))
                {
                    if (otherCard.target == card.target && otherCard.targetSlotOrder == card.targetSlotOrder)
                    {
                        RedirectToNewTarget(unit.faction, otherCard);
                    }
                }
                _aggroBuf = new BattleUnitBuf_seraph_forceClash(card);
                card.target.bufListDetail.AddBuf(_aggroBuf);
            }
            return base.GetCostAdder(unit, self);
        }

        public override void OnReleaseCard()
        {
            var aggroBuf = card.target.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_seraph_forceClash>().FirstOrDefault(x => x.AggroSource == card);
            aggroBuf?.RemoveBufsAndPassive();
            aggroBuf?.Destroy();
        }

        public override bool IsTargetChangable(BattleUnitModel attacker)
        {
            return false;
        }
        
        

        private class BattleUnitBuf_seraph_forceClash : BattleUnitBuf
        {

            public readonly BattlePlayingCardDataInUnitModel AggroSource;
            readonly List<BattleUnitBuf_seraph_dont_target_die> _redirectBuffs = new List<BattleUnitBuf_seraph_dont_target_die>();
            PassiveAbility_seraph_blockSpecificDice _blockTargetedDiePassive;
            PassiveAbility_seraph_blockSpecificDice _limitTargetsPassive;
            private BattleDiceCardModel _cardToRetarget;

            public override bool Hide => true;
            public BattleUnitBuf_seraph_forceClash(BattlePlayingCardDataInUnitModel aggro)
            {
                AggroSource = aggro;
            }

            public override void Init(BattleUnitModel owner)
            {
                foreach (var unit in BattleObjectManager.instance.GetAliveList())
                {
                    var _aggroSourceSlot = unit == AggroSource.owner ? AggroSource.slotOrder : -1;
                    var buf = new BattleUnitBuf_seraph_dont_target_die(unit, AggroSource.target, AggroSource.targetSlotOrder, _aggroSourceSlot);
                    _redirectBuffs.Add(buf);
                    unit.bufListDetail.AddBuf(buf);
                }

                var speedDicesField = typeof(SpeedDiceSetter).GetField("_speedDices", BindingFlags.NonPublic | BindingFlags.Instance);
                
                _limitTargetsPassive = new PassiveAbility_seraph_blockSpecificDice(AggroSource.owner, AggroSource.target, AggroSource.targetSlotOrder);
                var aggroSourceDice = (List<SpeedDiceUI>)speedDicesField.GetValue(AggroSource.owner.view.speedDiceSetterUI);
                _limitTargetsPassive.SetDice(aggroSourceDice.Where(x => x.OrderOfDice != AggroSource.slotOrder), new[] { aggroSourceDice.FirstOrDefault(x => x.OrderOfDice == AggroSource.slotOrder) });
                AggroSource.owner.passiveDetail.AddPassive(_limitTargetsPassive);
                AggroSource.owner.passiveDetail.OnCreated();

                _blockTargetedDiePassive = new PassiveAbility_seraph_blockSpecificDice(AggroSource.target);
                var aggroTargetDice = (List<SpeedDiceUI>)speedDicesField.GetValue(AggroSource.target.view.speedDiceSetterUI);
                _blockTargetedDiePassive.SetDice(new[] { aggroTargetDice.FirstOrDefault(x => x.OrderOfDice == AggroSource.targetSlotOrder) }, aggroTargetDice.Where(x => x.OrderOfDice != AggroSource.targetSlotOrder));
                AggroSource.target.passiveDetail.AddPassive(_blockTargetedDiePassive);
                AggroSource.target.passiveDetail.OnCreated();

                AggroSource.target.view.speedDiceSetterUI.GetSpeedDiceByIndex(AggroSource.targetSlotOrder).BlockDice(true, true);
            }

            public override List<BattleUnitModel> GetFixedTarget()
            {
                AggroSource.target.view.speedDiceSetterUI.GetSpeedDiceByIndex(AggroSource.targetSlotOrder).BlockDice(true, true);

                var selectedDieIndex = BattleManagerUI.Instance.selectedAllyDice?.OrderOfDice;
                if (selectedDieIndex == AggroSource.targetSlotOrder)
                {
                    return new List<BattleUnitModel> { AggroSource.owner };
                }
                return base.GetFixedTarget();
            }

            public override bool DirectAttack()
            {
                AggroSource.target.view.speedDiceSetterUI.GetSpeedDiceByIndex(AggroSource.targetSlotOrder).BlockDice(true, true);
                return base.DirectAttack();
            }

            public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int currentSlot)
            {
                if (currentSlot == AggroSource.targetSlotOrder)
                {
                    return AggroSource.owner;
                }
                return base.ChangeAttackTarget(card, currentSlot);
            }

            public override int ChangeTargetSlot(BattleDiceCardModel card, BattleUnitModel target, int currentSlot, int targetSlot, bool teamkill)
            {
                if (currentSlot == AggroSource.targetSlotOrder)
                {
                    if (target != AggroSource.owner)
                    {
                        _cardToRetarget = card;
                    }
                    return AggroSource.slotOrder;
                }
                return base.ChangeTargetSlot(card, target, currentSlot, targetSlot, teamkill);
            }

            public override int GetCardCostAdder(BattleDiceCardModel card)
            {
                //Hijack here to ensure all blocked dice stay visually blocked
                var selectedDieIndex = BattleManagerUI.Instance.selectedAllyDice?.OrderOfDice;
                if (selectedDieIndex == AggroSource.targetSlotOrder)
                {
                    var enemies = BattleObjectManager.instance.GetAliveList((card.owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy);
                    foreach (var enemy in enemies)
                    {
                        for (int i = 0; i < enemy.cardSlotDetail.cardAry.Count(); i++)
                        {
                            if (enemy.cardSlotDetail.cardAry[i] != AggroSource)
                                enemy.view.speedDiceSetterUI.GetSpeedDiceByIndex(i).BlockDice(true, true);
                        }
                    }
                }
                //Hijack here to change target of card after it's been set to its final value
                if (_cardToRetarget == card)
                {
                    var thisCard = AggroSource.target.cardSlotDetail.cardAry.FirstOrDefault(x => x.card == card);
                    thisCard.target = AggroSource.owner;
                    thisCard.earlyTarget = AggroSource.owner;
                    _cardToRetarget = null;
                }
                return base.GetCardCostAdder(card);
            }

            public override void OnRoundEnd()
            {
                RemoveBufsAndPassive();
                Destroy();
            }

            public void RemoveBufsAndPassive()
            {
                foreach (var buf in _redirectBuffs)
                {
                    buf?.Destroy();
                }
                _redirectBuffs.Clear();
                if (_limitTargetsPassive != null)
                {
                    _limitTargetsPassive.destroyed = true;
                    _limitTargetsPassive.Owner.passiveDetail.RemovePassive();
                }
                if (_blockTargetedDiePassive != null)
                {
                    _blockTargetedDiePassive.destroyed = true;
                    _blockTargetedDiePassive.Owner.passiveDetail.RemovePassive();
                }
            }

            private class BattleUnitBuf_seraph_dont_target_die : BattleUnitBuf
            {
                public override bool Hide => true;

                new readonly BattleUnitModel _owner;
                readonly BattleUnitModel _target;
                readonly int _targetSlot;
                readonly int _originSlot;
                private BattleDiceCardModel _cardToRetarget;

                public BattleUnitBuf_seraph_dont_target_die(BattleUnitModel owner, BattleUnitModel target, int targetSlot, int originSlot = -1)
                {
                    _owner = owner;
                    _target = target;
                    _targetSlot = targetSlot;
                    _originSlot = originSlot;
                }

                public override int ChangeTargetSlot(BattleDiceCardModel card, BattleUnitModel target, int currentSlot, int targetSlot, bool teamkill)
                {
                    if (targetSlot == _targetSlot && target == _target && currentSlot != _originSlot)
                    {
                        _cardToRetarget = card;
                    }
                    return base.ChangeTargetSlot(card, target, currentSlot, targetSlot, teamkill);
                }

                public override int GetCardCostAdder(BattleDiceCardModel card)
                {
                    //Hijack here to change target of card after it's been set to its final value
                    if (_cardToRetarget == card)
                    {
                        RedirectToNewTarget(_owner.faction, _owner.cardSlotDetail.cardAry.FirstOrDefault(x => x.card == card));
                        _cardToRetarget = null;
                    }
                    return base.GetCardCostAdder(card);
                }
            }

            private class PassiveAbility_seraph_blockSpecificDice : PassiveAbilityBase
            {
                readonly List<(SpeedDiceUI Die, int OriginalIndex)> _blockedDiceTuples;
                readonly List<(SpeedDiceUI Die, int OriginalIndex)> _targetableDiceTuples;
                readonly BattleUnitModel _diceOwner;
                readonly BattleUnitModel _specificEnemyToBlock;
                readonly int _specificEnemySlotToBlock;
                readonly FieldInfo _indexField = typeof(SpeedDiceUI).GetField("_speedDiceIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                readonly FieldInfo _isHighlightedField = typeof(SpeedDiceUI).GetField("isHighlighted", BindingFlags.NonPublic | BindingFlags.Instance);
                bool _doTheLast = false;

                public override string debugDesc => "A hacky solution to make it so CheckBlockDice() will return true for all dice except the one with the aggro card slotted";

                public PassiveAbility_seraph_blockSpecificDice(BattleUnitModel diceOwner, BattleUnitModel specificEnemyToBlock = null, int specificEnemySlotToBlock = -1)
                {
                    _diceOwner = diceOwner;
                    _specificEnemyToBlock = specificEnemyToBlock;
                    _specificEnemySlotToBlock = specificEnemySlotToBlock;
                    _blockedDiceTuples = new List<(SpeedDiceUI Die, int OriginalIndex)>();
                    _targetableDiceTuples = new List<(SpeedDiceUI Die, int OriginalIndex)>();
                }

                public void SetDice(IEnumerable<SpeedDiceUI> blockedDice, IEnumerable<SpeedDiceUI> targetableDice)
                {
                    _blockedDiceTuples.Clear();
                    _blockedDiceTuples.AddRange(blockedDice.Select(x => (x, (int)_indexField.GetValue(x))));
                    _targetableDiceTuples.Clear();
                    _targetableDiceTuples.AddRange(targetableDice.Select(x => (x, (int)_indexField.GetValue(x))));
                }

                public override bool IsTargetable(BattleUnitModel attacker)
                {
                    if ((_specificEnemyToBlock == null || SingletonBehavior<BattleManagerUI>.Instance.ui_unitCardsInHand.SelectedModel == _specificEnemyToBlock)
                        && ((_specificEnemySlotToBlock == -1) || BattleManagerUI.Instance.selectedAllyDice?.OrderOfDice == _specificEnemySlotToBlock))
                    {
                        if (_diceOwner.view.speedDiceSetterUI.SpeedDicesCount > 1)
                        {
                            foreach (var (die, _) in _blockedDiceTuples)
                            {
                                die.BlockDice(true, true);
                            }
                            if (new System.Diagnostics.StackTrace().GetFrames().Any(x => x?.GetMethod().Name == "CheckBlockDice"))
                            {
                                //Force all the dice to trigger IsTargetable_theLast by temporarily setting their indices to the last one
                                foreach (var (die, _) in _blockedDiceTuples.Concat(_targetableDiceTuples))
                                {
                                    _indexField.SetValue(die, owner.view.speedDiceSetterUI.SpeedDicesCount - 1);
                                }
                                _doTheLast = true;
                            }
                        }
                        else
                        {
                            return false;
                        }    
                    }
                    return base.IsTargetable(attacker);
                }

                public override bool IsTargetable_theLast()
                {
                    if (_doTheLast)
                    {
                        //Set the indices back to their original values 
                        foreach (var (die, originalIndex) in _blockedDiceTuples.Concat(_targetableDiceTuples))
                        {
                            _indexField.SetValue(die, originalIndex);
                        }
                        _doTheLast = false;
                        bool selectionIsTargetable;
                        if (UI.UIControlManager.isControllerInput)
                        {
                            selectionIsTargetable = _targetableDiceTuples.Select(x => x.Die).Contains(SingletonBehavior<BattleManagerUI>.Instance.focusedDice);
                        }
                        else
                        {
                            selectionIsTargetable = !_blockedDiceTuples.Any(x => (bool)_isHighlightedField.GetValue(x.Die));
                        }
                        return selectionIsTargetable;
                    }
                    return base.IsTargetable_theLast();
                }

                public override void OnRoundEnd_before()
                {
                    destroyed = true;
                }
            }
        }
    }
}
