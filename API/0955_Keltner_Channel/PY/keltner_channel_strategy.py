import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_channel_strategy(Strategy):
    def __init__(self):
        super(keltner_channel_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "EMA period for Keltner channels", "Keltner")
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetDisplay("Multiplier", "ATR multiplier for channel", "Keltner")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stops", "Risk Management")
        self._fast_ema_period = self.Param("FastEmaPeriod", 9) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Trend")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Trend")
        self._trend_ema_period = self.Param("TrendEmaPeriod", 50) \
            .SetDisplay("Trend EMA", "Trend filter EMA period", "Trend")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 1000) \
            .SetDisplay("Cooldown Bars", "Minimum bars between orders", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(keltner_channel_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(keltner_channel_strategy, self).OnStarted(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        kc = KeltnerChannels()
        kc.Length = self._length.Value
        kc.Multiplier = self._multiplier.Value
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._fast_ema_period.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._slow_ema_period.Value
        ema_trend = ExponentialMovingAverage()
        ema_trend.Length = self._trend_ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(kc, ema_fast, ema_slow, ema_trend, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kc)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, kc_value, ema_fast_value, ema_slow_value, ema_trend_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        upper = kc_value.Upper
        lower = kc_value.Lower
        middle = kc_value.Middle
        if upper is None or lower is None or middle is None:
            return
        upper = float(upper)
        lower = float(lower)
        middle = float(middle)
        ema_fast = float(ema_fast_value)
        ema_slow = float(ema_slow_value)
        ema_trend = float(ema_trend_value)
        atr_v = float(atr_value)
        price = float(candle.ClosePrice)
        atr_mult = float(self._atr_multiplier.Value)
        cross_under_lower = self._prev_close >= lower and price < lower
        cross_over_upper = self._prev_close <= upper and price > upper
        cross_over_ema = self._prev_fast <= self._prev_slow and ema_fast > ema_slow
        cross_under_ema = self._prev_fast >= self._prev_slow and ema_fast < ema_slow
        long_entry_kc = cross_under_lower
        short_entry_kc = cross_over_upper
        long_entry_trend = cross_over_ema and price > ema_trend
        short_entry_trend = cross_under_ema and price < ema_trend
        exit_long_kc = self._prev_close <= middle and price > middle
        exit_short_kc = self._prev_close >= middle and price < middle
        exit_long_trend = cross_under_ema
        exit_short_trend = cross_over_ema
        atr_distance = atr_v * atr_mult
        if self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_close = price
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            return
        if self.Position <= 0 and self._entries_executed < self._max_entries.Value and (long_entry_kc or long_entry_trend):
            self.BuyMarket()
            self._stop_price = price - atr_distance
            self._take_price = price + 2.0 * atr_distance
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif self.Position >= 0 and self._entries_executed < self._max_entries.Value and (short_entry_kc or short_entry_trend):
            self.SellMarket()
            self._stop_price = price + atr_distance
            self._take_price = price - 2.0 * atr_distance
            self._entries_executed += 1
            self._bars_since_signal = 0
        if self.Position > 0:
            if exit_long_kc or exit_long_trend or price <= self._stop_price or price >= self._take_price:
                self.SellMarket()
                self._bars_since_signal = 0
        elif self.Position < 0:
            if exit_short_kc or exit_short_trend or price >= self._stop_price or price <= self._take_price:
                self.BuyMarket()
                self._bars_since_signal = 0
        self._prev_close = price
        self._prev_fast = ema_fast
        self._prev_slow = ema_slow

    def CreateClone(self):
        return keltner_channel_strategy()
