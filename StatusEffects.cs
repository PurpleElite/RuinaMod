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

        public static int Duration = 2;
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
