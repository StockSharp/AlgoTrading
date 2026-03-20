import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_schaff_rsi_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_rsi_trend_cycle_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")
        self._fast_rsi = self.Param("FastRsi", 23) \
            .SetDisplay("Fast RSI", "Fast RSI period", "Parameters")
        self._slow_rsi = self.Param("SlowRsi", 50) \
            .SetDisplay("Slow RSI", "Slow RSI period", "Parameters")
        self._cycle = self.Param("Cycle", 10) \
            .SetDisplay("Cycle", "Cycle length", "Parameters")
        self._high_level = self.Param("HighLevel", 60) \
            .SetDisplay("High Level", "Upper level for the cycle", "Parameters")
        self._low_level = self.Param("LowLevel", -60) \
            .SetDisplay("Low Level", "Lower level for the cycle", "Parameters")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trading actions", "Trading")

        self._macd_history = []
        self._st_history = []
        self._fast_indicator = None
        self._slow_indicator = None
        self._prev_stc = 0.0
        self._prev_color = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastRsi(self):
        return self._fast_rsi.Value

    @FastRsi.setter
    def FastRsi(self, value):
        self._fast_rsi.Value = value

    @property
    def SlowRsi(self):
        return self._slow_rsi.Value

    @SlowRsi.setter
    def SlowRsi(self, value):
        self._slow_rsi.Value = value

    @property
    def Cycle(self):
        return self._cycle.Value

    @Cycle.setter
    def Cycle(self, value):
        self._cycle.Value = value

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

    def OnStarted(self, time):
        super(color_schaff_rsi_trend_cycle_strategy, self).OnStarted(time)

        self._fast_indicator = RelativeStrengthIndex()
        self._fast_indicator.Length = self.FastRsi
        self._slow_indicator = RelativeStrengthIndex()
        self._slow_indicator.Length = self.SlowRsi
        self._macd_history = []
        self._st_history = []
        self._prev_stc = 0.0
        self._prev_color = None
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close = float(candle.ClosePrice)
        fast_result = self._fast_indicator.Process(DecimalIndicatorValue(self._fast_indicator, close, candle.OpenTime, True))
        slow_result = self._slow_indicator.Process(DecimalIndicatorValue(self._slow_indicator, close, candle.OpenTime, True))
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return

        diff = float(fast_result.ToDecimal()) - float(slow_result.ToDecimal())
        cycle = self.Cycle

        self._add_value(self._macd_history, diff, cycle)
        if len(self._macd_history) < cycle:
            return

        macd_min, macd_max = self._get_min_max(self._macd_history)
        previous_st = self._st_history[-1] if len(self._st_history) > 0 else 0.0
        if macd_max == macd_min:
            st = previous_st
        else:
            st = (diff - macd_min) / (macd_max - macd_min) * 100.0
        self._add_value(self._st_history, st, cycle)

        st_min, st_max = self._get_min_max(self._st_history)
        if st_max == st_min:
            stc = self._prev_stc
        else:
            stc = (st - st_min) / (st_max - st_min) * 200.0 - 100.0
        delta = stc - self._prev_stc
        color = self._get_color(stc, delta)

        if self._prev_color is not None and self._cooldown_remaining == 0:
            if self._prev_color == 6 and color == 7 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif self._prev_color == 1 and color == 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif self.Position > 0 and color <= 1:
                self.SellMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif self.Position < 0 and color >= 6:
                self.BuyMarket()
                self._cooldown_remaining = self.SignalCooldownBars

        self._prev_color = color
        self._prev_stc = stc

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

    def _get_color(self, stc, delta):
        high = float(self.HighLevel)
        low = float(self.LowLevel)

        if stc > 0:
            if stc > high:
                return 7 if delta >= 0 else 6
            return 5 if delta >= 0 else 4

        if stc < low:
            return 0 if delta < 0 else 1

        return 2 if delta < 0 else 3

    def OnReseted(self):
        super(color_schaff_rsi_trend_cycle_strategy, self).OnReseted()
        self._fast_indicator = None
        self._slow_indicator = None
        self._macd_history = []
        self._st_history = []
        self._prev_stc = 0.0
        self._prev_color = None
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_schaff_rsi_trend_cycle_strategy()
