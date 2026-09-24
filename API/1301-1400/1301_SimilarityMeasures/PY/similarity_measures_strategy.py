import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class similarity_measures_strategy(Strategy):
    def __init__(self):
        super(similarity_measures_strategy, self).__init__()

        self._sma_length = self.Param("SmaLength", 20).SetGreaterThanZero()
        self._distance_threshold = self.Param("DistanceThreshold", 1.0).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._previous_distance = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(similarity_measures_strategy, self).OnReseted()
        self._previous_distance = None

    def OnStarted2(self, time):
        super(similarity_measures_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = int(self._sma_length.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        distance = abs(float(candle.ClosePrice) - float(sma_value))
        threshold = float(self._distance_threshold.Value)

        if self._previous_distance is not None:
            if self._previous_distance >= threshold and distance < threshold and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif self._previous_distance <= threshold and distance > threshold and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._previous_distance = distance

    def CreateClone(self):
        return similarity_measures_strategy()
