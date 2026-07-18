import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class renko_scalper_strategy(Strategy):
    def __init__(self):
        super(renko_scalper_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_close = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(renko_scalper_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(renko_scalper_strategy, self).OnStarted2(time)
        stdev = StandardDeviation()
        stdev.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(stdev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, stdev_val):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._previous_close = close
            self._has_prev = True
            return
        if stdev_val <= 0:
            self._previous_close = close
            return
        diff = close - self._previous_close
        if diff > stdev_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif diff < -stdev_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._previous_close = close

    def CreateClone(self):
        return renko_scalper_strategy()
