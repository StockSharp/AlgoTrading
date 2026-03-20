import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class color_schaff_mfi_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_mfi_trend_cycle_strategy, self).__init__()

        self._fast_mfi_period = self.Param("FastMfiPeriod", 23) \
            .SetDisplay("Fast MoneyFlowIndex", "Fast MoneyFlowIndex period", "Indicator")
        self._slow_mfi_period = self.Param("SlowMfiPeriod", 50) \
            .SetDisplay("Slow MoneyFlowIndex", "Slow MoneyFlowIndex period", "Indicator")
        self._cycle_length = self.Param("CycleLength", 10) \
            .SetDisplay("Cycle Length", "Cycle length for STC", "Indicator")
        self._high_level = self.Param("HighLevel", 60) \
            .SetDisplay("High Level", "Overbought threshold", "Indicator")
        self._low_level = self.Param("LowLevel", -60) \
            .SetDisplay("Low Level", "Oversold threshold", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")

        self._macd = None
        self._st = None
        self._index = 0
        self._values_count = 0
        self._st1 = False
        self._st2 = False
        self._prev_stc = 0.0
        self._prev_color = 0
        self._cooldown_remaining = 0

    @property
    def FastMfiPeriod(self):
        return self._fast_mfi_period.Value

    @FastMfiPeriod.setter
    def FastMfiPeriod(self, value):
        self._fast_mfi_period.Value = value

    @property
    def SlowMfiPeriod(self):
        return self._slow_mfi_period.Value

    @SlowMfiPeriod.setter
    def SlowMfiPeriod(self, value):
        self._slow_mfi_period.Value = value

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

    def OnStarted(self, time):
        super(color_schaff_mfi_trend_cycle_strategy, self).OnStarted(time)

        fast_mfi = MoneyFlowIndex()
        fast_mfi.Length = self.FastMfiPeriod
        slow_mfi = MoneyFlowIndex()
        slow_mfi.Length = self.SlowMfiPeriod

        cycle = self.CycleLength
        self._macd = [0.0] * cycle
        self._st = [0.0] * cycle
        self._index = 0
        self._values_count = 0
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_mfi, slow_mfi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_mfi_val, slow_mfi_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        color = self._calculate_color(float(fast_mfi_val), float(slow_mfi_val))

        if self._cooldown_remaining == 0 and self._prev_color == 6 and color == 7 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and self._prev_color == 1 and color == 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

        self._prev_color = color

    def _calculate_color(self, fast_mfi, slow_mfi):
        cycle = self.CycleLength
        diff = fast_mfi - slow_mfi
        self._macd[self._index] = diff

        count = self._values_count + 1 if self._values_count < cycle else cycle
        llv, hhv = self._get_min_max(self._macd, count)

        prev_index = (self._index - 1 + cycle) % cycle
        st_prev = self._st[prev_index]
        if hhv != llv:
            st = (diff - llv) / (hhv - llv) * 100.0
        else:
            st = st_prev
        if self._st1 and self._values_count > 0:
            st = 0.5 * (st - st_prev) + st_prev
        self._st1 = True
        self._st[self._index] = st

        llv2, hhv2 = self._get_min_max(self._st, count)
        stc_prev = self._prev_stc
        if hhv2 != llv2:
            stc = (st - llv2) / (hhv2 - llv2) * 200.0 - 100.0
        else:
            stc = stc_prev
        if self._st2 and self._values_count > 0:
            stc = 0.5 * (stc - stc_prev) + stc_prev
        self._st2 = True

        d_stc = stc - stc_prev
        self._prev_stc = stc

        self._index = (self._index + 1) % cycle
        if self._values_count < cycle:
            self._values_count += 1

        high = float(self.HighLevel)
        low = float(self.LowLevel)

        if stc > 0:
            if stc > high:
                color = 7 if d_stc >= 0 else 6
            else:
                color = 5 if d_stc >= 0 else 4
        else:
            if stc < low:
                color = 0 if d_stc < 0 else 1
            else:
                color = 2 if d_stc < 0 else 3

        return color

    def _get_min_max(self, buffer, count):
        min_val = buffer[0]
        max_val = buffer[0]
        for i in range(1, count):
            val = buffer[i]
            if val < min_val:
                min_val = val
            if val > max_val:
                max_val = val
        return min_val, max_val

    def OnReseted(self):
        super(color_schaff_mfi_trend_cycle_strategy, self).OnReseted()
        self._macd = None
        self._st = None
        self._index = 0
        self._values_count = 0
        self._st1 = False
        self._st2 = False
        self._prev_stc = 0.0
        self._prev_color = 0
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_schaff_mfi_trend_cycle_strategy()
