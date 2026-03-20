import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class candels_high_open_strategy(Strategy):
    def __init__(self):
        super(candels_high_open_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._reverse_signals = self.Param("ReverseSignals", False)
        self._stop_level = self.Param("StopLevel", 50.0)
        self._take_level = self.Param("TakeLevel", 50.0)
        self._sar_step = self.Param("SarStep", 0.02)
        self._sar_max = self.Param("SarMax", 0.2)

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    @ReverseSignals.setter
    def ReverseSignals(self, value):
        self._reverse_signals.Value = value

    @property
    def StopLevel(self):
        return self._stop_level.Value

    @StopLevel.setter
    def StopLevel(self, value):
        self._stop_level.Value = value

    @property
    def TakeLevel(self):
        return self._take_level.Value

    @TakeLevel.setter
    def TakeLevel(self, value):
        self._take_level.Value = value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @SarStep.setter
    def SarStep(self, value):
        self._sar_step.Value = value

    @property
    def SarMax(self):
        return self._sar_max.Value

    @SarMax.setter
    def SarMax(self, value):
        self._sar_max.Value = value

    def OnStarted(self, time):
        super(candels_high_open_strategy, self).OnStarted(time)

        psar = ParabolicSar()
        psar.Acceleration = self.SarStep
        psar.AccelerationMax = self.SarMax
        psar.AccelerationStep = self.SarStep

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(psar, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeLevel, UnitTypes.Absolute),
            Unit(self.StopLevel, UnitTypes.Absolute))

    def ProcessCandle(self, candle, psar_value):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        psar_val = float(psar_value)

        if self.Position > 0 and price < psar_val:
            self.SellMarket()
            return
        if self.Position < 0 and price > psar_val:
            self.BuyMarket()
            return

        open_at_high = float(candle.OpenPrice) == float(candle.HighPrice)
        open_at_low = float(candle.OpenPrice) == float(candle.LowPrice)

        if self.ReverseSignals:
            tmp = open_at_high
            open_at_high = open_at_low
            open_at_low = tmp

        if open_at_low and self.Position <= 0:
            self.BuyMarket()
        elif open_at_high and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return candels_high_open_strategy()
