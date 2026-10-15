import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Aroon
from StockSharp.Algo.Strategies import Strategy


class aroon_horn_sign_strategy(Strategy):
    def __init__(self):
        super(aroon_horn_sign_strategy, self).__init__()
        self._aroon_period = self.Param("AroonPeriod", 9) \
            .SetDisplay("Aroon Period", "Aroon indicator period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for processing", "General")
        self._prev_trend = 0

    @property
    def aroon_period(self):
        return self._aroon_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aroon_horn_sign_strategy, self).OnReseted()
        self._prev_trend = 0

    def OnStarted2(self, time):
        super(aroon_horn_sign_strategy, self).OnStarted2(time)
        self._prev_trend = 0
        aroon = Aroon()
        aroon.Length = self.aroon_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(aroon, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, aroon)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, aroon_value):
        if candle.State != CandleStates.Finished:
            return
        if not aroon_value.IsFormed:
            return
        up = aroon_value.Up
        down = aroon_value.Down
        if up is None or down is None:
            return
        up = float(up)
        down = float(down)
        trend = self._prev_trend
        if up > down and up >= 50.0:
            trend = 1
        elif down > up and down >= 50.0:
            trend = -1
        if self._prev_trend <= 0 and trend > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_trend >= 0 and trend < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_trend = trend

    def CreateClone(self):
        return aroon_horn_sign_strategy()
