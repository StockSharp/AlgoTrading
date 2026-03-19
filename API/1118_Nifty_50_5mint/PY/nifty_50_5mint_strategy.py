import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class nifty_50_5mint_strategy(Strategy):
    def __init__(self):
        super(nifty_50_5mint_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle type", "Primary timeframe.", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(nifty_50_5mint_strategy, self).OnReseted()
        self._prev_f = 0
        self._prev_s = 0
        self._init = False
        self._last_signal = None
        self._cooldown = TimeSpan.FromMinutes(120)

    def OnStarted(self, time):
        super(nifty_50_5mint_strategy, self).OnStarted(time)
        self._prev_f = 0
        self._prev_s = 0
        self._init = False
        self._last_signal = None
        self._cooldown = TimeSpan.FromMinutes(120)

        fast = ExponentialMovingAverage()
        fast.Length = 12
        slow = ExponentialMovingAverage()
        slow.Length = 34
        rsi = RelativeStrengthIndex()
        rsi.Length = 14

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, f, s, r):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not self._init:
            self._prev_f = f
            self._prev_s = s
            self._init = True
            return
        if self._last_signal is not None and (candle.OpenTime - self._last_signal) < self._cooldown:
            pass
        else:
            if self._prev_f <= self._prev_s and f > s and r > 50 and self.Position <= 0:
                self.BuyMarket()
                self._last_signal = candle.OpenTime
            elif self._prev_f >= self._prev_s and f < s and r < 50 and self.Position > 0:
                self.SellMarket()
                self._last_signal = candle.OpenTime
        self._prev_f = f
        self._prev_s = s

    def CreateClone(self):
        return nifty_50_5mint_strategy()
