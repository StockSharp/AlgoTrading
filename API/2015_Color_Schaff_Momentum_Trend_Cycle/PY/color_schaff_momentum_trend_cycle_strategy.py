import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_schaff_momentum_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_momentum_trend_cycle_strategy, self).__init__()

        self._fast_momentum_length = self.Param("FastMomentum", 23) \
            .SetDisplay("Fast Momentum", "Fast momentum length", "Indicator")
        self._slow_momentum_length = self.Param("SlowMomentum", 50) \
            .SetDisplay("Slow Momentum", "Slow momentum length", "Indicator")
        self._cycle = self.Param("Cycle", 10) \
            .SetDisplay("Cycle", "Cycle length", "Indicator")
        self._high_level = self.Param("HighLevel", 60) \
            .SetDisplay("High Level", "Upper threshold", "Indicator")
        self._low_level = self.Param("LowLevel", -60) \
            .SetDisplay("Low Level", "Lower threshold", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Long", "Allow long entries", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Short", "Allow short entries", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")

        self._macd_history = []
        self._st_history = []
        self._fast_momentum = None
        self._slow_momentum = None
        self._prev_stc = 0.0
        self._prev_color = None
        self._cooldown_remaining = 0

    @property
    def FastMomentum(self):
        return self._fast_momentum_length.Value

    @FastMomentum.setter
    def FastMomentum(self, value):
        self._fast_momentum_length.Value = value

    @property
    def SlowMomentum(self):
        return self._slow_momentum_length.Value

    @SlowMomentum.setter
    def SlowMomentum(self, value):
        self._slow_momentum_length.Value = value

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

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value

    @BuyPosClose.setter
    def BuyPosClose(self, value):
        self._buy_pos_close.Value = value

    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    @SellPosClose.setter
    def SellPosClose(self, value):
        self._sell_pos_close.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(color_schaff_momentum_trend_cycle_strategy, self).OnStarted2(time)

        self._fast_momentum = Momentum()
        self._fast_momentum.Length = self.FastMomentum
        self._slow_momentum = Momentum()
        self._slow_momentum.Length = self.SlowMomentum
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
        fast_input = DecimalIndicatorValue(self._fast_momentum, candle.ClosePrice, candle.OpenTime)
        fast_input.IsFinal = True
        fast_result = self._fast_momentum.Process(fast_input)
        slow_input = DecimalIndicatorValue(self._slow_momentum, candle.ClosePrice, candle.OpenTime)
        slow_input.IsFinal = True
        slow_result = self._slow_momentum.Process(slow_input)
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return

        diff = float(fast_result) - float(slow_result)
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
            if self._prev_color == 6 and color == 7 and self.BuyPosOpen and self.Position <= 0:
                if self.Position < 0 and self.SellPosClose:
                    self.BuyMarket(abs(self.Position))
                self.BuyMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif self._prev_color == 1 and color == 0 and self.SellPosOpen and self.Position >= 0:
                if self.Position > 0 and self.BuyPosClose:
                    self.SellMarket(self.Position)
                self.SellMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif self.Position > 0 and self.BuyPosClose and color <= 1:
                self.SellMarket(self.Position)
                self._cooldown_remaining = self.SignalCooldownBars
            elif self.Position < 0 and self.SellPosClose and color >= 6:
                self.BuyMarket(abs(self.Position))
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
        super(color_schaff_momentum_trend_cycle_strategy, self).OnReseted()
        self._fast_momentum = None
        self._slow_momentum = None
        self._macd_history = []
        self._st_history = []
        self._prev_stc = 0.0
        self._prev_color = None
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_schaff_momentum_trend_cycle_strategy()
