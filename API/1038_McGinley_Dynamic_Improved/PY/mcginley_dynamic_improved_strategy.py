import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mcginley_dynamic_improved_strategy(Strategy):
    def __init__(self):
        super(mcginley_dynamic_improved_strategy, self).__init__()
        self._period = self.Param("Period", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "McGinley base period", "General")
        self._signal_threshold_percent = self.Param("SignalThresholdPercent", 0.25) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Threshold %", "Minimum distance from McGinley in percent", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._md_prev = None
        self._previous_diff = 0.0
        self._has_previous_diff = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mcginley_dynamic_improved_strategy, self).OnReseted()
        self._md_prev = None
        self._previous_diff = 0.0
        self._has_previous_diff = False
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(mcginley_dynamic_improved_strategy, self).OnStarted(time)
        self._md_prev = None
        self._previous_diff = 0.0
        self._has_previous_diff = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self.OnProcess).Start()

    def OnProcess(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed:
            return
        close = float(candle.ClosePrice)
        if self._md_prev is None:
            md = close
        else:
            prev = self._md_prev
            if prev == 0.0:
                prev = close
            k = 0.6
            period = float(self._period.Value)
            ratio = close / prev if prev != 0.0 else 1.0
            pw = math.pow(ratio, 4.0)
            denom = k * period * pw
            if denom == 0.0:
                denom = 1.0
            md = prev + (close - prev) / denom
        self._md_prev = md
        if close <= 0.0:
            return
        diff = (close - md) / close * 100.0
        threshold = float(self._signal_threshold_percent.Value)
        crossed_up = self._has_previous_diff and self._previous_diff <= threshold and diff > threshold
        crossed_down = self._has_previous_diff and self._previous_diff >= -threshold and diff < -threshold
        self._previous_diff = diff
        self._has_previous_diff = True
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal < cd:
            return
        if crossed_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif crossed_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return mcginley_dynamic_improved_strategy()
