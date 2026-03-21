import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_bulls_gap_strategy(Strategy):
    def __init__(self):
        super(color_bulls_gap_strategy, self).__init__()
        self._length1 = self.Param("Length1", 12) \
            .SetDisplay("First Length", "Length for initial smoothing", "Indicator")
        self._length2 = self.Param("Length2", 5) \
            .SetDisplay("Second Length", "Length for secondary smoothing", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")
        self._sma_close = None
        self._sma_open = None
        self._sma_bulls_c = None
        self._sma_bulls_o = None
        self._prev_xbulls_c = 0.0
        self._is_first = True
        self._color_history = []

    @property
    def length1(self):
        return self._length1.Value

    @property
    def length2(self):
        return self._length2.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_bulls_gap_strategy, self).OnReseted()
        self._sma_close = None
        self._sma_open = None
        self._sma_bulls_c = None
        self._sma_bulls_o = None
        self._prev_xbulls_c = 0.0
        self._is_first = True
        self._color_history = []

    def OnStarted(self, time):
        super(color_bulls_gap_strategy, self).OnStarted(time)
        self._sma_close = ExponentialMovingAverage()
        self._sma_close.Length = self.length1
        self._sma_open = ExponentialMovingAverage()
        self._sma_open.Length = self.length1
        self._sma_bulls_c = ExponentialMovingAverage()
        self._sma_bulls_c.Length = self.length2
        self._sma_bulls_o = ExponentialMovingAverage()
        self._sma_bulls_o.Length = self.length2
        self._is_first = True
        self._color_history = []

        self.Indicators.Add(self._sma_close)
        self.Indicators.Add(self._sma_open)
        self.Indicators.Add(self._sma_bulls_c)
        self.Indicators.Add(self._sma_bulls_o)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime
        sma_close_val = float(self._sma_close.Process(candle.ClosePrice, t, True).GetValue[float]())
        sma_open_val = float(self._sma_open.Process(candle.OpenPrice, t, True).GetValue[float]())
        bulls_c = float(candle.HighPrice) - sma_close_val
        bulls_o = float(candle.HighPrice) - sma_open_val
        xbulls_c = float(self._sma_bulls_c.Process(bulls_c, t, True).GetValue[float]())
        xbulls_o = float(self._sma_bulls_o.Process(bulls_o, t, True).GetValue[float]())

        if self._is_first:
            self._prev_xbulls_c = xbulls_c
            self._is_first = False
            return

        diff = xbulls_o - self._prev_xbulls_c
        if diff > 0:
            color = 0
        elif diff < 0:
            color = 2
        else:
            color = 1
        self._prev_xbulls_c = xbulls_c
        self._color_history.append(color)
        if len(self._color_history) > 2:
            self._color_history.pop(0)
        if len(self._color_history) < 2:
            return

        prev_color = self._color_history[0]
        last_color = self._color_history[1]

        if prev_color == 0:
            if last_color > 0:
                self.BuyMarket()
            elif self.Position < 0:
                self.BuyMarket()
        elif prev_color == 2:
            if last_color < 2:
                self.SellMarket()
            elif self.Position > 0:
                self.SellMarket()

    def CreateClone(self):
        return color_bulls_gap_strategy()
