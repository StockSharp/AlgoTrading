import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_i_custom_v1_strategy(Strategy):
    """Fast/slow EMA crossover strategy. Reverses position on opposite crossover."""

    def __init__(self):
        super(exp_i_custom_v1_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
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
        super(exp_i_custom_v1_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(exp_i_custom_v1_strategy, self).OnStarted(time)

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

        if bull_cross and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif bear_cross and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return exp_i_custom_v1_strategy()
