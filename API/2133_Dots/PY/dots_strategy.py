import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dots_strategy(Strategy):
    """Strategy based on Dots indicator - weighted cosine filter with color changes."""

    def __init__(self):
        super(dots_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetDisplay("Length", "Dots calculation length", "Parameters")
        self._filter = self.Param("Filter", 0.0) \
            .SetDisplay("Filter", "Minimal delta to change color", "Parameters")
        self._coefficient = self.Param("Coefficient", 3.0 * math.pi) \
            .SetDisplay("Coefficient", "Weighting coefficient inside the filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prices = []
        self._prev_ma = None
        self._prev_color = 0.0
        self._prev_out_color = None

    @property
    def length(self):
        return self._length.Value

    @property
    def filter_val(self):
        return self._filter.Value

    @property
    def coefficient(self):
        return self._coefficient.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dots_strategy, self).OnReseted()
        self._prices = []
        self._prev_ma = None
        self._prev_color = 0.0
        self._prev_out_color = None

    def OnStarted(self, time):
        super(dots_strategy, self).OnStarted(time)
        self._prices = []
        self._prev_ma = None
        self._prev_color = 0.0
        self._prev_out_color = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        length = int(self.length)
        filt = float(self.filter_val)
        coeff = float(self.coefficient)
        total_len = length * 4 + (length - 1)
        res1 = 1.0 / max(1.0, length - 2)
        res2 = (2.0 * 4 - 1.0) / (4 * length - 1.0)
        self._prices.insert(0, price)
        if len(self._prices) > total_len:
            self._prices.pop()
        if len(self._prices) < total_len:
            return
        t = 0.0
        sum_val = 0.0
        weight = 0.0
        for i in range(total_len):
            g = 1.0 / (coeff * t + 1.0)
            if t <= 0.5:
                g = 1.0
            beta = math.cos(math.pi * t)
            alfa = g * beta
            sum_val += alfa * self._prices[i]
            weight += alfa
            if t < 1.0:
                t += res1
            elif t < total_len - 1:
                t += res2
        ma_prev = self._prev_ma if self._prev_ma is not None else self._prices[1]
        if weight != 0:
            ma = sum_val / abs(weight)
        else:
            ma = ma_prev
        if filt > 0 and abs(ma - ma_prev) < filt:
            ma = ma_prev
        if ma - ma_prev > filt:
            color = 0.0
        elif ma_prev - ma > filt:
            color = 1.0
        else:
            color = self._prev_color
        self._prev_ma = ma
        self._prev_color = color
        if self._prev_out_color is None:
            self._prev_out_color = color
            return
        if self._prev_out_color == 0.0 and color == 1.0 and self.Position >= 0:
            self.SellMarket()
        elif self._prev_out_color == 1.0 and color == 0.0 and self.Position <= 0:
            self.BuyMarket()
        self._prev_out_color = color

    def CreateClone(self):
        return dots_strategy()
