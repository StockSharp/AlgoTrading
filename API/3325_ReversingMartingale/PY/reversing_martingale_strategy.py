import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class reversing_martingale_strategy(Strategy):
    def __init__(self):
        super(reversing_martingale_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetGreaterThanZero().SetDisplay("Fast WMA", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetGreaterThanZero().SetDisplay("Slow WMA", "Slow WMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(reversing_martingale_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0

    def OnStarted2(self, time):
        super(reversing_martingale_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0

        fast = WeightedMovingAverage()
        fast.Length = self._fast_period.Value
        slow = WeightedMovingAverage()
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
        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return reversing_martingale_strategy()
