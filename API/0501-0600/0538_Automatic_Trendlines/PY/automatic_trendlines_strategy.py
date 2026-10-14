import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class automatic_trendlines_strategy(Strategy):
    """
    Automatic trendlines strategy.
    A pivot high is a high above the LeftBars candles before it and the RightBars candles after it, and a pivot low is the
    mirror. The resistance line connects the last two pivot highs and the support line the last two pivot lows, both extended
    to the current candle. A close crossing above resistance goes long and a close crossing below support goes short,
    reversing an opposite position.
    """

    def __init__(self):
        super(automatic_trendlines_strategy, self).__init__()
        self._left_bars = self.Param("LeftBars", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Left Bars", "Candles before a pivot it must exceed", "Pivots")
        self._right_bars = self.Param("RightBars", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Right Bars", "Candles after a pivot it must exceed", "Pivots")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._window = []
        self._bar_index = -1
        self._last_high = None
        self._prev_high = None
        self._last_low = None
        self._prev_low = None
        self._prev_close = None
        self._prev_resistance = None
        self._prev_support = None

    def OnReseted(self):
        super(automatic_trendlines_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(automatic_trendlines_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        self._window.append((float(candle.HighPrice), float(candle.LowPrice)))

        size = self._left_bars.Value + self._right_bars.Value + 1
        if len(self._window) > size:
            self._window.pop(0)

        if len(self._window) == size:
            self._detect_pivots()

        close = float(candle.ClosePrice)
        resistance = self._line_value(self._prev_high, self._last_high)
        support = self._line_value(self._prev_low, self._last_low)

        prev_close = self._prev_close
        prev_resistance = self._prev_resistance
        prev_support = self._prev_support
        self._prev_close = close
        self._prev_resistance = resistance
        self._prev_support = support

        if prev_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = resistance is not None and prev_resistance is not None and prev_close <= prev_resistance and close > resistance
        cross_down = support is not None and prev_support is not None and prev_close >= prev_support and close < support

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _detect_pivots(self):
        left = self._left_bars.Value
        pivot_high, pivot_low = self._window[left]
        is_high = True
        is_low = True

        for i in range(len(self._window)):
            if i == left:
                continue
            if self._window[i][0] >= pivot_high:
                is_high = False
            if self._window[i][1] <= pivot_low:
                is_low = False

        pivot_index = self._bar_index - self._right_bars.Value

        if is_high:
            self._prev_high = self._last_high
            self._last_high = (pivot_index, pivot_high)

        if is_low:
            self._prev_low = self._last_low
            self._last_low = (pivot_index, pivot_low)

    def _line_value(self, first, second):
        if first is None or second is None or first[0] == second[0]:
            return None
        slope = (second[1] - first[1]) / (second[0] - first[0])
        return second[1] + slope * (self._bar_index - second[0])

    def CreateClone(self):
        return automatic_trendlines_strategy()
