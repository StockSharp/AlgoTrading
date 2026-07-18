import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class up_3x1_shifted_sma_strategy(Strategy):
    """Simple SMA crossover (fast/slow) with cooldown between trades."""
    def __init__(self):
        super(up_3x1_shifted_sma_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 24).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(up_3x1_shifted_sma_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(up_3x1_shifted_sma_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._cooldown = 0

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fv
            self._prev_slow = sv
            return

        if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return up_3x1_shifted_sma_strategy()
