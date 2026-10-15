import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class color_bears_gap_strategy(Strategy):
    def __init__(self):
        super(color_bears_gap_strategy, self).__init__()
        self._length1 = self.Param("Length1", 12) \
            .SetDisplay("Length 1", "First smoothing length", "Parameters")
        self._length2 = self.Param("Length2", 5) \
            .SetDisplay("Length 2", "Second smoothing length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candle subscription", "Parameters")
        self._sma_close = None
        self._sma_open = None
        self._sma_bulls_c = None
        self._sma_bulls_o = None
        self._prev_xbulls_c = 0.0
        self._is_first = True
        self._prev_value = 0.0

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
        super(color_bears_gap_strategy, self).OnReseted()
        self._sma_close = None
        self._sma_open = None
        self._sma_bulls_c = None
        self._sma_bulls_o = None
        self._prev_xbulls_c = 0.0
        self._is_first = True
        self._prev_value = 0.0

    def OnStarted2(self, time):
        super(color_bears_gap_strategy, self).OnStarted2(time)
        self._sma_close = ExponentialMovingAverage()
        self._sma_close.Length = self.length1
        self._sma_open = ExponentialMovingAverage()
        self._sma_open.Length = self.length1
        self._sma_bulls_c = ExponentialMovingAverage()
        self._sma_bulls_c.Length = self.length2
        self._sma_bulls_o = ExponentialMovingAverage()
        self._sma_bulls_o.Length = self.length2
        self._is_first = True
        self._prev_value = 0.0
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
        smooth_close = float(process_float(self._sma_close, candle.ClosePrice, t, True))
        smooth_open = float(process_float(self._sma_open, candle.OpenPrice, t, True))
        bulls_c = float(candle.HighPrice) - smooth_close
        bulls_o = float(candle.HighPrice) - smooth_open
        xbulls_c = float(process_float(self._sma_bulls_c, bulls_c, t, True))
        xbulls_o = float(process_float(self._sma_bulls_o, bulls_o, t, True))
        if self._is_first:
            self._prev_xbulls_c = xbulls_c
            self._is_first = False
            return
        diff = xbulls_o - self._prev_xbulls_c
        self._prev_xbulls_c = xbulls_c
        if self._prev_value > 0:
            prev_signal = 1
        elif self._prev_value < 0:
            prev_signal = -1
        else:
            prev_signal = 0
        if diff > 0:
            signal = 1
        elif diff < 0:
            signal = -1
        else:
            signal = 0
        if prev_signal <= 0 and signal > 0:
            if self.Position <= 0:
                self.BuyMarket()
        elif prev_signal >= 0 and signal < 0:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_value = diff

    def CreateClone(self):
        return color_bears_gap_strategy()
