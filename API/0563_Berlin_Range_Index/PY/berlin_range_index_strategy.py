import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ChoppinessIndex
from StockSharp.Algo.Strategies import Strategy


class berlin_range_index_strategy(Strategy):
    def __init__(self):
        super(berlin_range_index_strategy, self).__init__()
        self._length = self.Param("Length", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Choppiness index period", "General")
        self._chop_threshold = self.Param("ChopThreshold", 55.0) \
            .SetDisplay("Chop Threshold", "Threshold for trend vs range", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_chop = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(berlin_range_index_strategy, self).OnReseted()
        self._prev_chop = 0.0

    def OnStarted(self, time):
        super(berlin_range_index_strategy, self).OnStarted(time)
        chop = ChoppinessIndex()
        chop.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(chop, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, chop)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, chop_val):
        if candle.State != CandleStates.Finished:
            return
        chop_v = float(chop_val)
        if self._prev_chop == 0:
            self._prev_chop = chop_v
            return
        threshold = float(self._chop_threshold.Value)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        if chop_v < threshold and self._prev_chop >= threshold:
            if close > open_p and self.Position <= 0:
                self.BuyMarket()
            elif close < open_p and self.Position >= 0:
                self.SellMarket()
        elif chop_v > threshold and self._prev_chop <= threshold:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
        self._prev_chop = chop_v

    def CreateClone(self):
        return berlin_range_index_strategy()
