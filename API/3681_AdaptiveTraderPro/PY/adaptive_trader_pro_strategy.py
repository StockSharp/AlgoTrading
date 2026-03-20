import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_trader_pro_strategy(Strategy):
    def __init__(self):
        super(adaptive_trader_pro_strategy, self).__init__()

        self._max_risk_percent = self.Param("MaxRiskPercent", 0.2) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._trailing_stop_multiplier = self.Param("TrailingStopMultiplier", 3.0) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._trailing_take_profit_multiplier = self.Param("TrailingTakeProfitMultiplier", 2.0) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._trend_period = self.Param("TrendPeriod", 20) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._higher_trend_period = self.Param("HigherTrendPeriod", 50) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._break_even_multiplier = self.Param("BreakEvenMultiplier", 1.5) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._partial_close_fraction = self.Param("PartialCloseFraction", 0) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._max_spread_points = self.Param("MaxSpreadPoints", 20) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")
        self._higher_candle_type = self.Param("HigherCandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Max Risk %", "Risk percentage applied on each trade", "Risk Management")

        self._best_bid_price = None
        self._best_ask_price = None
        self._last_higher_trend_value = 0.0
        self._entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._partial_take_profit_done = False
        self._trailing_stop_level = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_trader_pro_strategy, self).OnReseted()
        self._best_bid_price = None
        self._best_ask_price = None
        self._last_higher_trend_value = 0.0
        self._entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._partial_take_profit_done = False
        self._trailing_stop_level = 0.0

    def OnStarted(self, time):
        super(adaptive_trader_pro_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._trend_ma = SimpleMovingAverage()
        self._trend_ma.Length = self.trend_period
        self._higher_trend_ma = SimpleMovingAverage()
        self._higher_trend_ma.Length = Higherself.trend_period

        main_subscription = self.SubscribeCandles(self.candle_type)
        main_subscription.Bind(self._rsi, self._atr, self._trend_ma, self._process_candle).Start()

        higher_subscription = self.SubscribeCandles(Higherself.candle_type)
        higher_subscription.Bind(self._higher_trend_ma, self._process_candle_1).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return adaptive_trader_pro_strategy()
