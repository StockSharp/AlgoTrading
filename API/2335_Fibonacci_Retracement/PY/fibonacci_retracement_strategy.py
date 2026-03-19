import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class fibonacci_retracement_strategy(Strategy):
    """
    Strategy based on Fibonacci retracement levels from rolling high/low pivots.
    Arms setups when price touches retracement, enters on midpoint cross.
    """

    BUFFER_SIZE = 256

    def __init__(self):
        super(fibonacci_retracement_strategy, self).__init__()
        self._zigzag_depth = self.Param("ZigzagDepth", 12) \
            .SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag")
        self._safety_buffer = self.Param("SafetyBuffer", 1) \
            .SetDisplay("Safety Buffer", "Min distance from level", "General")
        self._trend_precision = self.Param("TrendPrecision", 5) \
            .SetDisplay("Trend Precision", "Min pivot difference in points", "General")
        self._close_bar_pause = self.Param("CloseBarPause", 5) \
            .SetDisplay("Pause Bars", "Bars to wait after close", "Risk")
        self._take_profit_factor = self.Param("TakeProfitFactor", 0.2) \
            .SetDisplay("Take Profit Factor", "Extension from last extreme", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 15) \
            .SetDisplay("Stop Loss Points", "Distance to stop from entry", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")

        self._high_buffer = [0.0] * self.BUFFER_SIZE
        self._low_buffer = [0.0] * self.BUFFER_SIZE
        self._long_setup_armed = False
        self._short_setup_armed = False
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._bars_since_exit = 0
        self._buffer_index = 0
        self._buffer_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibonacci_retracement_strategy, self).OnReseted()
        self._high_buffer = [0.0] * self.BUFFER_SIZE
        self._low_buffer = [0.0] * self.BUFFER_SIZE
        self._long_setup_armed = False
        self._short_setup_armed = False
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._bars_since_exit = self._close_bar_pause.Value
        self._buffer_index = 0
        self._buffer_count = 0

    def OnStarted(self, time):
        super(fibonacci_retracement_strategy, self).OnStarted(time)

        self._high_buffer = [0.0] * self.BUFFER_SIZE
        self._low_buffer = [0.0] * self.BUFFER_SIZE
        self._bars_since_exit = self._close_bar_pause.Value
        self._buffer_index = 0
        self._buffer_count = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._push_bar(high, low)
        self._bars_since_exit += 1

        depth = min(self._zigzag_depth.Value, self.BUFFER_SIZE)
        if self._buffer_count < depth:
            self._prev_close = close
            return

        highest = self._get_highest(depth)
        lowest = self._get_lowest(depth)
        rng = highest - lowest
        precision = 0.01 * self._trend_precision.Value

        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._long_setup_armed = False
                self._bars_since_exit = 0
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._short_setup_armed = False
                self._bars_since_exit = 0

        if rng <= precision or not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            return

        midpoint = lowest + rng / 2.0
        long_retracement = highest - 0.618 * rng
        short_retracement = lowest + 0.618 * rng
        buf = 0.01 * self._safety_buffer.Value

        if self.Position == 0 and self._bars_since_exit >= self._close_bar_pause.Value:
            if close > midpoint:
                self._short_setup_armed = False
                if low <= long_retracement + buf:
                    self._long_setup_armed = True
                if self._long_setup_armed and self._cross_above(self._prev_close, close, midpoint, buf):
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = self._entry_price - 0.01 * self._stop_loss_points.Value
                    self._take_price = highest + self._take_profit_factor.Value * rng
                    self._long_setup_armed = False
                    self._bars_since_exit = 0
            elif close < midpoint:
                self._long_setup_armed = False
                if high >= short_retracement - buf:
                    self._short_setup_armed = True
                if self._short_setup_armed and self._cross_below(self._prev_close, close, midpoint, buf):
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = self._entry_price + 0.01 * self._stop_loss_points.Value
                    self._take_price = lowest - self._take_profit_factor.Value * rng
                    self._short_setup_armed = False
                    self._bars_since_exit = 0

        self._prev_close = close

    def _push_bar(self, high, low):
        self._high_buffer[self._buffer_index] = high
        self._low_buffer[self._buffer_index] = low
        self._buffer_index = (self._buffer_index + 1) % self.BUFFER_SIZE
        if self._buffer_count < self.BUFFER_SIZE:
            self._buffer_count += 1

    def _get_highest(self, depth):
        highest = -1e18
        count = min(depth, self._buffer_count)
        for i in range(count):
            idx = (self._buffer_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            if self._high_buffer[idx] > highest:
                highest = self._high_buffer[idx]
        return highest

    def _get_lowest(self, depth):
        lowest = 1e18
        count = min(depth, self._buffer_count)
        for i in range(count):
            idx = (self._buffer_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            if self._low_buffer[idx] < lowest:
                lowest = self._low_buffer[idx]
        return lowest

    @staticmethod
    def _cross_above(prev, current, level, buf):
        return current - level > buf and level - prev > buf

    @staticmethod
    def _cross_below(prev, current, level, buf):
        return prev - level > buf and level - current > buf

    def CreateClone(self):
        return fibonacci_retracement_strategy()
