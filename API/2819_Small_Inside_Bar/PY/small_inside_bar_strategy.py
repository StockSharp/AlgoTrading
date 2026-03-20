import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class small_inside_bar_strategy(Strategy):
    def __init__(self):
        super(small_inside_bar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._range_ratio_threshold = self.Param("RangeRatioThreshold", 2.25)
        self._enable_long = self.Param("EnableLong", True)
        self._enable_short = self.Param("EnableShort", True)
        self._reverse_signals = self.Param("ReverseSignals", False)

        self._prev_candle_high = None
        self._prev_candle_low = None
        self._prev_candle_open = None
        self._prev_candle_close = None
        self._two_back_high = None
        self._two_back_low = None
        self._two_back_open = None
        self._two_back_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(small_inside_bar_strategy, self).OnStarted(time)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_candle_high is None:
            self._cache_prev(candle)
            return

        if self._two_back_high is None:
            self._shift_history(candle)
            return

        inside_high = self._prev_candle_high
        inside_low = self._prev_candle_low
        mother_high = self._two_back_high
        mother_low = self._two_back_low

        if inside_high <= inside_low or mother_high <= mother_low:
            self._shift_history(candle)
            return

        if not (inside_high < mother_high and inside_low > mother_low):
            self._shift_history(candle)
            return

        inside_range = inside_high - inside_low
        mother_range = mother_high - mother_low
        ratio = mother_range / inside_range if inside_range != 0 else 1e18

        if ratio <= self._range_ratio_threshold.Value:
            self._shift_history(candle)
            return

        midpoint = (mother_high + mother_low) / 2.0

        bullish_inside = (self._prev_candle_close > self._prev_candle_open and
                          inside_high < midpoint and
                          self._two_back_close < self._two_back_open)
        bearish_inside = (self._prev_candle_close < self._prev_candle_open and
                          inside_low < midpoint and
                          self._two_back_close > self._two_back_open)

        if self._reverse_signals.Value:
            bullish_inside, bearish_inside = bearish_inside, bullish_inside

        should_open_long = bullish_inside and self._enable_long.Value
        should_open_short = bearish_inside and self._enable_short.Value

        if should_open_long:
            volume = self._calc_order_volume(True)
            if volume > 0:
                self.BuyMarket(volume)

        if should_open_short:
            volume = self._calc_order_volume(False)
            if volume > 0:
                self.SellMarket(volume)

        self._shift_history(candle)

    def _calc_order_volume(self, is_long):
        base_volume = float(self.Volume)
        if base_volume <= 0:
            return 0

        position = self.Position

        if is_long:
            if position < 0:
                base_volume += abs(position)
        else:
            if position > 0:
                base_volume += abs(position)

        return base_volume

    def _cache_prev(self, candle):
        self._prev_candle_high = float(candle.HighPrice)
        self._prev_candle_low = float(candle.LowPrice)
        self._prev_candle_open = float(candle.OpenPrice)
        self._prev_candle_close = float(candle.ClosePrice)

    def _shift_history(self, candle):
        self._two_back_high = self._prev_candle_high
        self._two_back_low = self._prev_candle_low
        self._two_back_open = self._prev_candle_open
        self._two_back_close = self._prev_candle_close
        self._cache_prev(candle)

    def OnReseted(self):
        super(small_inside_bar_strategy, self).OnReseted()
        self._prev_candle_high = None
        self._prev_candle_low = None
        self._prev_candle_open = None
        self._prev_candle_close = None
        self._two_back_high = None
        self._two_back_low = None
        self._two_back_open = None
        self._two_back_close = None

    def CreateClone(self):
        return small_inside_bar_strategy()
