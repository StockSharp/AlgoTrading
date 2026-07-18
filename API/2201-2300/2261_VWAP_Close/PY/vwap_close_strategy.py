import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_close_strategy(Strategy):
    def __init__(self):
        super(vwap_close_strategy, self).__init__()
        self._period = self.Param("Period", 2) \
            .SetDisplay("Period", "VWMA calculation period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev1 = None
        self._prev2 = None

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_close_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None

    def OnStarted2(self, time):
        super(vwap_close_strategy, self).OnStarted2(time)
        self._prev1 = None
        self._prev2 = None
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, vwma_value):
        if candle.State != CandleStates.Finished:
            return
        vwma_value = float(vwma_value)
        if self._prev1 is None or self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = vwma_value
            return
        prev1 = self._prev1
        prev2 = self._prev2
        if prev1 < prev2 and vwma_value > prev1 and self.Position <= 0:
            self.BuyMarket()
        elif prev1 > prev2 and vwma_value < prev1 and self.Position >= 0:
            self.SellMarket()
        self._prev2 = prev1
        self._prev1 = vwma_value

    def CreateClone(self):
        return vwap_close_strategy()
