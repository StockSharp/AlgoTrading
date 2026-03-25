import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class trend_alexcud_strategy(Strategy):
    """Multiple EMA alignment: long when price above all 5 EMAs, short when below all."""
    def __init__(self):
        super(trend_alexcud_strategy, self).__init__()
        self._ma1 = self.Param("MaPeriod1", 5).SetGreaterThanZero().SetDisplay("MA 1", "Shortest MA period", "Indicators")
        self._ma2 = self.Param("MaPeriod2", 8).SetGreaterThanZero().SetDisplay("MA 2", "Second MA period", "Indicators")
        self._ma3 = self.Param("MaPeriod3", 13).SetGreaterThanZero().SetDisplay("MA 3", "Third MA period", "Indicators")
        self._ma4 = self.Param("MaPeriod4", 21).SetGreaterThanZero().SetDisplay("MA 4", "Fourth MA period", "Indicators")
        self._ma5 = self.Param("MaPeriod5", 34).SetGreaterThanZero().SetDisplay("MA 5", "Longest MA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Primary timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trend_alexcud_strategy, self).OnReseted()
        self._prev_bias = 0

    def OnStarted(self, time):
        super(trend_alexcud_strategy, self).OnStarted(time)
        self._prev_bias = 0

        ema1 = ExponentialMovingAverage()
        ema1.Length = self._ma1.Value
        ema2 = ExponentialMovingAverage()
        ema2.Length = self._ma2.Value
        ema3 = ExponentialMovingAverage()
        ema3.Length = self._ma3.Value
        ema4 = ExponentialMovingAverage()
        ema4.Length = self._ma4.Value
        ema5 = ExponentialMovingAverage()
        ema5.Length = self._ma5.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema1, ema2, ema3, ema4, ema5, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema1)
            self.DrawIndicator(area, ema5)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, v1, v2, v3, v4, v5):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        is_bull = price > v1 and price > v2 and price > v3 and price > v4 and price > v5
        is_bear = price < v1 and price < v2 and price < v3 and price < v4 and price < v5

        if is_bull and self._prev_bias != 1 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif is_bear and self._prev_bias != -1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        if is_bull:
            self._prev_bias = 1
        elif is_bear:
            self._prev_bias = -1
        else:
            self._prev_bias = 0

    def CreateClone(self):
        return trend_alexcud_strategy()
