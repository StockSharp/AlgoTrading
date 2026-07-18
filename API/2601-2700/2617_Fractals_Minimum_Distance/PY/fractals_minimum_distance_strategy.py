import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fractals_minimum_distance_strategy(Strategy):
    """Fractals minimum distance breakout strategy."""

    def __init__(self):
        super(fractals_minimum_distance_strategy, self).__init__()

        self._distance_pips = self.Param("DistancePips", 15) \
            .SetDisplay("Distance (pips)", "Minimum allowed gap between fractals", "Risk")
        self._signal_bar = self.Param("SignalBar", 3) \
            .SetDisplay("Signal bar offset", "How many closed bars ago the fractal must appear", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Primary candle series used for signals", "Data")

        self._prev_upper = None
        self._prev_lower = None
        self._highs = []
        self._lows = []
        self._buffer_count = 0
        self._window_size = 0
        self._signal_offset = 0
        self._pip_size = 0.0

    @property
    def DistancePips(self):
        return self._distance_pips.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        step = float(sec.PriceStep)
        # count decimals
        digits = 0
        temp = step
        while temp > 0 and temp < 1 and digits < 10:
            temp *= 10
            digits += 1
        if digits == 3 or digits == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(fractals_minimum_distance_strategy, self).OnStarted2(time)

        self._signal_offset = max(2, self.SignalBar)
        self._window_size = max(self._signal_offset + 3, 5)
        self._highs = [0.0] * self._window_size
        self._lows = [0.0] * self._window_size
        self._buffer_count = 0
        self._pip_size = self._calc_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished or self._window_size == 0:
            return

        ws = self._window_size
        for i in range(ws - 1):
            self._highs[i] = self._highs[i + 1]
            self._lows[i] = self._lows[i + 1]

        self._highs[ws - 1] = float(candle.HighPrice)
        self._lows[ws - 1] = float(candle.LowPrice)

        if self._buffer_count < ws:
            self._buffer_count += 1
        if self._buffer_count < ws:
            return

        ci = ws - 1 - self._signal_offset
        if ci < 2 or ci > ws - 3:
            return

        high = self._highs[ci]
        low = self._lows[ci]

        is_upper = (high > self._highs[ci - 1] and high > self._highs[ci - 2] and
                    high > self._highs[ci + 1] and high > self._highs[ci + 2])

        is_lower = (low < self._lows[ci - 1] and low < self._lows[ci - 2] and
                    low < self._lows[ci + 1] and low < self._lows[ci + 2])

        dist_threshold = self.DistancePips * self._pip_size

        if is_upper:
            self._prev_upper = high
            if self.Position > 0:
                self.SellMarket()
            if self._should_open(dist_threshold):
                self.SellMarket()

        if is_lower:
            self._prev_lower = low
            if self.Position < 0:
                self.BuyMarket()
            if self._should_open(dist_threshold):
                self.BuyMarket()

    def _should_open(self, threshold):
        if self.Volume <= 0:
            return False
        if self._prev_upper is None or self._prev_lower is None:
            return False
        return abs(self._prev_upper - self._prev_lower) >= abs(threshold)

    def OnReseted(self):
        super(fractals_minimum_distance_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None
        self._highs = []
        self._lows = []
        self._buffer_count = 0
        self._window_size = 0
        self._signal_offset = 0
        self._pip_size = 0.0

    def CreateClone(self):
        return fractals_minimum_distance_strategy()
