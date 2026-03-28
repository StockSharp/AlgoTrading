import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class de_mark_lines_strategy(Strategy):
    """
    DeMark trendline breakout strategy.
    Detects swing highs/lows (pivot points) and draws trendlines between them.
    Enters long on bullish breakout through downtrend line,
    enters short on bearish breakout through uptrend line.
    """

    def __init__(self):
        super(de_mark_lines_strategy, self).__init__()
        self._pivot_depth = self.Param("PivotDepth", 2) \
            .SetDisplay("Pivot depth", "Number of bars confirming a swing high/low", "Signals")
        self._min_bars_between = self.Param("MinBarsBetweenPivots", 5) \
            .SetDisplay("Min bars between pivots", "Prevents overlapping trendline anchors", "Signals")
        self._breakout_buffer = self.Param("BreakoutBuffer", 2.0) \
            .SetDisplay("Breakout buffer (pips)", "Extra distance beyond trendline before entering", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Primary timeframe for the analysis", "Data")

        self._high_buffer = []
        self._low_buffer = []
        self._time_buffer = []
        self._window_size = 0
        self._buffer_count = 0
        self._processed_bars = 0
        self._pip_size = 0.0

        self._prev_high = {"index": -1, "price": 0.0}
        self._recent_high = {"index": -1, "price": 0.0}
        self._prev_low = {"index": -1, "price": 0.0}
        self._recent_low = {"index": -1, "price": 0.0}

        self._last_long_signal = -1
        self._last_short_signal = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(de_mark_lines_strategy, self).OnReseted()
        self._high_buffer = []
        self._low_buffer = []
        self._time_buffer = []
        self._window_size = 0
        self._buffer_count = 0
        self._processed_bars = 0
        self._pip_size = 0.0
        self._prev_high = {"index": -1, "price": 0.0}
        self._recent_high = {"index": -1, "price": 0.0}
        self._prev_low = {"index": -1, "price": 0.0}
        self._recent_low = {"index": -1, "price": 0.0}
        self._last_long_signal = -1
        self._last_short_signal = -1

    def OnStarted(self, time):
        super(de_mark_lines_strategy, self).OnStarted(time)

        self._window_size = max(3, self._pivot_depth.Value * 2 + 1)
        self._high_buffer = [0.0] * self._window_size
        self._low_buffer = [0.0] * self._window_size
        self._time_buffer = [None] * self._window_size
        self._buffer_count = 0
        self._processed_bars = 0
        self._pip_size = self._calculate_pip_size()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or self._window_size == 0:
            return

        if self._buffer_count < self._window_size:
            self._high_buffer[self._buffer_count] = float(candle.HighPrice)
            self._low_buffer[self._buffer_count] = float(candle.LowPrice)
            self._time_buffer[self._buffer_count] = candle.OpenTime
            self._buffer_count += 1
            self._processed_bars += 1
            return

        for i in range(self._window_size - 1):
            self._high_buffer[i] = self._high_buffer[i + 1]
            self._low_buffer[i] = self._low_buffer[i + 1]
            self._time_buffer[i] = self._time_buffer[i + 1]

        self._high_buffer[self._window_size - 1] = float(candle.HighPrice)
        self._low_buffer[self._window_size - 1] = float(candle.LowPrice)
        self._time_buffer[self._window_size - 1] = candle.OpenTime
        self._processed_bars += 1

        center = self._window_size - 1 - self._pivot_depth.Value
        pivot_bar_index = self._processed_bars - self._pivot_depth.Value - 1
        pivot_high = self._high_buffer[center]
        pivot_low = self._low_buffer[center]

        if self._is_pivot_high(center):
            self._register_high_pivot(pivot_bar_index, pivot_high)

        if self._is_pivot_low(center):
            self._register_low_pivot(pivot_bar_index, pivot_low)

        self._evaluate_breakouts(candle)

    def _is_pivot_high(self, index):
        high = self._high_buffer[index]
        depth = self._pivot_depth.Value
        for offset in range(1, depth + 1):
            if high <= self._high_buffer[index - offset]:
                return False
            if high < self._high_buffer[index + offset]:
                return False
        return True

    def _is_pivot_low(self, index):
        low = self._low_buffer[index]
        depth = self._pivot_depth.Value
        for offset in range(1, depth + 1):
            if low >= self._low_buffer[index - offset]:
                return False
            if low > self._low_buffer[index + offset]:
                return False
        return True

    def _register_high_pivot(self, index, price):
        if self._recent_high["index"] >= 0 and index - self._recent_high["index"] < self._min_bars_between.Value:
            return
        self._prev_high = dict(self._recent_high)
        self._recent_high = {"index": index, "price": price}
        self._last_long_signal = -1

    def _register_low_pivot(self, index, price):
        if self._recent_low["index"] >= 0 and index - self._recent_low["index"] < self._min_bars_between.Value:
            return
        self._prev_low = dict(self._recent_low)
        self._recent_low = {"index": index, "price": price}
        self._last_short_signal = -1

    def _evaluate_breakouts(self, candle):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        current_index = self._processed_bars - 1
        price_buffer = self._breakout_buffer.Value * (self._pip_size if self._pip_size > 0 else 1.0)
        close = float(candle.ClosePrice)

        if (self._recent_high["index"] >= 0 and self._prev_high["index"] >= 0
                and current_index != self._last_long_signal):
            resistance = self._calc_trend_value(self._prev_high, self._recent_high, current_index)
            if close > resistance + price_buffer and self.Position <= 0:
                volume = self.Volume + abs(self.Position)
                if volume > 0:
                    self.BuyMarket(volume)
                    self._last_long_signal = current_index

        if (self._recent_low["index"] >= 0 and self._prev_low["index"] >= 0
                and current_index != self._last_short_signal):
            support = self._calc_trend_value(self._prev_low, self._recent_low, current_index)
            if close < support - price_buffer and self.Position >= 0:
                volume = self.Volume + abs(self.Position)
                if volume > 0:
                    self.SellMarket(volume)
                    self._last_short_signal = current_index

    def _calc_trend_value(self, older, newer, current_index):
        index_diff = newer["index"] - older["index"]
        if index_diff == 0:
            return newer["price"]
        slope = (newer["price"] - older["price"]) / float(index_diff)
        offset = current_index - newer["index"]
        return newer["price"] + slope * offset

    def _calculate_pip_size(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            return 1.0

        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)

        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def CreateClone(self):
        return de_mark_lines_strategy()
