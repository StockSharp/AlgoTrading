import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class vietnamese_3x_supertrend_strategy(Strategy):
    """Vietnamese 3x SuperTrend Strategy."""

    def __init__(self):
        super(vietnamese_3x_supertrend_strategy, self).__init__()

        self._fast_atr_length = self.Param("FastAtrLength", 10) \
            .SetDisplay("Fast ATR Length", "ATR length for fast SuperTrend", "SuperTrend")
        self._fast_multiplier = self.Param("FastMultiplier", 1.0) \
            .SetDisplay("Fast Multiplier", "ATR multiplier for fast SuperTrend", "SuperTrend")
        self._medium_atr_length = self.Param("MediumAtrLength", 11) \
            .SetDisplay("Medium ATR Length", "ATR length for medium SuperTrend", "SuperTrend")
        self._medium_multiplier = self.Param("MediumMultiplier", 2.0) \
            .SetDisplay("Medium Multiplier", "ATR multiplier for medium SuperTrend", "SuperTrend")
        self._slow_atr_length = self.Param("SlowAtrLength", 12) \
            .SetDisplay("Slow ATR Length", "ATR length for slow SuperTrend", "SuperTrend")
        self._slow_multiplier = self.Param("SlowMultiplier", 3.0) \
            .SetDisplay("Slow Multiplier", "ATR multiplier for slow SuperTrend", "SuperTrend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._highest_green = 0.0
        self._break_even_active = False
        self._avg_entry_price = 0.0
        self._entry_count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vietnamese_3x_supertrend_strategy, self).OnReseted()
        self._highest_green = 0.0
        self._break_even_active = False
        self._avg_entry_price = 0.0
        self._entry_count = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(vietnamese_3x_supertrend_strategy, self).OnStarted2(time)

        fast = SuperTrend()
        fast.Length = int(self._fast_atr_length.Value)
        fast.Multiplier = self._fast_multiplier.Value
        medium = SuperTrend()
        medium.Length = int(self._medium_atr_length.Value)
        medium.Multiplier = self._medium_multiplier.Value
        slow = SuperTrend()
        slow.Length = int(self._slow_atr_length.Value)
        slow.Multiplier = self._slow_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, medium, slow, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, med_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        dir1 = 1 if fast_val.IsUpTrend else -1
        dir2 = 1 if med_val.IsUpTrend else -1
        dir3 = 1 if slow_val.IsUpTrend else -1

        high_price = float(candle.HighPrice)
        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        low_price = float(candle.LowPrice)
        cooldown = int(self._cooldown_bars.Value)

        # Track highest green candle for breakout entry
        if dir1 < 0 and self._highest_green == 0.0:
            self._highest_green = high_price
        if self._highest_green > 0 and dir1 < 0:
            self._highest_green = max(self._highest_green, high_price)
        if dir1 >= 0:
            self._highest_green = 0.0

        # Exit logic for longs
        if self.Position > 0:
            # Break-even stop
            if dir1 > 0 and dir2 < 0 and dir3 < 0:
                if not self._break_even_active and low_price > self._avg_entry_price:
                    self._break_even_active = True
                if self._break_even_active and low_price <= self._avg_entry_price:
                    self.SellMarket(Math.Abs(self.Position))
                    self._reset_entries()
                    self._cooldown_remaining = cooldown
                    return

            # All uptrend + red candle exit
            if dir3 > 0 and dir2 > 0 and dir1 > 0 and close_price < open_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_entries()
                self._cooldown_remaining = cooldown
                return

            # Avg price in loss exit
            if self._avg_entry_price > close_price:
                self.SellMarket(Math.Abs(self.Position))
                self._reset_entries()
                self._cooldown_remaining = cooldown
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        # Entry logic - max 3 entries
        if self._entry_count < 3:
            if dir3 < 0:
                if dir2 > 0 and dir1 < 0:
                    self.BuyMarket(self.Volume)
                    self._add_entry(close_price)
                    self._cooldown_remaining = cooldown
                elif dir2 < 0 and close_price > float(IndicatorHelper.ToDecimal(fast_val)):
                    self.BuyMarket(self.Volume)
                    self._add_entry(close_price)
                    self._cooldown_remaining = cooldown
            else:
                if dir1 < 0 and self._highest_green > 0 and close_price > self._highest_green:
                    self.BuyMarket(self.Volume)
                    self._add_entry(close_price)
                    self._cooldown_remaining = cooldown

    def _add_entry(self, price):
        self._avg_entry_price = (self._avg_entry_price * self._entry_count + price) / (self._entry_count + 1)
        self._entry_count += 1

    def _reset_entries(self):
        self._avg_entry_price = 0.0
        self._entry_count = 0
        self._break_even_active = False

    def CreateClone(self):
        return vietnamese_3x_supertrend_strategy()
