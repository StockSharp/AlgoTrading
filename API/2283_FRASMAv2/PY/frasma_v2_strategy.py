import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import FractalDimension
from StockSharp.Algo.Strategies import Strategy


class frasma_v2_strategy(Strategy):
    def __init__(self):
        super(frasma_v2_strategy, self).__init__()
        self._period = self.Param("Period", 30) \
            .SetDisplay("Period", "FRAMA calculation period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._is_first = True
        self._prev_frama = 0.0
        self._prev_color = 1

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(frasma_v2_strategy, self).OnReseted()
        self._is_first = True
        self._prev_frama = 0.0
        self._prev_color = 1

    def OnStarted2(self, time):
        super(frasma_v2_strategy, self).OnStarted2(time)
        self._is_first = True
        self._prev_frama = 0.0
        self._prev_color = 1
        fdi = FractalDimension()
        fdi.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fdi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fdi_value):
        if candle.State != CandleStates.Finished:
            return
        if not fdi_value.IsFinal:
            return
        fdi = float(fdi_value)
        alpha = math.exp(-4.6 * (fdi - 1.0))
        alpha = max(0.01, min(1.0, alpha))
        price = float(candle.ClosePrice)
        if self._is_first:
            frama = price
        else:
            frama = alpha * price + (1.0 - alpha) * self._prev_frama
        if self._is_first:
            color = 1
            self._is_first = False
        elif frama > self._prev_frama:
            color = 0
        elif frama < self._prev_frama:
            color = 2
        else:
            color = 1
        if self._prev_color == 0 and color > 0 and self.Position >= 0:
            self.SellMarket()
        elif self._prev_color == 2 and color < 2 and self.Position <= 0:
            self.BuyMarket()
        self._prev_frama = frama
        self._prev_color = color

    def CreateClone(self):
        return frasma_v2_strategy()
