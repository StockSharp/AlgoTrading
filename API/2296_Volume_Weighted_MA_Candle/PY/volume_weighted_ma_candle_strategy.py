import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volume_weighted_ma_candle_strategy(Strategy):
    def __init__(self):
        super(volume_weighted_ma_candle_strategy, self).__init__()
        self._vwma_period = self.Param("VwmaPeriod", 12) \
            .SetDisplay("VWMA Period", "Period for volume weighted moving average", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "Parameters")
        self._prev_vwma = None
        self._prev_color = None

    @property
    def vwma_period(self):
        return self._vwma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_weighted_ma_candle_strategy, self).OnReseted()
        self._prev_vwma = None
        self._prev_color = None

    def OnStarted(self, time):
        super(volume_weighted_ma_candle_strategy, self).OnStarted(time)
        self._prev_vwma = None
        self._prev_color = None
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
        close_price = float(candle.ClosePrice)
        price_above = close_price > vwma_value
        price_below = close_price < vwma_value
        rising = self._prev_vwma is not None and vwma_value > self._prev_vwma
        falling = self._prev_vwma is not None and vwma_value < self._prev_vwma
        if price_above and rising:
            current_color = 2
        elif price_below and falling:
            current_color = 0
        else:
            current_color = 1
        if self._prev_color is not None:
            if self._prev_color == 2 and current_color < 2 and self.Position >= 0:
                self.SellMarket()
            elif self._prev_color == 0 and current_color > 0 and self.Position <= 0:
                self.BuyMarket()
        self._prev_color = current_color
        self._prev_vwma = vwma_value

    def CreateClone(self):
        return volume_weighted_ma_candle_strategy()
