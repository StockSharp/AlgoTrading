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

class color_bears_strategy(Strategy):
    def __init__(self):
        super(color_bears_strategy, self).__init__()
        self._ma1_period = self.Param("Ma1Period", 12) \
            .SetDisplay("MA1", "First MA length", "Parameters")
        self._ma2_period = self.Param("Ma2Period", 5) \
            .SetDisplay("MA2", "Second MA length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle", "Candle type", "Parameters")
        self._ma1 = None
        self._ma2 = None
        self._prev_value = None
        self._prev_color = None

    @property
    def ma1_period(self):
        return self._ma1_period.Value

    @property
    def ma2_period(self):
        return self._ma2_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_bears_strategy, self).OnReseted()
        self._ma1 = None
        self._ma2 = None
        self._prev_value = None
        self._prev_color = None

    def OnStarted2(self, time):
        super(color_bears_strategy, self).OnStarted2(time)
        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self.ma1_period
        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self.ma2_period
        self.Indicators.Add(self._ma1)
        self.Indicators.Add(self._ma2)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ma1_result = process_float(self._ma1, candle.ClosePrice, candle.OpenTime, True)
        if not self._ma1.IsFormed:
            return
        bears = float(candle.LowPrice) - float(ma1_result)
        ma2_result = process_float(self._ma2, bears, candle.OpenTime, True)
        if not self._ma2.IsFormed:
            return
        current = float(ma2_result)
        color = 1
        if self._prev_value is not None:
            if self._prev_value < current:
                color = 0
            elif self._prev_value > current:
                color = 2
            if self._prev_color == 0 and color == 2:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            elif self._prev_color == 2 and color == 0:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()
        self._prev_color = color
        self._prev_value = current

    def CreateClone(self):
        return color_bears_strategy()
