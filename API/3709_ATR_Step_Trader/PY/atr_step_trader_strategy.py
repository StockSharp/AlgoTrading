import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class atr_step_trader_strategy(Strategy):
    def __init__(self):
        super(atr_step_trader_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 70) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._slow_period = self.Param("SlowPeriod", 180) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._atr_period = self.Param("AtrPeriod", 100) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._momentum_period = self.Param("MomentumPeriod", 3) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._pyramid_limit = self.Param("PyramidLimit", 3) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._step_multiplier = self.Param("StepMultiplier", 4) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._steps_multiplier = self.Param("StepsMultiplier", 2) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._stop_multiplier = self.Param("StopMultiplier", 3) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")

        self._bullish_streak = 0.0
        self._bearish_streak = 0.0
        self._previous_slow = None
        self._long_entry_high = None
        self._long_entry_low = None
        self._short_entry_high = None
        self._short_entry_low = None
        self._long_stop_price = None
        self._short_stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_step_trader_strategy, self).OnReseted()
        self._bullish_streak = 0.0
        self._bearish_streak = 0.0
        self._previous_slow = None
        self._long_entry_high = None
        self._long_entry_low = None
        self._short_entry_high = None
        self._short_entry_low = None
        self._long_stop_price = None
        self._short_stop_price = None

    def OnStarted(self, time):
        super(atr_step_trader_strategy, self).OnStarted(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.fast_period
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.slow_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._highest = Highest()
        self._highest.Length = self.momentum_period
        self._lowest = Lowest()
        self._lowest.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self._atr, self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return atr_step_trader_strategy()
