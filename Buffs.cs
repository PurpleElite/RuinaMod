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
}
