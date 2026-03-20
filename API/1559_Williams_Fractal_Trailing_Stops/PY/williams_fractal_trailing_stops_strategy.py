import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class williams_fractal_trailing_stops_strategy(Strategy):
    def __init__(self):
        super(williams_fractal_trailing_stops_strategy, self).__init__()
        self._buffer_percent = self.Param("BufferPercent", 0.0) \
            .SetDisplay("Stop Buffer %", "Percent buffer added to fractal price", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = []
        self._lows = []
        self._long_stop = None
        self._short_stop = None

    @property
    def buffer_percent(self):
        return self._buffer_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_fractal_trailing_stops_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._long_stop = None
        self._short_stop = None

    def OnStarted(self, time):
        super(williams_fractal_trailing_stops_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []
        self._long_stop = None
        self._short_stop = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > 5:
            self._highs.pop(0)
        if len(self._lows) > 5:
            self._lows.pop(0)
        if len(self._highs) == 5 and len(self._lows) == 5:
            hs = self._highs
            ls = self._lows
            if hs[2] > hs[0] and hs[2] > hs[1] and hs[2] > hs[3] and hs[2] > hs[4]:
                price = hs[2] * (1.0 + self.buffer_percent / 100.0)
                if self._short_stop is not None:
                    self._short_stop = min(self._short_stop, price)
                else:
                    self._short_stop = price
            if ls[2] < ls[0] and ls[2] < ls[1] and ls[2] < ls[3] and ls[2] < ls[4]:
                price = ls[2] * (1.0 - self.buffer_percent / 100.0)
                if self._long_stop is not None:
                    self._long_stop = max(self._long_stop, price)
                else:
                    self._long_stop = price
        close = float(candle.ClosePrice)
        if self._short_stop is not None and close > self._short_stop and self.Position <= 0:
            self.BuyMarket()
        elif self._long_stop is not None and close < self._long_stop and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return williams_fractal_trailing_stops_strategy()
