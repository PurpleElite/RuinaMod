using System;
using System.Reflection;
using UnityEngine;

namespace SeraphDLL
{
    public class BattleUnitBuf_seraph_bonds : BattleUnitBuf
    {
        private const string buffName = "SeraphRunaways_BindBonds";
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
            //typeof(BattleUnitBuf).GetField("_bufIcon", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, ModData.Sprites["BufIcons_" + buffName]);
            //typeof(BattleUnitBuf).GetField("_iconInit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, true);
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

    public class BattleUnitBuf_seraph_unstable_entropy : BattleUnitBuf
    {
        private const string buffName = "SeraphRunaways_UnstableEntropy";
        //private const int overflowValue = 3;

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
            stack = Duration + 1;
        }

        public override void OnRoundStart()
        {
            var erosionStacks = _owner.bufListDetail.GetKewordBufStack(KeywordBuf.Decay);
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, erosionStacks);
            //var overflow = Math.Max(erosionStacks - overflowValue, 0);
            //if (overflow > 0)
            //{
            //    Debug.Log("Unstable Entropy triggered");
            //    var allies = BattleObjectManager.instance.GetAliveList(_owner.faction);
            //    allies.Remove(_owner);
            //    foreach (var ally in allies)
            //    {
            //        ally.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Decay, overflow);
            //        if (ally.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay decay)
            //        {
            //            decay.ChangeToYanDecay();
            //        }
            //    }
            //}
            base.OnRoundStart();
        }

        public override void OnAddBuf(int addedStack)
        {
            stack = Duration + 1;
        }

        public override void OnRoundEnd()
        {
            stack--;
            if (stack <= 0)
            {
                _owner.bufListDetail.RemoveBufAll(KeywordBuf.Decay);
                Destroy();
            }
        }
    }
}
