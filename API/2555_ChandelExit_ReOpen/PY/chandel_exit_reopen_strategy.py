import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class chandel_exit_reopen_strategy(Strategy):
    def __init__(self):
        super(chandel_exit_reopen_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._range_period = self.Param("RangePeriod", 15)
        self._shift = self.Param("Shift", 1)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_multiplier = self.Param("AtrMultiplier", 4.0)
        self._signal_bar = self.Param("SignalBar", 1)
        self._price_step_points = self.Param("PriceStepPoints", 1000.0)
        self._max_additions = self.Param("MaxAdditions", 1)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exits = self.Param("EnableBuyExits", True)
        self._enable_sell_exits = self.Param("EnableSellExits", True)

        self._history = []
        self._signals = []
        self._previous_up = None
        self._previous_down = None
        self._direction = 0

        self._long_additions = 0
        self._short_additions = 0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None
        self._last_long_addition_time = None
        self._last_short_addition_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RangePeriod(self):
        return self._range_period.Value

    @RangePeriod.setter
    def RangePeriod(self, value):
        self._range_period.Value = value

    @property
    def Shift(self):
        return self._shift.Value

    @Shift.setter
    def Shift(self, value):
        self._shift.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def PriceStepPoints(self):
        return self._price_step_points.Value

    @PriceStepPoints.setter
    def PriceStepPoints(self, value):
        self._price_step_points.Value = value

    @property
    def MaxAdditions(self):
        return self._max_additions.Value

    @MaxAdditions.setter
    def MaxAdditions(self, value):
        self._max_additions.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def EnableBuyEntries(self):
        return self._enable_buy_entries.Value

    @EnableBuyEntries.setter
    def EnableBuyEntries(self, value):
        self._enable_buy_entries.Value = value

    @property
    def EnableSellEntries(self):
        return self._enable_sell_entries.Value

    @EnableSellEntries.setter
    def EnableSellEntries(self, value):
        self._enable_sell_entries.Value = value

    @property
    def EnableBuyExits(self):
        return self._enable_buy_exits.Value

    @EnableBuyExits.setter
    def EnableBuyExits(self, value):
        self._enable_buy_exits.Value = value

    @property
    def EnableSellExits(self):
        return self._enable_sell_exits.Value

    @EnableSellExits.setter
    def EnableSellExits(self, value):
        self._enable_sell_exits.Value = value

    def OnStarted2(self, time):
        super(chandel_exit_reopen_strategy, self).OnStarted2(time)

        self._history = []
        self._signals = []
        self._previous_up = None
        self._previous_down = None
        self._direction = 0
        self._reset_long_state()
        self._reset_short_state()

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(atr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr = float(atr_value) if atr_value.IsFinal else 0.0
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        open_time = candle.OpenTime

        info = (open_time, high, low, close, atr)
        self._history.append(info)

        if atr_value.IsFinal:
            signal = self._calculate_signal(info)
        else:
            signal = (open_time, False, False, 0.0, 0.0)

        self._signals.append(signal)
        self._trim_cache()

        if not atr_value.IsFinal:
            return

        sb = int(self.SignalBar)
        if len(self._signals) <= sb:
            return

        target_index = len(self._signals) - 1 - sb
        if target_index < 0:
            return

        target_signal = self._signals[target_index]
        is_up_signal = target_signal[1]
        is_down_signal = target_signal[2]

        buy_open = is_up_signal and self.EnableBuyEntries
        sell_open = is_down_signal and self.EnableSellEntries
        buy_close = is_down_signal and self.EnableBuyExits
        sell_close = is_up_signal and self.EnableSellExits

        if ((self.EnableBuyEntries and self.EnableBuyExits) or (self.EnableSellEntries and self.EnableSellExits)) and not buy_close and not sell_close:
            for idx in range(target_index - 1, -1, -1):
                prev_signal = self._signals[idx]
                if not sell_close and self.EnableSellExits and prev_signal[1]:
                    sell_close = True
                    break
                if not buy_close and self.EnableBuyExits and prev_signal[2]:
                    buy_close = True
                    break

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        price_step_dist = float(self.PriceStepPoints) * step

        long_closed = False
        short_closed = False

        if self.Position > 0:
            if self._long_stop_price is not None and low <= self._long_stop_price:
                self.SellMarket()
                self._reset_long_state()
                long_closed = True
            elif self._long_take_price is not None and high >= self._long_take_price:
                self.SellMarket()
                self._reset_long_state()
                long_closed = True

        if self.Position < 0:
            if self._short_stop_price is not None and high >= self._short_stop_price:
                self.BuyMarket()
                self._reset_short_state()
                short_closed = True
            elif self._short_take_price is not None and low <= self._short_take_price:
                self.BuyMarket()
                self._reset_short_state()
                short_closed = True

        if not long_closed and buy_close and self.Position > 0:
            self.SellMarket()
            self._reset_long_state()
            long_closed = True

        if not short_closed and sell_close and self.Position < 0:
            self.BuyMarket()
            self._reset_short_state()
            short_closed = True

        if not long_closed and self.Position > 0 and self.MaxAdditions > 0 and self._long_entry_price is not None and price_step_dist > 0.0 and self._long_additions < self.MaxAdditions:
            if close - self._long_entry_price >= price_step_dist and self._last_long_addition_time != open_time:
                self.BuyMarket()
                self._long_additions += 1
                self._long_entry_price = close
                self._last_long_addition_time = open_time
                self._update_long_protection(close, step)

        if not short_closed and self.Position < 0 and self.MaxAdditions > 0 and self._short_entry_price is not None and price_step_dist > 0.0 and self._short_additions < self.MaxAdditions:
            if self._short_entry_price - close >= price_step_dist and self._last_short_addition_time != open_time:
                self.SellMarket()
                self._short_additions += 1
                self._short_entry_price = close
                self._last_short_addition_time = open_time
                self._update_short_protection(close, step)

        if buy_open and self.Position < 0 and not self.EnableSellExits:
            buy_open = False

        if sell_open and self.Position > 0 and not self.EnableBuyExits:
            sell_open = False

        if buy_open:
            self.BuyMarket()
            self._reset_short_state()
            self._long_additions = 0
            self._long_entry_price = close
            self._last_long_addition_time = open_time
            self._update_long_protection(close, step)

        if sell_open:
            self.SellMarket()
            self._reset_long_state()
            self._short_additions = 0
            self._short_entry_price = close
            self._last_short_addition_time = open_time
            self._update_short_protection(close, step)

    def _trim_cache(self):
        max_items = max(int(self.RangePeriod) + int(self.Shift) + 5, int(self.SignalBar) + 5) + 50
        if len(self._history) <= max_items:
            return
        remove_count = len(self._history) - max_items
        self._history = self._history[remove_count:]
        self._signals = self._signals[remove_count:]

    def _calculate_signal(self, current):
        current_time, current_high, current_low, current_close, current_atr = current
        history = list(self._history)
        current_index = len(history) - 1
        range_period = int(self.RangePeriod)
        shift = int(self.Shift)

        if range_period <= 0 or current_index - shift < 0:
            return (current_time, False, False, 0.0, 0.0)

        window_end = current_index - shift
        window_start = window_end - (range_period - 1)

        if window_start < 0 or window_end >= len(history):
            return (current_time, False, False, 0.0, 0.0)

        highest_high = -1e18
        lowest_low = 1e18

        for i in range(window_start, window_end + 1):
            item = history[i]
            if item[1] > highest_high:
                highest_high = item[1]
            if item[2] < lowest_low:
                lowest_low = item[2]

        if highest_high < -1e17 or lowest_low > 1e17:
            return (current_time, False, False, 0.0, 0.0)

        atr_adj = current_atr * float(self.AtrMultiplier)
        upper_band = highest_high - atr_adj
        lower_band = lowest_low + atr_adj

        if self._direction >= 0:
            if current_close < upper_band:
                self._direction = -1
                up = lower_band
                down = upper_band
            else:
                up = upper_band
                down = lower_band
        else:
            if current_close > lower_band:
                self._direction = 1
                down = lower_band
                up = upper_band
            else:
                up = lower_band
                down = upper_band

        is_up_signal = False
        is_down_signal = False

        if self._previous_down is not None and self._previous_up is not None:
            if self._previous_down <= self._previous_up and down > up:
                is_up_signal = True
            if self._previous_down >= self._previous_up and down < up:
                is_down_signal = True

        self._previous_up = up
        self._previous_down = down

        return (current_time, is_up_signal, is_down_signal, up, down)

    def _update_long_protection(self, entry_price, step):
        sl = int(self.StopLossPoints)
        tp = int(self.TakeProfitPoints)
        self._long_stop_price = entry_price - sl * step if sl > 0 else None
        self._long_take_price = entry_price + tp * step if tp > 0 else None

    def _update_short_protection(self, entry_price, step):
        sl = int(self.StopLossPoints)
        tp = int(self.TakeProfitPoints)
        self._short_stop_price = entry_price + sl * step if sl > 0 else None
        self._short_take_price = entry_price - tp * step if tp > 0 else None

    def _reset_long_state(self):
        self._long_additions = 0
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._last_long_addition_time = None

    def _reset_short_state(self):
        self._short_additions = 0
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._last_short_addition_time = None

    def OnReseted(self):
        super(chandel_exit_reopen_strategy, self).OnReseted()
        self._history = []
        self._signals = []
        self._previous_up = None
        self._previous_down = None
        self._direction = 0
        self._reset_long_state()
        self._reset_short_state()

    def CreateClone(self):
        return chandel_exit_reopen_strategy()
