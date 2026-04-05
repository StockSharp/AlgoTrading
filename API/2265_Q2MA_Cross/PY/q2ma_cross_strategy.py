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

class q2ma_cross_strategy(Strategy):
    def __init__(self):
        super(q2ma_cross_strategy, self).__init__()
        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "Moving average length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Indicator timeframe", "General")
        self._close_ma = None
        self._open_ma = None
        self._prev_up = None
        self._prev_dn = None

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(q2ma_cross_strategy, self).OnReseted()
        self._close_ma = None
        self._open_ma = None
        self._prev_up = None
        self._prev_dn = None

    def OnStarted2(self, time):
        super(q2ma_cross_strategy, self).OnStarted2(time)
        self._prev_up = None
        self._prev_dn = None
        self._close_ma = ExponentialMovingAverage()
        self._close_ma.Length = self.length
        self._open_ma = ExponentialMovingAverage()
        self._open_ma.Length = self.length
        self.Indicators.Add(self._close_ma)
        self.Indicators.Add(self._open_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        up_result = process_float(self._close_ma, float(candle.ClosePrice), t, True)
        dn_result = process_float(self._open_ma, float(candle.OpenPrice), t, True)
        if not self._close_ma.IsFormed or not self._open_ma.IsFormed:
            return
        up = float(up_result)
        dn = float(dn_result)
        if self._prev_up is None or self._prev_dn is None:
            self._prev_up = up
            self._prev_dn = dn
            return
        if self._prev_up <= self._prev_dn and up > dn and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_up >= self._prev_dn and up < dn and self.Position >= 0:
            self.SellMarket()
        self._prev_up = up
        self._prev_dn = dn

    def CreateClone(self):
        return q2ma_cross_strategy()
