import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ama_trader_v21_strategy(Strategy):
    """
    AMA Trader V2.1: dual EMA crossover strategy.
    """

    def __init__(self):
        super(ama_trader_v21_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def FastPeriod(self): return self._fast_period.Value
    @FastPeriod.setter
    def FastPeriod(self, v): self._fast_period.Value = v
    @property
    def SlowPeriod(self): return self._slow_period.Value
    @SlowPeriod.setter
    def SlowPeriod(self, v): self._slow_period.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(ama_trader_v21_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ama_trader_v21_strategy, self).OnStarted(time)

        self._has_prev = False
        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ama_trader_v21_strategy()
