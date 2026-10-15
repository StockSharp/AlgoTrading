import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeVigorIndex
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_rvi_strategy(Strategy):

    def __init__(self):
        super(color_zerolag_rvi_strategy, self).__init__()

        self._rvi_length = self.Param("RviLength", 14) \
            .SetDisplay("RVI Length", "RVI calculation period", "Indicator")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "RVI signal line period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_rvi = None
        self._prev_signal = None

    @property
    def RviLength(self):
        return self._rvi_length.Value

    @RviLength.setter
    def RviLength(self, value):
        self._rvi_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(color_zerolag_rvi_strategy, self).OnStarted2(time)

        rvi = RelativeVigorIndex()
        rvi.Average.Length = self.RviLength
        rvi.Signal.Length = self.SignalLength

        self.SubscribeCandles(self.CandleType) \
            .BindEx(rvi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, rvi_value):
        if candle.State != CandleStates.Finished:
            return

        avg_raw = rvi_value.Average
        sig_raw = rvi_value.Signal
        if avg_raw is None or sig_raw is None:
            return

        rvi_val = float(avg_raw)
        signal_val = float(sig_raw)

        if self._prev_rvi is None or self._prev_signal is None:
            self._prev_rvi = rvi_val
            self._prev_signal = signal_val
            return

        cross_up = self._prev_rvi < self._prev_signal and rvi_val > signal_val
        cross_down = self._prev_rvi > self._prev_signal and rvi_val < signal_val

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rvi = rvi_val
        self._prev_signal = signal_val

    def OnReseted(self):
        super(color_zerolag_rvi_strategy, self).OnReseted()
        self._prev_rvi = None
        self._prev_signal = None

    def CreateClone(self):
        return color_zerolag_rvi_strategy()
