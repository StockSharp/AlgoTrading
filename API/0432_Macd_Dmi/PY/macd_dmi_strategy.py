import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DirectionalIndex, MovingAverageConvergenceDivergenceSignal, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_dmi_strategy(Strategy):
    """MACD + DMI Strategy.

    The strategy looks for bullish MACD crossovers when the positive
    Directional Movement (+DI) is above the negative one (-DI).
    A volatility stop based on Average True Range protects open positions.
    """

    def __init__(self):
        super(macd_dmi_strategy, self).__init__()

        # parameters
        self._candle_type = self.Param("CandleType", tf(60)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._dmi_length = self.Param("DmiLength", 14).SetDisplay(
            "DMI Length", "DMI period", "DMI"
        )
        self._adx_smoothing = self.Param("AdxSmoothing", 14).SetDisplay(
            "ADX Smoothing", "ADX smoothing period", "DMI"
        )
        self._vstop_length = self.Param("VstopLength", 20).SetDisplay(
            "Vstop Length", "Volatility Stop period", "Vstop"
        )
        self._vstop_multiplier = self.Param("VstopMultiplier", 2.0).SetDisplay(
            "Vstop Multiplier", "Volatility Stop multiplier", "Vstop"
        )

        # indicator placeholders
        self._dmi = None
        self._macd = None
        self._atr = None

        # volatility stop state
        self._vstop = 0.0
        self._uptrend = True
        self._max = 0.0
        self._min = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def dmi_length(self):
        return self._dmi_length.Value

    @dmi_length.setter
    def dmi_length(self, value):
        self._dmi_length.Value = value

    @property
    def adx_smoothing(self):
        return self._adx_smoothing.Value

    @adx_smoothing.setter
    def adx_smoothing(self, value):
        self._adx_smoothing.Value = value

    @property
    def vstop_length(self):
        return self._vstop_length.Value

    @vstop_length.setter
    def vstop_length(self, value):
        self._vstop_length.Value = value

    @property
    def vstop_multiplier(self):
        return self._vstop_multiplier.Value

    @vstop_multiplier.setter
    def vstop_multiplier(self, value):
        self._vstop_multiplier.Value = value

    def OnReseted(self):
        super(macd_dmi_strategy, self).OnReseted()
        self._max = 0.0
        self._min = 0.0
        self._vstop = 0.0
        self._uptrend = True

    def OnStarted(self, time):
        super(macd_dmi_strategy, self).OnStarted(time)

        self._dmi = DirectionalIndex()
        self._dmi.Length = self.dmi_length
        self._dmi.AdxSmoothing = self.adx_smoothing

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 26
        self._macd.SignalMa.Length = 9

        self._atr = AverageTrueRange()
        self._atr.Length = self.vstop_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._dmi, self._macd, self._atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, dmi_value, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._dmi.IsFormed or not self._macd.IsFormed or not self._atr.IsFormed:
            return

        dmi_data = dmi_value
        pos_dm = dmi_data.Plus
        neg_dm = dmi_data.Minus

        macd_data = macd_value
        macd_line = macd_data.Macd
        signal_line = macd_data.Signal

        prev = self._macd.GetValue[MovingAverageConvergenceDivergenceSignal](1)
        prev_macd = prev.Macd
        prev_signal = prev.Signal

        self._calculate_vstop(candle, float(atr_value))

        crossover = macd_line > signal_line and prev_macd <= prev_signal
        crossunder = macd_line < signal_line and prev_macd >= prev_signal

        entry_long = crossover and pos_dm > neg_dm
        exit_long = crossunder or candle.ClosePrice < self._vstop

        if entry_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif exit_long and self.Position > 0:
            self.ClosePosition()

    def _calculate_vstop(self, candle, atr_value):
        src = candle.ClosePrice
        atr_m = atr_value * self.vstop_multiplier

        if self._max == 0.0:
            self._max = src
            self._min = src
            self._vstop = src
            return

        self._max = max(self._max, src)
        self._min = min(self._min, src)

        prev_uptrend = self._uptrend
        if self._uptrend:
            self._vstop = max(self._vstop, self._max - atr_m)
        else:
            self._vstop = min(self._vstop, self._min + atr_m)

        self._uptrend = (src - self._vstop) >= 0

        if self._uptrend != prev_uptrend:
            self._max = src
            self._min = src
            self._vstop = self._max - atr_m if self._uptrend else self._min + atr_m

    def CreateClone(self):
        return macd_dmi_strategy()
