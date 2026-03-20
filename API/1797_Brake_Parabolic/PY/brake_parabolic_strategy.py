import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class brake_parabolic_strategy(Strategy):
    def __init__(self):
        super(brake_parabolic_strategy, self).__init__()
        self._sar_step = self.Param("SarStep", 0.02)             .SetDisplay("SAR Step", "Acceleration step", "Indicators")
        self._sar_max = self.Param("SarMax", 0.2)             .SetDisplay("SAR Max", "Maximum acceleration", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))             .SetDisplay("Candle Type", "Candle type", "General")
        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._has_prev = False

    @property
    def sar_step(self):
        return self._sar_step.Value

    @property
    def sar_max(self):
        return self._sar_max.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(brake_parabolic_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sar = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(brake_parabolic_strategy, self).OnStarted(time)
        sar = ParabolicSar()
        sar.AccelerationStep = self.sar_step
        sar.AccelerationMax = self.sar_max
        self.SubscribeCandles(self.candle_type).Bind(sar, self.process_candle).Start()

    def process_candle(self, candle, sar_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sv = float(sar_val)

        if not self._has_prev:
            self._prev_close = close
            self._prev_sar = sv
            self._has_prev = True
            return

        cross_up = self._prev_close <= self._prev_sar and close > sv
        cross_down = self._prev_close >= self._prev_sar and close < sv

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_sar = sv

    def CreateClone(self):
        return brake_parabolic_strategy()
