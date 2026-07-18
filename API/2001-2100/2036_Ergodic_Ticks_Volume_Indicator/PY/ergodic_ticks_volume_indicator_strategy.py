import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ergodic_ticks_volume_indicator_strategy(Strategy):

    def __init__(self):
        super(ergodic_ticks_volume_indicator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._first_length = self.Param("FirstLength", 25) \
            .SetDisplay("First Length", "First smoothing length", "Indicator")
        self._second_length = self.Param("SecondLength", 13) \
            .SetDisplay("Second Length", "Second smoothing length", "Indicator")
        self._signal_length = self.Param("SignalLength", 7) \
            .SetDisplay("Signal Length", "Signal line length", "Indicator")

        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._prev_ready = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FirstLength(self):
        return self._first_length.Value

    @FirstLength.setter
    def FirstLength(self, value):
        self._first_length.Value = value

    @property
    def SecondLength(self):
        return self._second_length.Value

    @SecondLength.setter
    def SecondLength(self, value):
        self._second_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    def OnStarted2(self, time):
        super(ergodic_ticks_volume_indicator_strategy, self).OnStarted2(time)

        tsi = TrueStrengthIndex()
        tsi.FirstLength = self.FirstLength
        tsi.SecondLength = self.SecondLength
        tsi.SignalLength = self.SignalLength

        self.SubscribeCandles(self.CandleType) \
            .BindEx(tsi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        tsi_val = value.Tsi
        signal_val = value.Signal

        if tsi_val is None or signal_val is None:
            return

        tsi = float(tsi_val)
        signal = float(signal_val)

        if not self._prev_ready:
            self._prev_tsi = tsi
            self._prev_signal = signal
            self._prev_ready = True
            return

        if self._prev_tsi <= self._prev_signal and tsi > signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_tsi >= self._prev_signal and tsi < signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_tsi = tsi
        self._prev_signal = signal

    def OnReseted(self):
        super(ergodic_ticks_volume_indicator_strategy, self).OnReseted()
        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._prev_ready = False

    def CreateClone(self):
        return ergodic_ticks_volume_indicator_strategy()
