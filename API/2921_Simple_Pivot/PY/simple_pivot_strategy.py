import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class simple_pivot_strategy(Strategy):
    def __init__(self):
        super(simple_pivot_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(simple_pivot_strategy, self).OnReseted()
        self._prev_high = 0
        self._prev_low = 0
        self._has_prev = False
        self._last_dir = 0  # 1=long, -1=short

    def OnStarted2(self, time):
        super(simple_pivot_strategy, self).OnStarted2(time)
        self._prev_high = 0
        self._prev_low = 0
        self._has_prev = False
        self._last_dir = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return

        pivot = (self._prev_high + self._prev_low) / 2.0
        open_price = float(candle.OpenPrice)

        desired = 1  # long
        if open_price < self._prev_high and open_price > pivot:
            desired = -1  # short

        if desired == self._last_dir and self._last_dir != 0:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            return

        # Close existing
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        if desired == 1:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._last_dir = desired
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return simple_pivot_strategy()
