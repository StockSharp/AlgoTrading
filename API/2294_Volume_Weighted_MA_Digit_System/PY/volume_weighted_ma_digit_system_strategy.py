import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volume_weighted_ma_digit_system_strategy(Strategy):
    def __init__(self):
        super(volume_weighted_ma_digit_system_strategy, self).__init__()
        self._vwma_period = self.Param("VwmaPeriod", 12) \
            .SetDisplay("VWMA Period", "Length of the VWMA indicator", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe for analysis", "Parameters")
        self._prev_vwma = None
        self._prev_slope = None

    @property
    def vwma_period(self):
        return self._vwma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_weighted_ma_digit_system_strategy, self).OnReseted()
        self._prev_vwma = None
        self._prev_slope = None

    def OnStarted(self, time):
        super(volume_weighted_ma_digit_system_strategy, self).OnStarted(time)
        self._prev_vwma = None
        self._prev_slope = None
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self.vwma_period
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
        if self._prev_vwma is not None:
            slope = vwma_value - self._prev_vwma
            if self._prev_slope is not None:
                turned_up = self._prev_slope <= 0 and slope > 0
                turned_down = self._prev_slope >= 0 and slope < 0
                if turned_up and self.Position <= 0:
                    self.BuyMarket()
                elif turned_down and self.Position >= 0:
                    self.SellMarket()
            self._prev_slope = slope
        self._prev_vwma = vwma_value

    def CreateClone(self):
        return volume_weighted_ma_digit_system_strategy()
