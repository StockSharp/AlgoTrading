import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class color_metro_duplex_strategy(Strategy):
    def __init__(self):
        super(color_metro_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicator")
        self._fast_step = self.Param("FastStep", 8) \
            .SetDisplay("Fast Step", "Step size for fast envelope", "Indicator")
        self._slow_step = self.Param("SlowStep", 24) \
            .SetDisplay("Slow Step", "Step size for slow envelope", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Trading")

        self._fast_min = None
        self._fast_max = None
        self._fast_trend = 0
        self._prev_fast_band = None
        self._slow_min = None
        self._slow_max = None
        self._slow_trend = 0
        self._prev_slow_band = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value
    @property
    def FastStep(self):
        return self._fast_step.Value
    @property
    def SlowStep(self):
        return self._slow_step.Value
    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(color_metro_duplex_strategy, self).OnReseted()
        self._fast_min = None
        self._fast_max = None
        self._fast_trend = 0
        self._prev_fast_band = None
        self._slow_min = None
        self._slow_max = None
        self._slow_trend = 0
        self._prev_slow_band = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(color_metro_duplex_strategy, self).OnStarted2(time)
        self._fast_min = None
        self._fast_max = None
        self._fast_trend = 0
        self._prev_fast_band = None
        self._slow_min = None
        self._slow_max = None
        self._slow_trend = 0
        self._prev_slow_band = None
        self._cooldown_remaining = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._on_process).Start()
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        rv = float(rsi_value)
        f_step = float(self.FastStep)
        s_step = float(self.SlowStep)

        fast_min_cand = rv - 2.0 * f_step
        fast_max_cand = rv + 2.0 * f_step

        if self._fast_min is None or self._fast_max is None:
            self._fast_min = fast_min_cand
            self._fast_max = fast_max_cand
            self._fast_trend = 0
            self._slow_min = rv - 2.0 * s_step
            self._slow_max = rv + 2.0 * s_step
            self._slow_trend = 0
            return

        if rv > self._fast_max:
            self._fast_trend = 1
        elif rv < self._fast_min:
            self._fast_trend = -1

        if self._fast_trend > 0 and fast_min_cand < self._fast_min:
            fast_min_cand = self._fast_min
        elif self._fast_trend < 0 and fast_max_cand > self._fast_max:
            fast_max_cand = self._fast_max

        slow_min_cand = rv - 2.0 * s_step
        slow_max_cand = rv + 2.0 * s_step

        if rv > self._slow_max:
            self._slow_trend = 1
        elif rv < self._slow_min:
            self._slow_trend = -1

        if self._slow_trend > 0 and slow_min_cand < self._slow_min:
            slow_min_cand = self._slow_min
        elif self._slow_trend < 0 and slow_max_cand > self._slow_max:
            slow_max_cand = self._slow_max

        fast_band = None
        if self._fast_trend > 0:
            fast_band = fast_min_cand + f_step
        elif self._fast_trend < 0:
            fast_band = fast_max_cand - f_step

        slow_band = None
        if self._slow_trend > 0:
            slow_band = slow_min_cand + s_step
        elif self._slow_trend < 0:
            slow_band = slow_max_cand - s_step

        self._fast_min = fast_min_cand
        self._fast_max = fast_max_cand
        self._slow_min = slow_min_cand
        self._slow_max = slow_max_cand

        if fast_band is None or slow_band is None:
            self._prev_fast_band = fast_band
            self._prev_slow_band = slow_band
            return

        if self._prev_fast_band is None or self._prev_slow_band is None:
            self._prev_fast_band = fast_band
            self._prev_slow_band = slow_band
            return

        up = fast_band
        down = slow_band
        prev_up = self._prev_fast_band
        prev_down = self._prev_slow_band

        self._prev_fast_band = fast_band
        self._prev_slow_band = slow_band

        long_open = prev_up > prev_down and up <= down
        short_open = prev_up < prev_down and up >= down

        if self._cooldown_remaining == 0 and long_open and self.Position == 0:
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and short_open and self.Position == 0:
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

    def CreateClone(self):
        return color_metro_duplex_strategy()
