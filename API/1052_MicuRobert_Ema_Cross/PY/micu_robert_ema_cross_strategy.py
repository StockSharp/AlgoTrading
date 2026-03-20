import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ZeroLagExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class micu_robert_ema_cross_strategy(Strategy):
    def __init__(self):
        super(micu_robert_ema_cross_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast EMA length", "General")
        self._slow_length = self.Param("SlowLength", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow EMA length", "General")
        self._signal_threshold_percent = self.Param("SignalThresholdPercent", 0.08) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Threshold %", "Minimum EMA spread in percent", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(micu_robert_ema_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(micu_robert_ema_cross_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._fast_ma = ZeroLagExponentialMovingAverage()
        self._fast_ma.Length = self._fast_length.Value
        self._slow_ma = ZeroLagExponentialMovingAverage()
        self._slow_ma.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self.OnProcess).Start()

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast)
        sv = float(slow)
        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return
        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return
        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        close = float(candle.ClosePrice)
        if close <= 0.0:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        spread_percent = abs(fv - sv) / close * 100.0
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        thr = float(self._signal_threshold_percent.Value)
        if self._bars_from_signal >= cd and spread_percent >= thr and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and spread_percent >= thr and cross_down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return micu_robert_ema_cross_strategy()
