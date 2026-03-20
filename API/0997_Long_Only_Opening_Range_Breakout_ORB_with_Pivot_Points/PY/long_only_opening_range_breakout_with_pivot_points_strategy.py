import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class long_only_opening_range_breakout_with_pivot_points_strategy(Strategy):
    def __init__(self):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Working candle type", "General")
        self._range_bars = self.Param("RangeBars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Range Bars", "Lookback bars for channel", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 5.0) \
            .SetDisplay("Stop Loss %", "Initial stop loss percent", "Risk")
        self._pivot_length = self.Param("PivotLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Length", "Bars for pivot calculation", "Indicators")
        self._entry_price = 0.0
        self._sl0 = 0.0
        self._trail_stop = 0.0
        self._cooldown = 0
        self._prev_ready = False
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._pivot_high = 0.0
        self._pivot_low = 0.0
        self._pivot_close = 0.0
        self._pivot_bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._sl0 = 0.0
        self._trail_stop = 0.0
        self._cooldown = 0
        self._prev_ready = False
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._pivot_high = 0.0
        self._pivot_low = 0.0
        self._pivot_close = 0.0
        self._pivot_bar_count = 0

    def OnStarted(self, time):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._sl0 = 0.0
        self._trail_stop = 0.0
        self._cooldown = 0
        self._prev_ready = False
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._r1 = 0.0
        self._r2 = 0.0
        self._s1 = 0.0
        self._s2 = 0.0
        self._pivot_high = 0.0
        self._pivot_low = 0.0
        self._pivot_close = 0.0
        self._pivot_bar_count = 0
        self._highest = Highest()
        self._highest.Length = self._range_bars.Value
        self._lowest = Lowest()
        self._lowest.Length = self._range_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return
        hv = float(highest_val)
        lv = float(lowest_val)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._pivot_bar_count += 1
        if self._pivot_high == 0.0:
            self._pivot_high = high
        else:
            self._pivot_high = max(self._pivot_high, high)
        if self._pivot_low == 0.0:
            self._pivot_low = low
        else:
            self._pivot_low = min(self._pivot_low, low)
        self._pivot_close = close
        if self._pivot_bar_count >= self._pivot_length.Value:
            pivot = (self._pivot_high + self._pivot_low + self._pivot_close) / 3.0
            self._r1 = pivot + pivot - self._pivot_low
            self._r2 = pivot + (self._pivot_high - self._pivot_low)
            self._s1 = pivot + pivot - self._pivot_high
            self._s2 = pivot - (self._pivot_high - self._pivot_low)
            self._pivot_high = 0.0
            self._pivot_low = 0.0
            self._pivot_bar_count = 0
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_highest = hv
            self._prev_lowest = lv
            self._prev_ready = True
            return
        sl_pct = float(self._stop_loss_percent.Value) / 100.0
        if self._prev_ready and self.Position <= 0 and close > self._prev_highest and self._r1 > 0.0:
            self._entry_price = close
            self._sl0 = self._entry_price * (1.0 - sl_pct)
            self._trail_stop = 0.0
            self.BuyMarket()
            self._cooldown = 40
        elif self._prev_ready and self.Position >= 0 and close < self._prev_lowest and self._s1 > 0.0:
            self._entry_price = close
            self._sl0 = self._entry_price * (1.0 + sl_pct)
            self._trail_stop = 0.0
            self.SellMarket()
            self._cooldown = 40
        if self.Position > 0 and self._r1 > 0.0:
            if high > self._r2:
                self._trail_stop = max(self._trail_stop, self._r1)
            elif high > self._r1:
                self._trail_stop = max(self._trail_stop, hv)
            sl = max(self._sl0, self._trail_stop)
            if low <= sl:
                self.SellMarket()
                self._cooldown = 40
        if self.Position < 0 and self._s1 > 0.0:
            if low < self._s2:
                if self._trail_stop == 0.0:
                    self._trail_stop = self._s1
                else:
                    self._trail_stop = min(self._trail_stop, self._s1)
            elif low < self._s1:
                if self._trail_stop == 0.0:
                    self._trail_stop = lv
                else:
                    self._trail_stop = min(self._trail_stop, lv)
            sl = min(self._sl0, self._trail_stop) if self._trail_stop > 0.0 else self._sl0
            if high >= sl:
                self.BuyMarket()
                self._cooldown = 40
        self._prev_highest = hv
        self._prev_lowest = lv
        self._prev_ready = True

    def CreateClone(self):
        return long_only_opening_range_breakout_with_pivot_points_strategy()
