import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class pending_limit_grid_strategy(Strategy):
    def __init__(self):
        super(pending_limit_grid_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 24).SetDisplay("Channel Period", "Grid channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pending_limit_grid_strategy, self).OnReseted()
        self._has_prev = False
        self._prev_close = 0
        self._prev_mid = 0

    def OnStarted2(self, time):
        super(pending_limit_grid_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._prev_close = 0
        self._prev_mid = 0

        highest = Highest()
        highest.Length = self._channel_period.Value
        lowest = Lowest()
        lowest.Length = self._channel_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(highest, lowest, self.OnProcess).Start()

    def OnProcess(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        mid = (highest + lowest) / 2.0
        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return

        if self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return pending_limit_grid_strategy()
