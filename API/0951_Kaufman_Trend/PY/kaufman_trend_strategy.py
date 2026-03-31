import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class kaufman_trend_strategy(Strategy):
    def __init__(self):
        super(kaufman_trend_strategy, self).__init__()
        self._trend_strength_entry = self.Param("TrendStrengthEntry", 80) \
            .SetDisplay("Trend Strength Entry", "Entry threshold", "Trend")
        self._trend_strength_exit = self.Param("TrendStrengthExit", 20) \
            .SetDisplay("Trend Strength Exit", "Exit threshold", "Trend")
        self._process_noise = self.Param("ProcessNoise", 0.01) \
            .SetDisplay("Process Noise", "Kalman process noise", "Kalman")
        self._measurement_noise = self.Param("MeasurementNoise", 500.0) \
            .SetDisplay("Measurement Noise", "Observation noise", "Kalman")
        self._osc_buffer_length = self.Param("OscBufferLength", 10) \
            .SetDisplay("Oscillator Buffer", "Bars for normalization", "Trend")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 300) \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._filtered_src = 0.0
        self._oscillator = 0.0
        self._p00 = 1.0
        self._p01 = 0.0
        self._p10 = 0.0
        self._p11 = 1.0
        self._osc_abs_average = 0.0
        self._warmup_count = 0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kaufman_trend_strategy, self).OnReseted()
        self._filtered_src = 0.0
        self._oscillator = 0.0
        self._p00 = 1.0
        self._p01 = 0.0
        self._p10 = 0.0
        self._p11 = 1.0
        self._osc_abs_average = 0.0
        self._warmup_count = 0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(kaufman_trend_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _update_kalman(self, price):
        pn = float(self._process_noise.Value)
        mn = float(self._measurement_noise.Value)
        if self._filtered_src == 0.0:
            self._filtered_src = price
            return
        self._filtered_src += self._oscillator
        p00p = self._p00 + self._p01 + self._p10 + self._p11 + pn
        p01p = self._p01 + self._p11
        p10p = self._p10 + self._p11
        p11p = self._p11 + pn
        s = p00p + mn
        if s == 0.0:
            return
        k0 = p00p / s
        k1 = p10p / s
        innovation = price - self._filtered_src
        self._filtered_src += k0 * innovation
        self._oscillator += k1 * innovation
        self._p00 = (1.0 - k0) * p00p
        self._p01 = (1.0 - k0) * p01p
        self._p10 = p10p - k1 * p00p
        self._p11 = p11p - k1 * p01p

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        close = float(candle.ClosePrice)
        self._update_kalman(close)
        abs_osc = abs(self._oscillator)
        osc_buf = self._osc_buffer_length.Value
        if self._warmup_count == 0:
            self._osc_abs_average = abs_osc
        else:
            alpha = 2.0 / (osc_buf + 1.0)
            self._osc_abs_average += (abs_osc - self._osc_abs_average) * alpha
        self._warmup_count += 1
        if self._osc_abs_average > 0.0:
            trend_strength = self._oscillator / self._osc_abs_average * 100.0
        else:
            trend_strength = 0.0
        if self._warmup_count < osc_buf:
            return
        entry_th = float(self._trend_strength_entry.Value)
        exit_th = float(self._trend_strength_exit.Value)
        price_above = close > self._filtered_src
        price_below = close < self._filtered_src
        strong_long = trend_strength >= entry_th
        strong_short = trend_strength <= -entry_th
        weak_long = trend_strength < exit_th
        weak_short = trend_strength > -exit_th
        if self.Position > 0 and weak_long:
            self.SellMarket()
            self._bars_since_signal = 0
        elif self.Position < 0 and weak_short:
            self.BuyMarket()
            self._bars_since_signal = 0
        if self.Position == 0 and self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value:
            if strong_long and price_above:
                self.BuyMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0
            elif strong_short and price_below:
                self.SellMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0

    def CreateClone(self):
        return kaufman_trend_strategy()
