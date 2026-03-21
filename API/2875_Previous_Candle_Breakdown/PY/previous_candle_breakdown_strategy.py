import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class previous_candle_breakdown_strategy(Strategy):
    def __init__(self):
        super(previous_candle_breakdown_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(previous_candle_breakdown_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(previous_candle_breakdown_strategy, self).OnStarted(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_high = h
            self._prev_low = l
            self._has_prev = True
            return
        if c > self._prev_high and self.Position <= 0:
            self.BuyMarket()
        elif c < self._prev_low and self.Position >= 0:
            self.SellMarket()
        self._prev_high = h
        self._prev_low = l

    def CreateClone(self):
        return previous_candle_breakdown_strategy()
