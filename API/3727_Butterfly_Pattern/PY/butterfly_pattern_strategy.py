import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class butterfly_pattern_strategy(Strategy):
    def __init__(self):
        super(butterfly_pattern_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._pivot_left = self.Param("PivotLeft", 1) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._pivot_right = self.Param("PivotRight", 1) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._tolerance = self.Param("Tolerance", 0.50) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._allow_trading = self.Param("AllowTrading", True) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._use_fixed_volume = self.Param("UseFixedVolume", True) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._fixed_volume = self.Param("FixedVolume", 1) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._adjust_lots = self.Param("AdjustLotsForTakeProfits", True) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._tp1_percent = self.Param("Tp1Percent", 50) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._tp2_percent = self.Param("Tp2Percent", 30) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._tp3_percent = self.Param("Tp3Percent", 20) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._min_pattern_quality = self.Param("MinPatternQuality", 0.01) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._use_session_filter = self.Param("UseSessionFilter", False) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._session_start_hour = self.Param("SessionStartHour", 8) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._session_end_hour = self.Param("SessionEndHour", 16) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._revalidate_pattern = self.Param("RevalidatePattern", False) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._use_break_even = self.Param("UseBreakEven", False) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._break_even_after_tp = self.Param("BreakEvenAfterTp", 1) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._break_even_trigger = self.Param("BreakEvenTrigger", 30) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._break_even_profit = self.Param("BreakEvenProfit", 5) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._use_trailing_stop = self.Param("UseTrailingStop", False) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._trail_after_tp = self.Param("TrailAfterTp", 2) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._trail_start = self.Param("TrailStart", 20) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")
        self._trail_step = self.Param("TrailStep", 5) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "General")

        self._candles = new()
        self._pivots = new()
        self._state = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(butterfly_pattern_strategy, self).OnReseted()
        self._candles = new()
        self._pivots = new()
        self._state = None

    def OnStarted(self, time):
        super(butterfly_pattern_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return butterfly_pattern_strategy()
