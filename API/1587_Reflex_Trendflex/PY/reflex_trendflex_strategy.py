import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class reflex_trendflex_strategy(Strategy):
    def __init__(self):
        super(reflex_trendflex_strategy, self).__init__()
        self._reflex_len = self.Param("ReflexLength", 10) \
            .SetDisplay("Reflex Length", "Reflex EMA length", "General")
        self._trendflex_len = self.Param("TrendflexLength", 30) \
            .SetDisplay("Trendflex Length", "Trendflex EMA length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._prev_reflex = 0.0
        self._prev_trend = 0.0

    @property
    def reflex_len(self):
        return self._reflex_len.Value

    @property
    def trendflex_len(self):
        return self._trendflex_len.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(reflex_trendflex_strategy, self).OnReseted()
        self._prev_reflex = 0.0
        self._prev_trend = 0.0

    def OnStarted(self, time):
        super(reflex_trendflex_strategy, self).OnStarted(time)
        reflex = ExponentialMovingAverage()
        reflex.Length = self.reflex_length
        trend = ExponentialMovingAverage()
        trend.Length = self.trendflex_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(reflex, trend, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, reflex)
            self.DrawIndicator(area, trend)
            self.DrawOwnTrades(area)

    def on_process(self, candle, reflex_val, trend_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_reflex == 0:
            self._prev_reflex = reflex_val
            self._prev_trend = trend_val
            return
        prev_diff = self._prev_reflex - self._prev_trend
        curr_diff = reflex_val - trend_val
        if prev_diff <= 0 and curr_diff > 0 and self.Position <= 0:
            self.BuyMarket()
        elif prev_diff >= 0 and curr_diff < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_reflex = reflex_val
        self._prev_trend = trend_val

    def CreateClone(self):
        return reflex_trendflex_strategy()
