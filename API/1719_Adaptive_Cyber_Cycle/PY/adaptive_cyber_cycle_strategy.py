import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_cyber_cycle_strategy(Strategy):
    def __init__(self):
        super(adaptive_cyber_cycle_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", TimeSpan.FromHours(4)) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_cyber_cycle_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(adaptive_cyber_cycle_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._has_prev = True
            return
        # Fast crosses above slow => buy
        if self._prev_fast <= self._prev_slow and fast_value > slow_value and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Fast crosses below slow => sell
        elif self._prev_fast >= self._prev_slow and fast_value < slow_value and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        return adaptive_cyber_cycle_strategy()
