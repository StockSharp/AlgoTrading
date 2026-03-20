import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides
from StockSharp.Messages import Unit, UnitTypes


class stellar_lite_ict_ea_strategy(Strategy):
    def __init__(self):
        super(stellar_lite_ict_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._higher_timeframe_type = self.Param("HigherTimeframeType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._higher_ma_period = self.Param("HigherMaPeriod", 20) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._liquidity_lookback = self.Param("LiquidityLookback", 20) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._atr_threshold = self.Param("AtrThreshold", 2.0) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp1_ratio = self.Param("Tp1Ratio", 1) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp2_ratio = self.Param("Tp2Ratio", 2) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp3_ratio = self.Param("Tp3Ratio", 3) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp1_percent = self.Param("Tp1Percent", 50) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp2_percent = self.Param("Tp2Percent", 25) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._tp3_percent = self.Param("Tp3Percent", 25) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._move_to_break_even = self.Param("MoveToBreakEven", True) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._break_even_offset = self.Param("BreakEvenOffset", 1) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._trailing_distance = self.Param("TrailingDistance", 10) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._use_silver_bullet = self.Param("UseSilverBullet", True) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._use2022_model = self.Param("Use2022Model", True) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._use_ote_entry = self.Param("UseOteEntry", True) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._risk_percent = self.Param("RiskPercent", 0.25) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")
        self._ote_lower_level = self.Param("OteLowerLevel", 0.618) \
            .SetDisplay("Entry Candle", "Primary timeframe used for entries", "General")

        self._higher_ma = None
        self._atr = None
        self._last_htf_ma = None
        self._previous_htf_ma = None
        self._current_bias = None
        self._history_count = 0.0
        self._latest_atr = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stellar_lite_ict_ea_strategy, self).OnReseted()
        self._higher_ma = None
        self._atr = None
        self._last_htf_ma = None
        self._previous_htf_ma = None
        self._current_bias = None
        self._history_count = 0.0
        self._latest_atr = 0.0

    def OnStarted(self, time):
        super(stellar_lite_ict_ea_strategy, self).OnStarted(time)

        self.__higher_ma = SimpleMovingAverage()
        self.__higher_ma.Length = self.higher_ma_period
        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period

        main_subscription = self.SubscribeCandles(self.candle_type)
        main_subscription.Bind(self._process_candle).Start()

        higher_subscription = self.SubscribeCandles(self.higher_timeframe_type)
        higher_subscription.Bind(self.__higher_ma, self._process_candle_1).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return stellar_lite_ict_ea_strategy()
