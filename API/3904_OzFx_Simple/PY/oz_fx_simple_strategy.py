import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class oz_fx_simple_strategy(Strategy):
    def __init__(self):
        super(oz_fx_simple_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 20).SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 80).SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 100).SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(oz_fx_simple_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(oz_fx_simple_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._cooldown_remaining = 0

        fast = WeightedMovingAverage()
        fast.Length = self._fast_period.Value
        slow = WeightedMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_candles.Value
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_candles.Value

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return oz_fx_simple_strategy()
