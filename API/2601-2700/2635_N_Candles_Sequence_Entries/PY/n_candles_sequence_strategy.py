import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class n_candles_sequence_strategy(Strategy):
    """Opens positions after detecting N identical-direction candles in a row."""

    def __init__(self):
        super(n_candles_sequence_strategy, self).__init__()

        self._consecutive = self.Param("ConsecutiveCandles", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Consecutive Candles", "Number of identical candles in a row", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")

        self._direction = 0
        self._count = 0

    @property
    def ConsecutiveCandles(self):
        return self._consecutive.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(n_candles_sequence_strategy, self).OnStarted2(time)

        self._direction = 0
        self._count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        d = 0
        if float(candle.ClosePrice) > float(candle.OpenPrice):
            d = 1
        elif float(candle.ClosePrice) < float(candle.OpenPrice):
            d = -1

        if d == 0:
            self._direction = 0
            self._count = 0
            return

        if d == self._direction:
            self._count += 1
        else:
            self._direction = d
            self._count = 1

        if self._count < self.ConsecutiveCandles:
            return

        if d > 0 and self.Position <= 0:
            self.BuyMarket()
        elif d < 0 and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(n_candles_sequence_strategy, self).OnReseted()
        self._direction = 0
        self._count = 0

    def CreateClone(self):
        return n_candles_sequence_strategy()
