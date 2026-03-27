import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest, CandleIndicatorValue


class force_trend_strategy(Strategy):
    """Force Trend strategy: reacts to Fisher-like transform color changes to switch positions."""

    def __init__(self):
        super(force_trend_strategy, self).__init__()

        self._length = self.Param("Length", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "ForceTrend lookback length", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Number of finished bars to shift the signal", "Trading")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for ForceTrend calculations", "General")

        self._prev_force = 0.0
        self._prev_indicator = 0.0
        self._dir_history = []
        self._last_known_dir = 0

    @property
    def Length(self):
        return int(self._length.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)
    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value
    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value
    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value
    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(force_trend_strategy, self).OnStarted(time)

        self._prev_force = 0.0
        self._prev_indicator = 0.0
        self._dir_history = []
        self._last_known_dir = 0

        self._highest = Highest()
        self._highest.Length = self.Length
        self._lowest = Lowest()
        self._lowest.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h_val = float(self._highest.Process(CandleIndicatorValue(self._highest, candle)))
        l_val = float(self._lowest.Process(CandleIndicatorValue(self._lowest, candle)))

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        rng = h_val - l_val
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if rng != 0:
            avg = (h + lo) / 2.0
            normalized = (avg - l_val) / rng - 0.5
            force_val = 0.66 * normalized + 0.67 * self._prev_force
        else:
            force_val = 0.67 * self._prev_force - 0.33

        force_val = max(-0.999, min(0.999, force_val))

        denom = 1.0 - force_val
        if denom != 0:
            ratio = (force_val + 1.0) / denom
            indicator_val = math.log(ratio) / 2.0 + self._prev_indicator / 2.0
        else:
            indicator_val = self._prev_indicator / 2.0 + 0.5

        self._prev_force = force_val
        self._prev_indicator = indicator_val

        if indicator_val > 0:
            direction = 1
        elif indicator_val < 0:
            direction = -1
        else:
            direction = self._last_known_dir

        if direction != 0:
            self._last_known_dir = direction

        self._dir_history.append(direction)
        max_len = max(self.SignalBar + 2, 2)
        while len(self._dir_history) > max_len:
            self._dir_history.pop(0)

        cur_dir = self._get_dir(self.SignalBar)
        if cur_dir is None:
            return

        prev_dir = self._get_dir(self.SignalBar + 1)
        bullish = cur_dir > 0
        bearish = cur_dir < 0
        bullish_flip = bullish and prev_dir is not None and prev_dir <= 0
        bearish_flip = bearish and prev_dir is not None and prev_dir >= 0

        if bullish:
            should_buy = False
            if self.EnableShortExit and self.Position < 0:
                should_buy = True
            if self.EnableLongEntry and bullish_flip and self.Position <= 0:
                should_buy = True
            if should_buy:
                self.BuyMarket()
        elif bearish:
            should_sell = False
            if self.EnableLongExit and self.Position > 0:
                should_sell = True
            if self.EnableShortEntry and bearish_flip and self.Position >= 0:
                should_sell = True
            if should_sell:
                self.SellMarket()

    def _get_dir(self, offset):
        idx = len(self._dir_history) - 1 - offset
        if idx < 0:
            return None
        return self._dir_history[idx]

    def OnReseted(self):
        super(force_trend_strategy, self).OnReseted()
        self._prev_force = 0.0
        self._prev_indicator = 0.0
        self._dir_history = []
        self._last_known_dir = 0

    def CreateClone(self):
        return force_trend_strategy()
