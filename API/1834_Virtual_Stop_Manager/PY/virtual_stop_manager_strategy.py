import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class virtual_stop_manager_strategy(Strategy):
    def __init__(self):
        super(virtual_stop_manager_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period", "Risk")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
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
        super(virtual_stop_manager_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(virtual_stop_manager_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(fast, slow, self.process_candle) \
            .Start()

    def process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return
        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return virtual_stop_manager_strategy()
