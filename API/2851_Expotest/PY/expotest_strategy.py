import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class expotest_strategy(Strategy):
    def __init__(self):
        super(expotest_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type for signal generation", "General")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators")
        self._sar_maximum = self.Param("SarMaximum", 0.2) \
            .SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators")

        self._prev_sar_below = False
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @property
    def SarMaximum(self):
        return self._sar_maximum.Value

    def OnReseted(self):
        super(expotest_strategy, self).OnReseted()
        self._prev_sar_below = False
        self._initialized = False

    def OnStarted(self, time):
        super(expotest_strategy, self).OnStarted(time)

        self._prev_sar_below = False
        self._initialized = False

        sar = ParabolicSar()
        sar.Acceleration = self.SarStep
        sar.AccelerationMax = self.SarMaximum

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(sar, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sar_value)
        sar_below = sv < float(candle.ClosePrice)

        if not self._initialized:
            self._prev_sar_below = sar_below
            self._initialized = True
            return

        if sar_below and not self._prev_sar_below and self.Position <= 0:
            self.BuyMarket()
        elif not sar_below and self._prev_sar_below and self.Position >= 0:
            self.SellMarket()

        self._prev_sar_below = sar_below

    def CreateClone(self):
        return expotest_strategy()
