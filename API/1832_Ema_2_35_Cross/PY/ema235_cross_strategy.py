import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema235_cross_strategy(Strategy):
    def __init__(self):
        super(ema235_cross_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA length", "Parameters")
        self._slow_length = self.Param("SlowLength", 35) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema235_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ema235_cross_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
        self.SubscribeCandles(self.candle_type).Bind(fast, slow, self.process_candle).Start()

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_fast = float(fast_val)
            self._prev_slow = float(slow_val)
            self._has_prev = True
            return

        f = float(fast_val)
        s = float(slow_val)

        cross_up = self._prev_fast <= self._prev_slow and f > s
        cross_down = self._prev_fast >= self._prev_slow and f < s

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return ema235_cross_strategy()
