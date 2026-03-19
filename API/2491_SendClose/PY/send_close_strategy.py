import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class send_close_strategy(Strategy):
    """Fractal breakout with trend lines. Simplified: uses 5-bar fractal detection and trend line entries."""
    def __init__(self):
        super(send_close_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(send_close_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._up_fractals = []
        self._down_fractals = []

    def OnStarted(self, time):
        super(send_close_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []
        self._up_fractals = []
        self._down_fractals = []

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > 220:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < 5:
            return

        h = self._highs
        l = self._lows
        n = len(h)

        # Check for up fractal (high[2] is highest of 5)
        if h[n-3] >= h[n-4] and h[n-3] > h[n-5] and h[n-3] >= h[n-2] and h[n-3] > h[n-1]:
            self._up_fractals.append(h[n-3])
            if len(self._up_fractals) > 6:
                self._up_fractals.pop(0)

        # Check for down fractal (low[2] is lowest of 5)
        if l[n-3] <= l[n-4] and l[n-3] < l[n-5] and l[n-3] <= l[n-2] and l[n-3] < l[n-1]:
            self._down_fractals.append(l[n-3])
            if len(self._down_fractals) > 6:
                self._down_fractals.pop(0)

        close = float(candle.ClosePrice)

        # Entry: break above latest up fractal
        if len(self._up_fractals) >= 2:
            resistance = self._up_fractals[-1]
            if close > resistance and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()

        # Entry: break below latest down fractal
        if len(self._down_fractals) >= 2:
            support = self._down_fractals[-1]
            if close < support and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

    def CreateClone(self):
        return send_close_strategy()
