import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class loco_strategy(Strategy):
    def __init__(self):
        super(loco_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._length = self.Param("Length", 1) \
            .SetDisplay("Length", "Lookback length", "Indicator")
        self._prices = []
        self._prev = None
        self._prev_color = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    def OnReseted(self):
        super(loco_strategy, self).OnReseted()
        self._prices = []
        self._prev = None
        self._prev_color = -1

    def OnStarted2(self, time):
        super(loco_strategy, self).OnStarted2(time)
        self._prices = []
        self._prev = None
        self._prev_color = -1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        ln = int(self.length)
        self._prices.append(price)
        if len(self._prices) <= ln:
            self._prev = price
            return
        series1 = self._prices.pop(0)
        prev = self._prev if self._prev is not None else price
        if price == prev:
            result = prev
            color = 0
        elif series1 > prev and price > prev:
            result = max(prev, price * 0.999)
            color = 0
        elif series1 < prev and price < prev:
            result = min(prev, price * 1.001)
            color = 1
        else:
            if price > prev:
                result = price * 0.999
                color = 0
            else:
                result = price * 1.001
                color = 1
        self._prev = result
        if self._prev_color == -1:
            self._prev_color = color
            return
        if color != self._prev_color:
            if color == 1:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            else:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return loco_strategy()
