import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_trend_flow_strategy(Strategy):
    def __init__(self):
        super(adaptive_trend_flow_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_length = self.Param("FastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Fast EMA period", "Trend")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Slow EMA period", "Trend")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period for volatility", "Trend")
        self._sensitivity = self.Param("Sensitivity", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Sensitivity", "ATR multiplier for channel", "Trend")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_trend_flow_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_trend_flow_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = int(self._fast_length.Value)
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = int(self._slow_length.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast_v = float(fast_val)
        slow_v = float(slow_val)
        atr_v = float(atr_val)

        if self._prev_fast == 0:
            self._prev_fast = fast_v
            self._prev_slow = slow_v
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_fast = fast_v
            self._prev_slow = slow_v
            return

        sens = float(self._sensitivity.Value)
        cooldown = int(self._cooldown_bars.Value)
        channel = atr_v * sens

        crossed_above = self._prev_fast <= self._prev_slow + channel and fast_v > slow_v + channel
        crossed_below = self._prev_fast >= self._prev_slow - channel and fast_v < slow_v - channel

        if crossed_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif crossed_below and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_fast = fast_v
        self._prev_slow = slow_v

    def CreateClone(self):
        return adaptive_trend_flow_strategy()
