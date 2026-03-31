import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_hull_trend_strategy(Strategy):
    def __init__(self):
        super(exp_hull_trend_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Hull Length", "Base period for Hull calculation", "Indicator")
        self._min_spread_percent = self.Param("MinSpreadPercent", 0.0015) \
            .SetDisplay("Min Spread %", "Minimum normalized spread between Hull lines", "Signal")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for processing", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0
        self._final_buffer = []
        self._final_length = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def min_spread_percent(self):
        return self._min_spread_percent.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_hull_trend_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown_remaining = 0
        self._final_buffer = []
        self._final_length = 0

    def OnStarted2(self, time):
        super(exp_hull_trend_strategy, self).OnStarted2(time)
        self._final_length = max(1, int(Math.Sqrt(self.length)))
        wma_half = WeightedMovingAverage()
        wma_half.Length = max(1, self.length // 2)
        wma_full = WeightedMovingAverage()
        wma_full.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma_half, wma_full, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _calc_wma(self, new_val):
        self._final_buffer.append(new_val)
        if len(self._final_buffer) > self._final_length:
            self._final_buffer.pop(0)
        if len(self._final_buffer) < self._final_length:
            return new_val
        sum_weight = 0.0
        sum_val = 0.0
        for i in range(len(self._final_buffer)):
            w = i + 1
            sum_val += self._final_buffer[i] * w
            sum_weight += w
        return sum_val / sum_weight

    def process_candle(self, candle, half_value, full_value):
        if candle.State != CandleStates.Finished:
            return
        half_value = float(half_value)
        full_value = float(full_value)
        fast = 2.0 * half_value - full_value
        slow = self._calc_wma(fast)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        spread = abs(fast - slow) / max(abs(slow), 1.0)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        min_spread = float(self.min_spread_percent)
        if cross_up and spread >= min_spread and self._cooldown_remaining == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and spread >= min_spread and self._cooldown_remaining == 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return exp_hull_trend_strategy()
