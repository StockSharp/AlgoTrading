import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class extrapolated_pivot_connector_strategy(Strategy):
    """
    Extrapolated Pivot Connector strategy.
    Pivot highs and lows are bars that stay the extreme for PivotLength bars on each side. The resistance line connects the pivot
    high HighStart pivots back with the one HighEnd pivots back (0 is the latest) and is extrapolated to the current bar; the
    support line does the same with LowStart and LowEnd pivot lows. A close crossing above resistance goes long and a close
    crossing below support goes short, reversing an opposite position.
    """

    def __init__(self):
        super(extrapolated_pivot_connector_strategy, self).__init__()
        self._pivot_length = self.Param("PivotLength", 100).SetGreaterThanZero().SetDisplay("Pivot Length", "Bars on each side that a pivot must exceed", "Pivots")
        self._high_start = self.Param("HighStart", 1).SetNotNegative().SetDisplay("High Start", "Pivot high where the resistance line starts", "Pivots")
        self._high_end = self.Param("HighEnd", 0).SetNotNegative().SetDisplay("High End", "Pivot high where the resistance line ends", "Pivots")
        self._low_start = self.Param("LowStart", 1).SetNotNegative().SetDisplay("Low Start", "Pivot low where the support line starts", "Pivots")
        self._low_end = self.Param("LowEnd", 0).SetNotNegative().SetDisplay("Low End", "Pivot low where the support line ends", "Pivots")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._pivot_highs = []
        self._pivot_lows = []
        self._bar_index = 0
        self._prev_close = None
        self._prev_resistance = None
        self._prev_support = None

    def OnReseted(self):
        super(extrapolated_pivot_connector_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(extrapolated_pivot_connector_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        bar = self._bar_index
        self._bar_index += 1

        self._update_pivots(candle, bar)

        resistance = self._line_value(self._pivot_highs, self._high_start.Value, self._high_end.Value, bar)
        support = self._line_value(self._pivot_lows, self._low_start.Value, self._low_end.Value, bar)

        close = float(candle.ClosePrice)
        last_close = self._prev_close
        last_res = self._prev_resistance
        last_sup = self._prev_support

        self._prev_close = close
        self._prev_resistance = resistance
        self._prev_support = support

        if last_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_signal = resistance is not None and last_res is not None and last_close <= last_res and close > resistance
        short_signal = support is not None and last_sup is not None and last_close >= last_sup and close < support

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _update_pivots(self, candle, bar):
        length = self._pivot_length.Value
        size = length * 2 + 1

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > size:
            del self._highs[0]
            del self._lows[0]

        if len(self._highs) < size:
            return

        # The candidate is the middle bar: it is confirmed only after PivotLength newer bars.
        center_high = self._highs[length]
        center_low = self._lows[length]

        is_high = max(self._highs[:length]) < center_high and max(self._highs[length + 1:]) <= center_high
        is_low = min(self._lows[:length]) > center_low and min(self._lows[length + 1:]) >= center_low

        pivot_bar = bar - length
        keep = max(self._high_start.Value, self._high_end.Value, self._low_start.Value, self._low_end.Value) + 1

        if is_high:
            self._add_pivot(self._pivot_highs, pivot_bar, center_high, keep)
        if is_low:
            self._add_pivot(self._pivot_lows, pivot_bar, center_low, keep)

    def _add_pivot(self, pivots, bar, price, keep):
        pivots.insert(0, (bar, price))
        if len(pivots) > keep:
            pivots.pop()

    def _line_value(self, pivots, start, end, bar):
        if start >= len(pivots) or end >= len(pivots):
            return None

        start_bar, start_price = pivots[start]
        end_bar, end_price = pivots[end]

        if start_bar == end_bar:
            return None

        slope = (end_price - start_price) / (end_bar - start_bar)
        return end_price + slope * (bar - end_bar)

    def CreateClone(self):
        return extrapolated_pivot_connector_strategy()
