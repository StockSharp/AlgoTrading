import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class double_zig_zag_strategy(Strategy):
    """Double ZigZag alignment strategy: fast and slow swing detectors for aligned breakout signals."""

    def __init__(self):
        super(double_zig_zag_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Lookback for the fast swing detector", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Lookback for the slow confirmation swing", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")

        self._highs = []
        self._lows = []
        self._fast_dir = 0
        self._slow_dir = 0

    @property
    def FastLength(self):
        return self._fast_length.Value
    @property
    def SlowLength(self):
        return self._slow_length.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(double_zig_zag_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._fast_dir = 0
        self._slow_dir = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        self._highs.append(h)
        self._lows.append(lo)

        max_buf = max(self.FastLength, self.SlowLength) + 1
        if len(self._highs) > max_buf:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < self.SlowLength:
            return

        fast_highest = self._get_max(self._highs, self.FastLength)
        fast_lowest = self._get_min(self._lows, self.FastLength)
        slow_highest = self._get_max(self._highs, self.SlowLength)
        slow_lowest = self._get_min(self._lows, self.SlowLength)

        prev_fast = self._fast_dir
        prev_slow = self._slow_dir

        if self._fast_dir <= 0 and h >= fast_highest:
            self._fast_dir = 1
        elif self._fast_dir >= 0 and lo <= fast_lowest:
            self._fast_dir = -1

        if self._slow_dir <= 0 and h >= slow_highest:
            self._slow_dir = 1
        elif self._slow_dir >= 0 and lo <= slow_lowest:
            self._slow_dir = -1

        fast_up = prev_fast <= 0 and self._fast_dir > 0
        fast_down = prev_fast >= 0 and self._fast_dir < 0
        slow_up = prev_slow <= 0 and self._slow_dir > 0

        if fast_up and (slow_up or self._slow_dir > 0):
            if self.Position <= 0:
                self.BuyMarket()
        elif fast_down and (prev_slow >= 0 and self._slow_dir < 0 or self._slow_dir < 0):
            if self.Position >= 0:
                self.SellMarket()

    def _get_max(self, data, length):
        start = max(0, len(data) - length)
        mx = data[start]
        for i in range(start + 1, len(data)):
            if data[i] > mx:
                mx = data[i]
        return mx

    def _get_min(self, data, length):
        start = max(0, len(data) - length)
        mn = data[start]
        for i in range(start + 1, len(data)):
            if data[i] < mn:
                mn = data[i]
        return mn

    def OnReseted(self):
        super(double_zig_zag_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._fast_dir = 0
        self._slow_dir = 0

    def CreateClone(self):
        return double_zig_zag_strategy()
