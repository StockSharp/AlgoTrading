import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema5813_adx_filter_strategy(Strategy):
    def __init__(self):
        super(ema5813_adx_filter_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_diff = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema5813_adx_filter_strategy, self).OnReseted()
        self._prev_diff = 0.0

    def OnStarted(self, time):
        super(ema5813_adx_filter_strategy, self).OnStarted(time)
        ema5 = ExponentialMovingAverage()
        ema5.Length = 5
        ema8 = ExponentialMovingAverage()
        ema8.Length = 8
        ema13 = ExponentialMovingAverage()
        ema13.Length = 13
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema5, ema8, ema13, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema5)
            self.DrawIndicator(area, ema8)
            self.DrawIndicator(area, ema13)
            self.DrawOwnTrades(area)

    def on_process(self, candle, e5, e8, e13):
        if candle.State != CandleStates.Finished:
            return
        diff = e5 - e8
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        self._prev_diff = diff
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        # Exit on EMA13 cross
        if self.Position > 0 and candle.ClosePrice < e13:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice > e13:
            self.BuyMarket()

    def CreateClone(self):
        return ema5813_adx_filter_strategy()
