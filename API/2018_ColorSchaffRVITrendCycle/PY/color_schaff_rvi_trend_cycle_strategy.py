import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from collections import deque


class color_schaff_rvi_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_rvi_trend_cycle_strategy, self).__init__()

        self._fast_rvi_length = self.Param("FastRviLength", 23) \
            .SetDisplay("Fast RVI Length", "Smoothing length for fast RVI", "General")
        self._slow_rvi_length = self.Param("SlowRviLength", 50) \
            .SetDisplay("Slow RVI Length", "Smoothing length for slow RVI", "General")
        self._cycle_length = self.Param("CycleLength", 10) \
            .SetDisplay("Cycle", "Length of the stochastic cycle", "General")
        self._high_level = self.Param("HighLevel", 60) \
            .SetDisplay("High Level", "Upper threshold", "General")
        self._low_level = self.Param("LowLevel", -60) \
            .SetDisplay("Low Level", "Lower threshold", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._recent_candles = []
        self._fast_window = deque()
        self._slow_window = deque()
        self._macd_history = []
        self._st_history = []
        self._fast_sum = 0.0
        self._slow_sum = 0.0
        self._st_ready = False
        self._stc_ready = False
        self._prev_st = 0.0
        self._prev_stc = 0.0
        self._cooldown_remaining = 0

    @property
    def FastRviLength(self):
        return self._fast_rvi_length.Value

    @FastRviLength.setter
    def FastRviLength(self, value):
        self._fast_rvi_length.Value = value

    @property
    def SlowRviLength(self):
        return self._slow_rvi_length.Value

    @SlowRviLength.setter
    def SlowRviLength(self, value):
        self._slow_rvi_length.Value = value

    @property
    def CycleLength(self):
        return self._cycle_length.Value

    @CycleLength.setter
    def CycleLength(self, value):
        self._cycle_length.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(color_schaff_rvi_trend_cycle_strategy, self).OnStarted2(time)

        self._recent_candles = []
        self._fast_window = deque()
        self._slow_window = deque()
        self._macd_history = []
        self._st_history = []
        self._fast_sum = 0.0
        self._slow_sum = 0.0
        self._st_ready = False
        self._stc_ready = False
        self._prev_st = 0.0
        self._prev_stc = 0.0
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        self._recent_candles.append(candle)
        if len(self._recent_candles) > 4:
            self._recent_candles.pop(0)

        if len(self._recent_candles) < 4:
            return

        raw_rvi = self._calculate_raw_rvi()
        fast_len = self.FastRviLength
        slow_len = self.SlowRviLength
        cycle = self.CycleLength

        self._fast_window.append(raw_rvi)
        self._fast_sum += raw_rvi
        while len(self._fast_window) > fast_len:
            self._fast_sum -= self._fast_window.popleft()

        self._slow_window.append(raw_rvi)
        self._slow_sum += raw_rvi
        while len(self._slow_window) > slow_len:
            self._slow_sum -= self._slow_window.popleft()

        if len(self._fast_window) < fast_len or len(self._slow_window) < slow_len:
            return

        fast = self._fast_sum / len(self._fast_window)
        slow = self._slow_sum / len(self._slow_window)
        macd = fast - slow

        self._add_value(self._macd_history, macd, cycle)
        if len(self._macd_history) < cycle:
            return

        min_macd, max_macd = self._get_min_max(self._macd_history)
        if max_macd == min_macd:
            st = self._prev_st
        else:
            st = (macd - min_macd) / (max_macd - min_macd) * 100.0
        if self._st_ready:
            st = 0.5 * (st - self._prev_st) + self._prev_st
        else:
            self._st_ready = True

        self._prev_st = st
        self._add_value(self._st_history, st, cycle)

        min_st, max_st = self._get_min_max(self._st_history)
        previous_stc = self._prev_stc
        if max_st == min_st:
            stc = previous_stc
        else:
            stc = (st - min_st) / (max_st - min_st) * 200.0 - 100.0
        if self._stc_ready:
            stc = 0.5 * (stc - previous_stc) + previous_stc
        else:
            self._stc_ready = True

        self._prev_stc = stc
        delta = stc - previous_stc
        high = float(self.HighLevel)
        low = float(self.LowLevel)

        long_entry = previous_stc <= high and stc > high and delta > 0
        short_entry = previous_stc >= low and stc < low and delta < 0
        long_exit = self.Position > 0 and stc < 0
        short_exit = self.Position < 0 and stc > 0

        if long_exit:
            self.SellMarket(self.Position)
            self._cooldown_remaining = self.SignalCooldownBars
        elif short_exit:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and long_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and short_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

    def _calculate_raw_rvi(self):
        c0 = self._recent_candles[0]
        c1 = self._recent_candles[1]
        c2 = self._recent_candles[2]
        c3 = self._recent_candles[3]
        value_up = ((float(c0.ClosePrice) - float(c0.OpenPrice)) +
                    2.0 * (float(c1.ClosePrice) - float(c1.OpenPrice)) +
                    2.0 * (float(c2.ClosePrice) - float(c2.OpenPrice)) +
                    (float(c3.ClosePrice) - float(c3.OpenPrice))) / 6.0
        value_dn = ((float(c0.HighPrice) - float(c0.LowPrice)) +
                    2.0 * (float(c1.HighPrice) - float(c1.LowPrice)) +
                    2.0 * (float(c2.HighPrice) - float(c2.LowPrice)) +
                    (float(c3.HighPrice) - float(c3.LowPrice))) / 6.0
        if value_dn == 0:
            return value_up
        return value_up / value_dn

    def _add_value(self, values, value, limit):
        values.append(value)
        if len(values) > limit:
            values.pop(0)

    def _get_min_max(self, values):
        min_val = values[0]
        max_val = values[0]
        for i in range(1, len(values)):
            val = values[i]
            if val < min_val:
                min_val = val
            if val > max_val:
                max_val = val
        return min_val, max_val

    def OnReseted(self):
        super(color_schaff_rvi_trend_cycle_strategy, self).OnReseted()
        self._recent_candles = []
        self._fast_window = deque()
        self._slow_window = deque()
        self._macd_history = []
        self._st_history = []
        self._fast_sum = 0.0
        self._slow_sum = 0.0
        self._st_ready = False
        self._stc_ready = False
        self._prev_st = 0.0
        self._prev_stc = 0.0
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_schaff_rvi_trend_cycle_strategy()
