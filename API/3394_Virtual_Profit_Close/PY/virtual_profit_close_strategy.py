import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class virtual_profit_close_strategy(Strategy):
    def __init__(self):
        super(virtual_profit_close_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 20) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")

        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(virtual_profit_close_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(virtual_profit_close_strategy, self).OnStarted(time)

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self.slow_period
        self._has_prev = False
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._fast, self._slow, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast.IsFormed or not self._slow.IsFormed:
            return

        close = float(candle.ClosePrice)
        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if self._has_prev:
            if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
            elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._has_prev = True

    def CreateClone(self):
        return virtual_profit_close_strategy()
