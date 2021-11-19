
using LOR_DiceSystem;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace Passives
{
    internal static class ModData
    {
        internal const string WorkshopId = "SeraphOffice";
        internal static string Language = "en";
        internal static DirectoryInfo AssembliesPath;
        internal static Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    }

    public class ModInitializer_seraph : ModInitializer
    {
        public override void OnInitializeMod()
        {
            ModData.AssembliesPath = new DirectoryInfo(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)));
            GetSprites(new DirectoryInfo((ModData.AssembliesPath?.ToString()) + "/Sprites"));
            AddEffectText();
        }

        private static void GetSprites(DirectoryInfo parentDir)
        {
            if (parentDir.GetDirectories().Length != 0)
            {
                DirectoryInfo[] childDirs = parentDir.GetDirectories();
                for (int i = 0; i < childDirs.Length; i++)
                {
                    GetSprites(childDirs[i]);
                }
            }
            foreach (FileInfo fileInfo in parentDir.GetFiles())
            {
                Texture2D texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
                Sprite value = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0f, 0f));
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                ModData.Sprites[fileNameWithoutExtension] = value;
            }
        }

        private static void AddEffectText()
        {
            Dictionary<string, BattleEffectText> dictionary = 
                typeof(BattleEffectTextsXmlList).GetField("_dictionary", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(Singleton<BattleEffectTextsXmlList>.Instance) as Dictionary<string, BattleEffectText>;
            FileInfo[] files = new DirectoryInfo((ModData.AssembliesPath?.ToString()) + "/Localize/" + ModData.Language + "/EffectTexts").GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                using (StringReader stringReader = new StringReader(File.ReadAllText(files[i].FullName)))
                {
                    BattleEffectTextRoot battleEffectTextRoot = (BattleEffectTextRoot)new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(stringReader);
                    for (int j = 0; j < battleEffectTextRoot.effectTextList.Count; j++)
                    {
                        BattleEffectText battleEffectText = battleEffectTextRoot.effectTextList[j];
                        dictionary.Add(battleEffectText.ID, battleEffectText);
                    }
                }
            }
        }
    }

    public class DiceCardAbility_multi : DiceCardAbilityBase
    {
        public static string Desc = "This die is rolled up to 4 times at the cost of one light per extra roll.";

        private int rerollCount = 0;
        public override void AfterAction()
        {
            if (rerollCount < 3 && owner.cardSlotDetail.PlayPoint > 0)
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
        
        protected override void TargetEnemy(BattleUnitBuf bondsBuff)
        {
            if (bondsBuff == null || bondsBuff.stack < 1)
            {
                Debug.Log("Cannot use special, not enough stacks");
                return;
            }
            Debug.Log("Using mass attack");
            //TODO: give the mass attack a proper animation
            bondsBuff.stack--;
            var cardSpec = card.card.GetSpec();
            cardSpec.Ranged = CardRange.FarAreaEach;
            cardSpec.affection = CardAffection.Team;
            var behaviorList = card.GetDiceBehaviorList();
            foreach (var dice in behaviorList)
            {
                dice.behaviourInCard.Type = BehaviourType.Atk;
                dice.behaviourInCard.ActionScript = "linus_area";
            }
            var targetList = BattleObjectManager.instance.GetAliveList((card.owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy);
            targetList.Remove(card.target);
            card.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
            foreach (BattleUnitModel battleUnitModel in targetList)
            {
                if (battleUnitModel.IsTargetable(card.owner))
                {
                    BattlePlayingCardSlotDetail cardSlotDetail = battleUnitModel.cardSlotDetail;
                    bool flag;
                    if (cardSlotDetail == null)
                    {
                        flag = false;
                    }
                    else
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

        public override void OnEndBattle()
        {
            var cardSpec = card.card.GetSpec();
            cardSpec.Ranged = CardRange.Near;
            cardSpec.affection = CardAffection.One;
            var behaviorList = card.GetDiceBehaviorList();
            foreach (var dice in behaviorList)
            {
                dice.behaviourInCard.Type = BehaviourType.Standby;
                dice.behaviourInCard.ActionScript = string.Empty;
            }
        }
    }

    public class BattleUnitBuf_bonds : BattleUnitBuf
    {
        private const string buffName = "BindBonds";
        protected override string keywordId
        {
            get
            {
                return buffName;
            }
        }

        public override BufPositiveType positiveType
        {
            get
            {
                return BufPositiveType.Positive;
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            typeof(BattleUnitBuf).GetField("_bufIcon", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, ModData.Sprites[buffName]);
            typeof(BattleUnitBuf).GetField("_iconInit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, true);
        }

        public override void OnRoundEnd()
        {
            if (stack <= 0)
            {
                Destroy();
            }
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