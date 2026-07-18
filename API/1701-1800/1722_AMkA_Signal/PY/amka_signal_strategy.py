import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation, KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class amka_signal_strategy(Strategy):
    def __init__(self):
        super(amka_signal_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetDisplay("KAMA Length", "Lookback period for the adaptive moving average", "Indicator")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.0) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation filter", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator calculation", "General")
        self._prev_kama = 0.0
        self._has_prev = False

    @property
    def length(self):
        return self._length.Value

    @property
    def deviation_multiplier(self):
        return self._deviation_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(amka_signal_strategy, self).OnReseted()
        self._prev_kama = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(amka_signal_strategy, self).OnStarted2(time)
        kama = KaufmanAdaptiveMovingAverage()
        kama.Length = self.length
        stdev = StandardDeviation()
        stdev.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kama, stdev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, kama_value, stdev_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_kama = kama_value
            self._has_prev = True
            return
        delta = kama_value - self._prev_kama
        self._prev_kama = kama_value
        if stdev_value <= 0:
            return
        threshold = stdev_value * self.deviation_multiplier
        if delta > threshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif delta < -threshold and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return amka_signal_strategy()
