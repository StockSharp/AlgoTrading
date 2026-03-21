import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class exp_x_period_candle_strategy(Strategy):
    """
    ExpXPeriodCandle: Smoothed OHLC candle coloring strategy.
    Smooths price data with configurable MA types, trades on color changes.
    """

    def __init__(self):
        super(exp_x_period_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")
        self._period = self.Param("Period", 5) \
            .SetDisplay("Smoothing Window", "Depth of smoothing window", "Indicator")
        self._smoothing_length = self.Param("SmoothingLength", 3) \
            .SetDisplay("Smoothing Length", "Length used by smoother", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Shift", "Which completed candle to evaluate", "Trading")

        self._open_smoother = None
        self._high_smoother = None
        self._low_smoother = None
        self._close_smoother = None
        self._color_history = []
        self._smoothed_highs = deque()
        self._smoothed_lows = deque()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_x_period_candle_strategy, self).OnReseted()
        self._color_history = []
        self._smoothed_highs = deque()
        self._smoothed_lows = deque()
        self._open_smoother = None
        self._high_smoother = None
        self._low_smoother = None
        self._close_smoother = None

    def OnStarted(self, time):
        super(exp_x_period_candle_strategy, self).OnStarted(time)

        length = self._smoothing_length.Value
        self._open_smoother = _EmaSmoother(length)
        self._high_smoother = _EmaSmoother(length)
        self._low_smoother = _EmaSmoother(length)
        self._close_smoother = _EmaSmoother(length)

        self._color_history = []
        self._smoothed_highs = deque()
        self._smoothed_lows = deque()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_val = self._open_smoother.process(float(candle.OpenPrice))
        high_val = self._high_smoother.process(float(candle.HighPrice))
        low_val = self._low_smoother.process(float(candle.LowPrice))
        close_val = self._close_smoother.process(float(candle.ClosePrice))

        if open_val is None or high_val is None or low_val is None or close_val is None:
            return

        period = self._period.Value
        self._smoothed_highs.append(high_val)
        self._smoothed_lows.append(low_val)
        while len(self._smoothed_highs) > period:
            self._smoothed_highs.popleft()
        while len(self._smoothed_lows) > period:
            self._smoothed_lows.popleft()

        if len(self._smoothed_highs) < period or len(self._smoothed_lows) < period:
            return

        color = 0 if open_val <= close_val else 2
        self._color_history.append(color)

        sig_bar = self._signal_bar.Value
        max_hist = max(period * 4, sig_bar + 4)
        while len(self._color_history) > max_hist:
            self._color_history.pop(0)

        if len(self._color_history) < sig_bar + 1:
            return

        if len(self._color_history) <= sig_bar:
            return

        index0 = len(self._color_history) - sig_bar
        if index0 >= len(self._color_history):
            index0 = len(self._color_history) - 1
        index1 = index0 - 1
        if index1 < 0:
            return

        val0 = self._color_history[index0]
        val1 = self._color_history[index1]

        base_long = val1 < 1
        base_short = val1 > 1
        open_long = base_long and val0 > 0
        open_short = base_short and val0 < 2
        close_long = base_short
        close_short = base_long

        if close_long and self.Position > 0:
            self.SellMarket()

        if close_short and self.Position < 0:
            self.BuyMarket()

        if open_long and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif open_short and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return exp_x_period_candle_strategy()


class _EmaSmoother:
    def __init__(self, length):
        self._length = max(1, length)
        self._alpha = 2.0 / (self._length + 1.0)
        self._ema = None

    def process(self, value):
        if self._ema is None:
            self._ema = value
        else:
            self._ema += self._alpha * (value - self._ema)
        return self._ema
