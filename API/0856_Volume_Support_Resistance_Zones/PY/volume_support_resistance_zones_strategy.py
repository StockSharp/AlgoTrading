import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_support_resistance_zones_strategy(Strategy):
    """EMA crossover (120/450) for support/resistance zone breakout signals."""
    def __init__(self):
        super(volume_support_resistance_zones_strategy, self).__init__()
        self._fast_period = self.Param("FastEmaPeriod", 120).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowEmaPeriod", 450).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(volume_support_resistance_zones_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0

    def OnStarted(self, time):
        super(volume_support_resistance_zones_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return volume_support_resistance_zones_strategy()
