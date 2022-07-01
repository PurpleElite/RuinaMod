using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomDLLs
{
    public class BattleUnitBuf_seraph_bonds : BattleUnitBuf
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

        public override void OnBreakState()
        {
            stack--;
        }
    }

    public class BattleUnitBuf_unstable_entropy : BattleUnitBuf
    {
        private const string buffName = "UnstableEntropy";
        private const int overflowValue = 3;
        //private PassiveAbilityBase addedPassive;

        public static int Duration = 3;
        public override BufPositiveType positiveType => BufPositiveType.Negative;

        protected override string keywordId
        {
            get
            {
                return buffName;
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            typeof(BattleUnitBuf).GetField("_bufIcon", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, ModData.Sprites[buffName]);
            typeof(BattleUnitBuf).GetField("_iconInit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, true);
            //if (addedPassive == null)
            //{
            //    addedPassive = _owner.passiveDetail.AddPassive(new PassiveAbility_unstable_entropy());
            //    _owner.passiveDetail.OnCreated();
            //}
            stack = Duration;
        }

        public override void OnRoundStart()
        {
            var erosionBuff = _owner.bufListDetail.GetKewordBufStack(KeywordBuf.Decay);
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, erosionBuff);
            var overflow = Math.Max(erosionBuff - 3, 0);
            if (overflow > 0)
            {
                Debug.Log("Unstable Entropy triggered");
                var allies = BattleObjectManager.instance.GetAliveList(_owner.faction);
                allies.Remove(_owner);
                foreach (var ally in allies)
                {
                    ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Decay, overflow);
                    if (ally.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay decay)
                    {
                        decay.ChangeToYanDecay();
                    }
                }
            }
            base.OnRoundStart();
        }

        public override void OnAddBuf(int addedStack)
        {
            Debug.Log("entropy OnAddBuf()");
            //if (addedPassive == null)
            //{
            //    addedPassive = _owner.passiveDetail.AddPassive(new PassiveAbility_unstable_entropy());
            //    _owner.passiveDetail.OnCreated();
            //}
            stack = Duration;
        }

        public override void OnRoundEnd()
        {
            Debug.Log("entropy OnRoundEnd()");
            stack--;
            if (stack <= 0)
            {
                //Debug.Log("entropy Destroy Passive");
                //_owner.passiveDetail.DestroyPassive(addedPassive);
                //_owner.passiveDetail.RemovePassive();
                Destroy();
            }
        }

        //private class PassiveAbility_unstable_entropy : PassiveAbilityBase
        //{
        //    public override bool isHide => true;
        //    public override int OnAddKeywordBufByCard(BattleUnitBuf buf, int stack)
        //    {
        //        Debug.Log("entropy OnAddKeywordBufByCard()");
        //        if (buf.stack >= overflowValue)
        //        {
        //            var allies = BattleObjectManager.instance.GetAliveList(owner.faction);
        //            allies.Remove(owner);
        //            foreach (var ally in allies)
        //            {
        //                ally.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Decay, 1);
        //            }
        //            var thisBuff = owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_unstable_entropy);
        //            owner.bufListDetail.RemoveBuf(thisBuff);
        //            thisBuff.Destroy();
        //            destroyed = true;
        //            owner.passiveDetail.RemovePassive();
        //        }
        //        return base.OnAddKeywordBufByCard(buf, stack);
        //    }
        //}
    }
}
