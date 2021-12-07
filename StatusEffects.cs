using System.Linq;
using System.Reflection;

namespace CustomDLLs
{
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

    public class BattleUnitBuf_revive_at_turn_end : BattleUnitBuf
    {
        public override bool Hide => true;
        public override void OnRoundEndTheLast()
        {
            if (_owner.IsKnockout())
            {
                var knockoutBuf = _owner.bufListDetail.GetActivatedBufList().FirstOrDefault(x => x is BattleUnitBuf_knockout);
                knockoutBuf?.Destroy();
                typeof(BattleUnitBaseModel).GetField("_isKnockout", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_owner, false);
                _owner.SetHp(_owner.MaxHp / 2);
                _owner.breakDetail.ResetGauge();
                Destroy();
            }
        }
    }

    public class BattleUnitBuf_unstable_entropy : BattleUnitBuf
    {
        private const int overflowValue = 5;
        private const int duration = 2;
        private PassiveAbilityBase addedPassive;
        public override BufPositiveType positiveType => BufPositiveType.Negative;

        public override void OnRoundStart()
        {
            var erosionBuff = _owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay);
            _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Quickness, erosionBuff?.stack/2 ?? 0);
            base.OnRoundStart();
        }

        public override void OnAddBuf(int addedStack)
        {
            if (addedPassive == null)
            {
                addedPassive = _owner.passiveDetail.AddPassive(new PassiveAbility_unstable_entropy());
                _owner.passiveDetail.OnCreated();
            }
            stack = 2;
        }

        public override void OnRoundEnd()
        {
            stack--;
            if (stack <= 0)
            {
                _owner.passiveDetail.DestroyPassive(addedPassive);
                _owner.passiveDetail.RemovePassive();
                Destroy();
            }
        }

        private class PassiveAbility_unstable_entropy : PassiveAbilityBase
        {
            public override bool isHide => true;
            public override int OnAddKeywordBufByCard(BattleUnitBuf buf, int stack)
            {
                if (buf.stack >= overflowValue)
                {
                    var allies = Singleton<BattleObjectManager>.Instance.GetAliveList(owner.faction);
                    allies.Remove(owner);
                    foreach (var ally in allies)
                    {
                        ally.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Decay, 1);
                    }
                }
                return base.OnAddKeywordBufByCard(buf, stack);
            }
        }
    }
}
