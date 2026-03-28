import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class suffic369_strategy(Strategy):
    """Fast/slow SMA crossover (3/6)."""
    def __init__(self):
        super(suffic369_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 3).SetDisplay("Fast SMA Length", "Fast moving average period", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 6).SetDisplay("Slow SMA Length", "Slow moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Primary candle source", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(suffic369_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(suffic369_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self._fast_ma_length.Value
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_sma, slow_sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return

        long_signal = self._prev_fast <= self._prev_slow and fast > slow
        short_signal = self._prev_fast >= self._prev_slow and fast < slow

        if self.Position > 0 and short_signal:
            self.SellMarket()
        elif self.Position < 0 and long_signal:
            self.BuyMarket()
        elif self.Position == 0:
            if long_signal:
                self.BuyMarket()
            elif short_signal:
                self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return suffic369_strategy()
