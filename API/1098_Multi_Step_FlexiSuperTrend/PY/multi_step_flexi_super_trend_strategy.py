import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_step_flexi_super_trend_strategy(Strategy):
    def __init__(self):
        super(multi_step_flexi_super_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_length = self.Param("FastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_high = self.Param("RsiHigh", 55.0) \
            .SetDisplay("RSI High", "RSI overbought", "Indicators")
        self._rsi_low = self.Param("RsiLow", 45.0) \
            .SetDisplay("RSI Low", "RSI oversold", "Indicators")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_step_flexi_super_trend_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(multi_step_flexi_super_trend_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0
        self._fast = ExponentialMovingAverage()
        self._fast.Length = self._fast_length.Value
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self._slow_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_val)
        sv = float(slow_val)
        rv = float(rsi_val)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if not self._initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._initialized = True
            return
        rh = float(self._rsi_high.Value)
        rl = float(self._rsi_low.Value)
        if self._cooldown_remaining == 0 and self._prev_fast <= self._prev_slow and fv > sv and rv > rh and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        elif self._cooldown_remaining == 0 and self._prev_fast >= self._prev_slow and fv < sv and rv < rl and self.Position > 0:
            self.SellMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return multi_step_flexi_super_trend_strategy()
