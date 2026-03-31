import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multi_ema_crossover_strategy(Strategy):
    def __init__(self):
        super(multi_ema_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period", "Parameters")
        self._slow_length = self.Param("SlowLength", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period", "Parameters")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_ema_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_ema_crossover_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self.OnProcess).Start()

    def OnProcess(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self.Position > 0 and self._prev_fast >= self._prev_slow and fv < sv:
            self.SellMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        elif self.Position == 0 and self._cooldown_remaining == 0 and self._prev_fast <= self._prev_slow and fv > sv:
            self.BuyMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return multi_ema_crossover_strategy()
