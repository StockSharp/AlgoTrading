import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class vietnamese_3x_supertrend_strategy(Strategy):
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

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(vietnamese_3x_supertrend_strategy, self).OnReseted()
        self._highest_green = 0.0
        self._break_even_active = False
        self._avg_entry_price = 0.0
        self._entry_count = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(vietnamese_3x_supertrend_strategy, self).OnStarted(time)
        fast = SuperTrend()
        fast.Length = self._fast_atr_length.Value
        fast.Multiplier = self._fast_multiplier.Value
        medium = SuperTrend()
        medium.Length = self._medium_atr_length.Value
        medium.Multiplier = self._medium_multiplier.Value
        slow = SuperTrend()
        slow.Length = self._slow_atr_length.Value
        slow.Multiplier = self._slow_multiplier.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, medium, slow, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, med_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast_st = fast_val
        med_st = med_val
        slow_st = slow_val
        dir1 = 1 if fast_st.IsUpTrend else -1
        dir2 = 1 if med_st.IsUpTrend else -1
        dir3 = 1 if slow_st.IsUpTrend else -1
        high_price = float(candle.HighPrice)
        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        low_price = float(candle.LowPrice)
        if dir1 < 0 and self._highest_green == 0.0:
            self._highest_green = high_price
        if self._highest_green > 0 and dir1 < 0:
            self._highest_green = max(self._highest_green, high_price)
        if dir1 >= 0:
            self._highest_green = 0.0
        if self.Position > 0:
            if dir1 > 0 and dir2 < 0 and dir3 < 0:
                if not self._break_even_active and low_price > self._avg_entry_price:
                    self._break_even_active = True
                if self._break_even_active and low_price <= self._avg_entry_price:
                    self.SellMarket()
                    self._reset_entries()
                    self._cooldown_remaining = self.cooldown_bars
                    return
            if dir3 > 0 and dir2 > 0 and dir1 > 0 and close_price < open_price:
                self.SellMarket()
                self._reset_entries()
                self._cooldown_remaining = self.cooldown_bars
                return
            if self._avg_entry_price > close_price:
                self.SellMarket()
                self._reset_entries()
                self._cooldown_remaining = self.cooldown_bars
                return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        if self._entry_count < 3:
            if dir3 < 0:
                if dir2 > 0 and dir1 < 0:
                    self.BuyMarket()
                    self._add_entry(close_price)
                    self._cooldown_remaining = self.cooldown_bars
                elif dir2 < 0 and close_price > float(fast_st.GetValue[float]()):
                    self.BuyMarket()
                    self._add_entry(close_price)
                    self._cooldown_remaining = self.cooldown_bars
            else:
                if dir1 < 0 and self._highest_green > 0 and close_price > self._highest_green:
                    self.BuyMarket()
                    self._add_entry(close_price)
                    self._cooldown_remaining = self.cooldown_bars

    def _add_entry(self, price):
        self._avg_entry_price = (self._avg_entry_price * self._entry_count + price) / (self._entry_count + 1)
        self._entry_count += 1

    def _reset_entries(self):
        self._avg_entry_price = 0.0
        self._entry_count = 0
        self._break_even_active = False

    def CreateClone(self):
        return vietnamese_3x_supertrend_strategy()
