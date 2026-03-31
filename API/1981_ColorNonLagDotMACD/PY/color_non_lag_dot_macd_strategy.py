import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class color_non_lag_dot_macd_strategy(Strategy):

    def __init__(self):
        super(color_non_lag_dot_macd_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast EMA period", "Indicator")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "Slow EMA period", "Indicator")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "Signal line period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_macd = None
        self._prev_signal = None

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

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
        super(color_non_lag_dot_macd_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastLength
        macd.Macd.LongMa.Length = self.SlowLength
        macd.SignalMa.Length = self.SignalLength

        self.SubscribeCandles(self.CandleType) \
            .BindEx(macd, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_raw = macd_value.Macd
        signal_raw = macd_value.Signal
        if macd_raw is None or signal_raw is None:
            return

        macd_line = float(macd_raw)
        signal_line = float(signal_raw)

        if self._prev_macd is not None and self._prev_signal is not None:
            cross_up = self._prev_macd <= self._prev_signal and macd_line > signal_line
            cross_down = self._prev_macd >= self._prev_signal and macd_line < signal_line

            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_macd = macd_line
        self._prev_signal = signal_line

    def OnReseted(self):
        super(color_non_lag_dot_macd_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_signal = None

    def CreateClone(self):
        return color_non_lag_dot_macd_strategy()
