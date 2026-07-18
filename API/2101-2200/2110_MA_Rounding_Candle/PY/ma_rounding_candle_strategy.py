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

class ma_rounding_candle_strategy(Strategy):
    def __init__(self):
        super(ma_rounding_candle_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 12) \
            .SetDisplay("MA Length", "Moving average length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._open_ma = None
        self._close_ma = None
        self._prev_color = 1

    @property
    def ma_length(self):
        return self._ma_length.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_rounding_candle_strategy, self).OnReseted()
        self._open_ma = None
        self._close_ma = None
        self._prev_color = 1

    def OnStarted2(self, time):
        super(ma_rounding_candle_strategy, self).OnStarted2(time)
        self._open_ma = ExponentialMovingAverage()
        self._open_ma.Length = self.ma_length
        self._close_ma = ExponentialMovingAverage()
        self._close_ma.Length = self.ma_length
        self.Indicators.Add(self._open_ma)
        self.Indicators.Add(self._close_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._open_ma)
            self.DrawIndicator(area, self._close_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_result = process_float(self._open_ma, candle.OpenPrice, candle.OpenTime, True)
        close_result = process_float(self._close_ma, candle.ClosePrice, candle.OpenTime, True)
        if not self._open_ma.IsFormed or not self._close_ma.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_val = float(open_result)
        close_val = float(close_result)
        if open_val < close_val:
            color = 2
        elif open_val > close_val:
            color = 0
        else:
            color = 1

        if self._prev_color == 2 and color != 2 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_color == 0 and color != 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return ma_rounding_candle_strategy()
