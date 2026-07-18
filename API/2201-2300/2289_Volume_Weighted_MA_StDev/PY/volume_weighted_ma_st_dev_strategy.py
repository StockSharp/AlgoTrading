import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class volume_weighted_ma_st_dev_strategy(Strategy):
    def __init__(self):
        super(volume_weighted_ma_st_dev_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")
        self._vwma_length = self.Param("VwmaLength", 12) \
            .SetDisplay("VWMA Length", "Period for Volume Weighted MA", "Indicators")
        self._std_period = self.Param("StdPeriod", 9) \
            .SetDisplay("StdDev Period", "Period for standard deviation", "Indicators")
        self._k1 = self.Param("K1", 0.5) \
            .SetDisplay("K1", "Deviation multiplier for signal threshold", "Signal")
        self._vwma = None
        self._std_dev = None
        self._prev_vwma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def vwma_length(self):
        return self._vwma_length.Value

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def k1(self):
        return self._k1.Value

    def OnReseted(self):
        super(volume_weighted_ma_st_dev_strategy, self).OnReseted()
        self._vwma = None
        self._std_dev = None
        self._prev_vwma = None

    def OnStarted2(self, time):
        super(volume_weighted_ma_st_dev_strategy, self).OnStarted2(time)
        self._prev_vwma = None
        self._vwma = VolumeWeightedMovingAverage()
        self._vwma.Length = self.vwma_length
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.std_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._vwma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, vwma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._vwma.IsFormed:
            return
        vwma_value = float(vwma_value)
        if self._prev_vwma is None:
            self._prev_vwma = vwma_value
            return
        diff = vwma_value - self._prev_vwma
        std_result = process_float(self._std_dev, diff, candle.ServerTime, True)
        if not self._std_dev.IsFormed:
            self._prev_vwma = vwma_value
            return
        std_value = float(std_result)
        k1 = float(self.k1)
        filter_val = k1 * std_value
        if diff > filter_val and self.Position <= 0:
            self.BuyMarket()
        elif diff < -filter_val and self.Position >= 0:
            self.SellMarket()
        self._prev_vwma = vwma_value

    def CreateClone(self):
        return volume_weighted_ma_st_dev_strategy()
