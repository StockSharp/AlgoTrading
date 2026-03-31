import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_indicator_trend_following_strategy(Strategy):
    def __init__(self):
        super(multi_indicator_trend_following_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._long_rsi_level = self.Param("LongRsiLevel", 55.0) \
            .SetDisplay("Long RSI", "Minimum RSI for long entries", "Indicators")
        self._short_rsi_level = self.Param("ShortRsiLevel", 45.0) \
            .SetDisplay("Short RSI", "Maximum RSI for short entries", "Indicators")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait after each crossover trade", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
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
        super(multi_indicator_trend_following_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(multi_indicator_trend_following_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_ma_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_ma_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self.OnProcess).Start()

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
        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        long_rsi = float(self._long_rsi_level.Value)
        short_rsi = float(self._short_rsi_level.Value)
        if self._cooldown_remaining == 0 and cross_up and rv > long_rsi and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        elif self._cooldown_remaining == 0 and cross_down and rv < short_rsi and self.Position >= 0:
            self.SellMarket()
            self._cooldown_remaining = self._signal_cooldown_bars.Value
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return multi_indicator_trend_following_strategy()
