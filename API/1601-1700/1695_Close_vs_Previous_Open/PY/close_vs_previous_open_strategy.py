import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class close_vs_previous_open_strategy(Strategy):
    def __init__(self):
        super(close_vs_previous_open_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_open = 0.0
        self._prev_prev_open = 0.0
        self._prev_close = 0.0
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(close_vs_previous_open_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_prev_open = 0.0
        self._prev_close = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(close_vs_previous_open_strategy, self).OnStarted2(time)
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
        open_price = candle.OpenPrice
        self._bar_count += 1
        if self._bar_count >= 3 and stdev_val > 0:
            diff = self._prev_close - self._prev_prev_open
            # Only trade on significant moves (> 1 stdev)
            if diff > stdev_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif diff < -stdev_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_prev_open = self._prev_open
        self._prev_open = open_price
        self._prev_close = close

    def CreateClone(self):
        return close_vs_previous_open_strategy()
