import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class gold_rsi_divergence_strategy(Strategy):
    def __init__(self):
        super(gold_rsi_divergence_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._lookback_left = self.Param("LookbackLeft", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Left", "Bars to the left of pivot", "Divergence")
        self._lookback_right = self.Param("LookbackRight", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Right", "Bars to the right of pivot", "Divergence")
        self._range_lower = self.Param("RangeLower", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Range Lower", "Minimum bars between pivots", "Divergence")
        self._range_upper = self.Param("RangeUpper", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Range Upper", "Maximum bars between pivots", "Divergence")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._rsi_buffer = []
        self._low_buffer = []
        self._high_buffer = []
        self._bar_index = 0
        self._last_rsi_low = None
        self._last_price_low = None
        self._last_pivot_low_index = -1
        self._last_rsi_high = None
        self._last_price_high = None
        self._last_pivot_high_index = -1
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(gold_rsi_divergence_strategy, self).OnReseted()
        self._initialize_buffers()
        self._bar_index = 0
        self._last_rsi_low = None
        self._last_price_low = None
        self._last_pivot_low_index = -1
        self._last_rsi_high = None
        self._last_price_high = None
        self._last_pivot_high_index = -1
        self._cooldown_remaining = 0

    def _initialize_buffers(self):
        length = max(1, self._lookback_left.Value + self._lookback_right.Value + 1)
        self._rsi_buffer = [0.0] * length
        self._low_buffer = [0.0] * length
        self._high_buffer = [0.0] * length
        self._buffer_count = 0

    def OnStarted(self, time):
        super(gold_rsi_divergence_strategy, self).OnStarted(time)
        self._initialize_buffers()
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _add_to_buffer(self, rsi, low, high):
        buf_len = len(self._rsi_buffer)
        if self._buffer_count < buf_len:
            self._rsi_buffer[self._buffer_count] = rsi
            self._low_buffer[self._buffer_count] = low
            self._high_buffer[self._buffer_count] = high
            self._buffer_count += 1
        else:
            self._rsi_buffer = self._rsi_buffer[1:] + [rsi]
            self._low_buffer = self._low_buffer[1:] + [low]
            self._high_buffer = self._high_buffer[1:] + [high]

    def _is_pivot_low(self, value):
        lr = self._lookback_right.Value
        for i in range(len(self._rsi_buffer)):
            if i == lr:
                continue
            if self._rsi_buffer[i] <= value:
                return False
        return True

    def _is_pivot_high(self, value):
        lr = self._lookback_right.Value
        for i in range(len(self._rsi_buffer)):
            if i == lr:
                continue
            if self._rsi_buffer[i] >= value:
                return False
        return True

    def _check_pivots(self, rsi_value, candle):
        lr = self._lookback_right.Value
        candidate_rsi = self._rsi_buffer[lr]
        candidate_bar = self._bar_index - lr
        if self._is_pivot_low(candidate_rsi):
            self._last_rsi_low = candidate_rsi
            self._last_price_low = self._low_buffer[lr]
            self._last_pivot_low_index = candidate_bar
        if self._is_pivot_high(candidate_rsi):
            self._last_rsi_high = candidate_rsi
            self._last_price_high = self._high_buffer[lr]
            self._last_pivot_high_index = candidate_bar

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_val)
        self._bar_index += 1
        self._add_to_buffer(rsi_value, float(candle.LowPrice), float(candle.HighPrice))
        if self._buffer_count < len(self._rsi_buffer):
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._check_pivots(rsi_value, candle)
            return
        lr = self._lookback_right.Value
        candidate_rsi = self._rsi_buffer[lr]
        candidate_low = self._low_buffer[lr]
        candidate_high = self._high_buffer[lr]
        candidate_bar = self._bar_index - lr
        range_lo = self._range_lower.Value
        range_up = self._range_upper.Value
        is_pivot_low = self._is_pivot_low(candidate_rsi)
        is_pivot_high = self._is_pivot_high(candidate_rsi)
        if is_pivot_low:
            in_range = (self._last_pivot_low_index >= 0 and
                        candidate_bar - self._last_pivot_low_index >= range_lo and
                        candidate_bar - self._last_pivot_low_index <= range_up)
            bullish_div = (in_range and
                           self._last_rsi_low is not None and
                           self._last_price_low is not None and
                           candidate_rsi > self._last_rsi_low and
                           candidate_low < self._last_price_low)
            if bullish_div and rsi_value < 40.0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            self._last_rsi_low = candidate_rsi
            self._last_price_low = candidate_low
            self._last_pivot_low_index = candidate_bar
        if is_pivot_high:
            in_range = (self._last_pivot_high_index >= 0 and
                        candidate_bar - self._last_pivot_high_index >= range_lo and
                        candidate_bar - self._last_pivot_high_index <= range_up)
            bearish_div = (in_range and
                           self._last_rsi_high is not None and
                           self._last_price_high is not None and
                           candidate_rsi < self._last_rsi_high and
                           candidate_high > self._last_price_high)
            if bearish_div and rsi_value > 60.0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            self._last_rsi_high = candidate_rsi
            self._last_price_high = candidate_high
            self._last_pivot_high_index = candidate_bar

    def CreateClone(self):
        return gold_rsi_divergence_strategy()
