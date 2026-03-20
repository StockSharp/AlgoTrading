import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class is_connected_strategy(Strategy):
    def __init__(self):
        super(is_connected_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._acceleration = self.Param("Acceleration", 0.01)
        self._acceleration_max = self.Param("AccelerationMax", 0.1)

        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Acceleration(self):
        return self._acceleration.Value

    @Acceleration.setter
    def Acceleration(self, value):
        self._acceleration.Value = value

    @property
    def AccelerationMax(self):
        return self._acceleration_max.Value

    @AccelerationMax.setter
    def AccelerationMax(self, value):
        self._acceleration_max.Value = value

    def OnReseted(self):
        super(is_connected_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(is_connected_strategy, self).OnStarted(time)
        self._prev_sar = 0.0
        self._prev_close = 0.0
        self._has_prev = False

        sar = ParabolicSar()
        sar.Acceleration = self.Acceleration
        sar.AccelerationMax = self.AccelerationMax

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sar, self._process_candle).Start()

    def _process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sar_val = float(sar_value)

        if self._has_prev:
            if self._prev_close <= self._prev_sar and close > sar_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_close >= self._prev_sar and close < sar_val and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sar = sar_val
        self._has_prev = True

    def CreateClone(self):
        return is_connected_strategy()
