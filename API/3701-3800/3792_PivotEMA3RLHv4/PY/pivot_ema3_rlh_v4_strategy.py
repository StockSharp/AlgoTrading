import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class pivot_ema3_rlh_v4_strategy(Strategy):
    """Pivot EMA strategy using fast and slow EMA crossover.
    Buy when fast EMA crosses above slow EMA.
    Sell when fast EMA crosses below slow EMA."""

    def __init__(self):
        super(pivot_ema3_rlh_v4_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(pivot_ema3_rlh_v4_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(pivot_ema3_rlh_v4_strategy, self).OnStarted2(time)

        self._has_prev = False

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        slow_val = float(slow)

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        bull_cross = self._prev_fast <= self._prev_slow and fast_val > slow_val
        bear_cross = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if self.Position <= 0 and bull_cross:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and bear_cross:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return pivot_ema3_rlh_v4_strategy()
