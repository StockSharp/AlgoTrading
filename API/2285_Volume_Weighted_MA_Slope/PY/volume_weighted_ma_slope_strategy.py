import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volume_weighted_ma_slope_strategy(Strategy):
    def __init__(self):
        super(volume_weighted_ma_slope_strategy, self).__init__()
        self._vwma_period = self.Param("VwmaPeriod", 12) \
            .SetDisplay("VWMA Period", "Period of the Volume Weighted Moving Average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_vwma1 = None
        self._prev_vwma2 = None

    @property
    def vwma_period(self):
        return self._vwma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_weighted_ma_slope_strategy, self).OnReseted()
        self._prev_vwma1 = None
        self._prev_vwma2 = None

    def OnStarted(self, time):
        super(volume_weighted_ma_slope_strategy, self).OnStarted(time)
        self._prev_vwma1 = None
        self._prev_vwma2 = None
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self.vwma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, current_vwma):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        current_vwma = float(current_vwma)
        if self._prev_vwma1 is None:
            self._prev_vwma1 = current_vwma
            return
        if self._prev_vwma2 is None:
            self._prev_vwma2 = self._prev_vwma1
            self._prev_vwma1 = current_vwma
            return
        if self._prev_vwma2 > self._prev_vwma1 and current_vwma > self._prev_vwma1 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_vwma2 < self._prev_vwma1 and current_vwma < self._prev_vwma1 and self.Position >= 0:
            self.SellMarket()
        self._prev_vwma2 = self._prev_vwma1
        self._prev_vwma1 = current_vwma

    def CreateClone(self):
        return volume_weighted_ma_slope_strategy()
