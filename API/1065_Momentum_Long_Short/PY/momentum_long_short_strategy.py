import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class momentum_long_short_strategy(Strategy):
    def __init__(self):
        super(momentum_long_short_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 20) \
            .SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 50) \
            .SetGreaterThanZero()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero()
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
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
        super(momentum_long_short_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(momentum_long_short_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._ma_fast = ExponentialMovingAverage()
        self._ma_fast.Length = self._fast_length.Value
        self._ma_slow = ExponentialMovingAverage()
        self._ma_slow.Length = self._slow_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma_fast, self._ma_slow, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, ma_fast, ma_slow, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(ma_fast)
        sv = float(ma_slow)
        rv = float(rsi_value)
        self._bars_from_signal += 1
        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return
        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and cross_up and rv <= 65.0 and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and cross_down and rv >= 35.0 and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return momentum_long_short_strategy()
