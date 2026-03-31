import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class vo_vix_devma_strategy(Strategy):
    def __init__(self):
        super(vo_vix_devma_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast Length", "Fast StdDev period", "DEVMA")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow Length", "Slow StdDev period", "DEVMA")
        self._stop_pct = self.Param("StopPct", 1.0) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_mult = self.Param("TpMult", 2.0) \
            .SetDisplay("TP Mult", "Take profit as multiple of stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 3) \
            .SetDisplay("Signal Cooldown", "Closed candles to wait before a new entry", "General")
        self._prev_fast_std = 0.0
        self._prev_slow_std = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0
        self._fast_std = None
        self._slow_std = None
        self._ema = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_mult(self):
        return self._tp_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(vo_vix_devma_strategy, self).OnReseted()
        self._prev_fast_std = 0.0
        self._prev_slow_std = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(vo_vix_devma_strategy, self).OnStarted2(time)
        self._fast_std = StandardDeviation()
        self._fast_std.Length = self.fast_length
        self._slow_std = StandardDeviation()
        self._slow_std.Length = self.slow_length
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.fast_length
        self._prev_fast_std = 0.0
        self._prev_slow_std = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._cooldown_remaining = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        t = candle.OpenTime
        price_input = DecimalIndicatorValue(self._fast_std, candle.ClosePrice, t)
        price_input.IsFinal = True
        fast_std_value = self._fast_std.Process(price_input)
        slow_input = DecimalIndicatorValue(self._slow_std, candle.ClosePrice, t)
        slow_input.IsFinal = True
        slow_std_value = self._slow_std.Process(slow_input)
        ema_input = DecimalIndicatorValue(self._ema, candle.ClosePrice, t)
        ema_input.IsFinal = True
        ema_value = self._ema.Process(ema_input)
        if not fast_std_value.IsFinal or not slow_std_value.IsFinal or not ema_value.IsFinal:
            return
        if not self._fast_std.IsFormed or not self._slow_std.IsFormed or not self._ema.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        fast_std_val = float(fast_std_value)
        slow_std_val = float(slow_std_value)
        ema_val = float(ema_value)
        close = float(candle.ClosePrice)
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            if close <= self._entry_price - self._stop_dist or close >= self._entry_price + self._stop_dist * float(self.tp_mult):
                self.SellMarket(self.Position)
                self._entry_price = 0.0
                self._stop_dist = 0.0
                self._cooldown_remaining = self.signal_cooldown_bars
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            if close >= self._entry_price + self._stop_dist or close <= self._entry_price - self._stop_dist * float(self.tp_mult):
                self.BuyMarket(-self.Position)
                self._entry_price = 0.0
                self._stop_dist = 0.0
                self._cooldown_remaining = self.signal_cooldown_bars
        if self._prev_fast_std == 0 or self._prev_slow_std == 0 or fast_std_val <= 0 or slow_std_val <= 0:
            self._prev_fast_std = fast_std_val
            self._prev_slow_std = slow_std_val
            return
        vol_expanding = fast_std_val > slow_std_val
        was_contracting = self._prev_fast_std <= self._prev_slow_std
        bull_cross = self._cooldown_remaining == 0 and was_contracting and vol_expanding and close > ema_val
        bear_cross = self._cooldown_remaining == 0 and was_contracting and vol_expanding and close < ema_val
        if bull_cross and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = close
            self._stop_dist = close * float(self.stop_pct) / 100.0
            self._cooldown_remaining = self.signal_cooldown_bars
        elif bear_cross and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = close
            self._stop_dist = close * float(self.stop_pct) / 100.0
            self._cooldown_remaining = self.signal_cooldown_bars
        self._prev_fast_std = fast_std_val
        self._prev_slow_std = slow_std_val

    def CreateClone(self):
        return vo_vix_devma_strategy()
