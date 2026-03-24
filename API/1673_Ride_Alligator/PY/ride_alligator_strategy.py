import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ride_alligator_strategy(Strategy):
    def __init__(self):
        super(ride_alligator_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_jaw = 0.0
        self._prev_lips = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ride_alligator_strategy, self).OnReseted()
        self._prev_jaw = 0.0
        self._prev_lips = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ride_alligator_strategy, self).OnStarted(time)
        jaw = SmoothedMovingAverage()
        jaw.Length = 13
        teeth = SmoothedMovingAverage()
        teeth.Length = 8
        lips = SmoothedMovingAverage()
        lips.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jaw, teeth, lips, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, jaw, teeth, lips):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_jaw = jaw
            self._prev_lips = lips
            self._has_prev = True
            return
        # Lips crosses above jaw -> buy
        if self._prev_lips <= self._prev_jaw and lips > jaw and teeth < jaw:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Lips crosses below jaw -> sell
        elif self._prev_lips >= self._prev_jaw and lips < jaw and teeth > jaw:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        # Exit on price crossing jaw
        if self.Position > 0 and candle.ClosePrice < jaw:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice > jaw:
            self.BuyMarket()
        self._prev_jaw = jaw
        self._prev_lips = lips

    def CreateClone(self):
        return ride_alligator_strategy()
