import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class exp_hans_indicator_cloud_system_tm_plus_strategy(Strategy):
    _SESSION1_START = 4
    _SESSION1_END = 8
    _SESSION2_END = 12

    def __init__(self):
        super(exp_hans_indicator_cloud_system_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Time frame used for Hans calculations", "Data")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (points)", "Distance to the protective stop in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (points)", "Distance to the profit target in points", "Risk")
        self._pips_for_entry = self.Param("PipsForEntry", 5) \
            .SetDisplay("Pips For Entry", "Offset added above/below the breakout range", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Closed candle offset used for signals", "Indicator")
        self._local_time_zone = self.Param("LocalTimeZone", 0) \
            .SetDisplay("Local Time Zone", "Broker/server time zone", "Indicator")
        self._destination_time_zone = self.Param("DestinationTimeZone", 0) \
            .SetDisplay("Destination Time Zone", "Target time zone for sessions", "Indicator")
        self._entry_cooldown_bars = self.Param("EntryCooldownBars", 10) \
            .SetDisplay("Entry Cooldown", "Bars to wait after an entry signal", "Risk")
        self._use_time_exit = self.Param("UseTimeExit", True) \
            .SetDisplay("Use Time Exit", "Close positions after the holding period", "Risk")
        self._holding_minutes = self.Param("HoldingMinutes", 1500) \
            .SetDisplay("Holding Minutes", "Maximum position lifetime in minutes", "Risk")

        self._color_history = []
        self._s1_high = None
        self._s1_low = None
        self._s1_completed = False
        self._s2_high = None
        self._s2_low = None
        self._s2_completed = False
        self._day_date = None
        self._day_entry_taken = False
        self._prev_close_price = 0.0
        self._has_prev_close = False
        self._cooldown_remaining = 0
        self._stop_price = None
        self._take_price = None
        self._entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value
    @property
    def PipsForEntry(self):
        return self._pips_for_entry.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def LocalTimeZone(self):
        return self._local_time_zone.Value
    @property
    def DestinationTimeZone(self):
        return self._destination_time_zone.Value
    @property
    def EntryCooldownBars(self):
        return self._entry_cooldown_bars.Value
    @property
    def UseTimeExit(self):
        return self._use_time_exit.Value
    @property
    def HoldingMinutes(self):
        return self._holding_minutes.Value

    def OnReseted(self):
        super(exp_hans_indicator_cloud_system_tm_plus_strategy, self).OnReseted()
        self._color_history = []
        self._s1_high = None
        self._s1_low = None
        self._s1_completed = False
        self._s2_high = None
        self._s2_low = None
        self._s2_completed = False
        self._day_date = None
        self._day_entry_taken = False
        self._prev_close_price = 0.0
        self._has_prev_close = False
        self._cooldown_remaining = 0
        self._stop_price = None
        self._take_price = None
        self._entry_time = None

    def OnStarted(self, time):
        super(exp_hans_indicator_cloud_system_tm_plus_strategy, self).OnStarted(time)
        self._color_history = []
        self._s1_high = None
        self._s1_low = None
        self._s1_completed = False
        self._s2_high = None
        self._s2_low = None
        self._s2_completed = False
        self._day_date = None
        self._day_entry_taken = False
        self._prev_close_price = 0.0
        self._has_prev_close = False
        self._cooldown_remaining = 0
        self._stop_price = None
        self._take_price = None
        self._entry_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0 and (self._entry_time is not None or self._stop_price is not None or self._take_price is not None):
            self._reset_position_state()

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        self._update_daily_state(candle)

        color = self._calculate_color(candle)
        self._color_history.append(color)
        self._trim_history()

        offset = max(1, self.SignalBar)
        if len(self._color_history) <= offset:
            self._manage_exits(candle)
            self._prev_close_price = float(candle.ClosePrice)
            self._has_prev_close = True
            return

        current_index = len(self._color_history) - offset
        if current_index >= len(self._color_history):
            self._manage_exits(candle)
            self._prev_close_price = float(candle.ClosePrice)
            self._has_prev_close = True
            return

        current_color = self._color_history[current_index]
        has_bands, upper, lower = self._try_get_active_bands()
        buy_entry_signal = False
        sell_entry_signal = False

        if has_bands and self._has_prev_close:
            buy_entry_signal = self._prev_close_price <= upper and float(candle.ClosePrice) > upper
            sell_entry_signal = self._prev_close_price >= lower and float(candle.ClosePrice) < lower

        buy_exit_signal = self._is_lower_breakout(current_color) or (has_bands and float(candle.ClosePrice) < lower)
        sell_exit_signal = self._is_upper_breakout(current_color) or (has_bands and float(candle.ClosePrice) > upper)

        if self.Position > 0:
            exit_by_time = self.UseTimeExit and self.HoldingMinutes > 0 and self._entry_time is not None and (candle.CloseTime - self._entry_time).TotalMinutes >= self.HoldingMinutes
            exit_by_stop = self._stop_price is not None and float(candle.LowPrice) <= self._stop_price
            exit_by_target = self._take_price is not None and float(candle.HighPrice) >= self._take_price
            if exit_by_time or buy_exit_signal or exit_by_stop or exit_by_target:
                self.SellMarket()
                self._reset_position_state()
        elif self.Position < 0:
            exit_by_time = self.UseTimeExit and self.HoldingMinutes > 0 and self._entry_time is not None and (candle.CloseTime - self._entry_time).TotalMinutes >= self.HoldingMinutes
            exit_by_stop = self._stop_price is not None and float(candle.HighPrice) >= self._stop_price
            exit_by_target = self._take_price is not None and float(candle.LowPrice) <= self._take_price
            if exit_by_time or sell_exit_signal or exit_by_stop or exit_by_target:
                self.BuyMarket()
                self._reset_position_state()

        self._prev_close_price = float(candle.ClosePrice)
        self._has_prev_close = True

        if self._cooldown_remaining > 0:
            return

        if self._day_entry_taken:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if buy_entry_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.EntryCooldownBars
            self._day_entry_taken = True
            self._entry_time = candle.CloseTime
            pip_size = self._get_pip_size()
            if pip_size > 0.0:
                self._stop_price = float(candle.ClosePrice) - pip_size * float(self.StopLossPoints) if self.StopLossPoints > 0 else None
                self._take_price = float(candle.ClosePrice) + pip_size * float(self.TakeProfitPoints) if self.TakeProfitPoints > 0 else None
        elif sell_entry_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.EntryCooldownBars
            self._day_entry_taken = True
            self._entry_time = candle.CloseTime
            pip_size = self._get_pip_size()
            if pip_size > 0.0:
                self._stop_price = float(candle.ClosePrice) + pip_size * float(self.StopLossPoints) if self.StopLossPoints > 0 else None
                self._take_price = float(candle.ClosePrice) - pip_size * float(self.TakeProfitPoints) if self.TakeProfitPoints > 0 else None

    def _manage_exits(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_position_state()
            elif self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset_position_state()
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_position_state()
            elif self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset_position_state()

    def _update_daily_state(self, candle):
        dest_hour = candle.OpenTime.Hour + (self.DestinationTimeZone - self.LocalTimeZone)
        dest_date = candle.OpenTime.Date
        if dest_hour >= 24:
            dest_hour -= 24
            dest_date = dest_date.AddDays(1)
        elif dest_hour < 0:
            dest_hour += 24
            dest_date = dest_date.AddDays(-1)

        if self._day_date is None or self._day_date != dest_date:
            self._day_date = dest_date
            self._s1_high = None
            self._s1_low = None
            self._s1_completed = False
            self._s2_high = None
            self._s2_low = None
            self._s2_completed = False
            self._day_entry_taken = False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if dest_hour >= self._SESSION1_START and dest_hour < self._SESSION1_END:
            self._s1_high = max(self._s1_high, high) if self._s1_high is not None else high
            self._s1_low = min(self._s1_low, low) if self._s1_low is not None else low
            self._s1_completed = False
        elif dest_hour >= self._SESSION1_END and dest_hour < self._SESSION2_END:
            if not self._s1_completed and self._s1_high is not None and self._s1_low is not None:
                self._s1_completed = True
            self._s2_high = max(self._s2_high, high) if self._s2_high is not None else high
            self._s2_low = min(self._s2_low, low) if self._s2_low is not None else low
            self._s2_completed = False
        else:
            if not self._s1_completed and self._s1_high is not None and self._s1_low is not None:
                self._s1_completed = True
            if not self._s2_completed and self._s2_high is not None and self._s2_low is not None:
                self._s2_completed = True

    def _calculate_color(self, candle):
        has_bands, upper, lower = self._try_get_active_bands()
        if not has_bands:
            return 2

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        if close > upper:
            return 0 if close >= open_p else 1
        if close < lower:
            return 4 if close <= open_p else 3
        return 2

    def _try_get_active_bands(self):
        pip_size = self._get_pip_size()
        if pip_size <= 0.0:
            return False, 0.0, 0.0

        if self._s2_completed and self._s2_high is not None and self._s2_low is not None:
            upper = self._s2_high + pip_size * float(self.PipsForEntry)
            lower = self._s2_low - pip_size * float(self.PipsForEntry)
            return True, upper, lower

        if self._s1_completed and self._s1_high is not None and self._s1_low is not None:
            upper = self._s1_high + pip_size * float(self.PipsForEntry)
            lower = self._s1_low - pip_size * float(self.PipsForEntry)
            return True, upper, lower

        return False, 0.0, 0.0

    def _get_pip_size(self):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None:
            step = float(sec.PriceStep)
            if step > 0.0:
                return step
        return 0.01

    def _reset_position_state(self):
        self._entry_time = None
        self._stop_price = None
        self._take_price = None

    def _trim_history(self):
        max_len = 1024
        if len(self._color_history) > max_len:
            excess = len(self._color_history) - max_len
            del self._color_history[:excess]

    @staticmethod
    def _is_upper_breakout(color):
        return color == 0 or color == 1

    @staticmethod
    def _is_lower_breakout(color):
        return color == 3 or color == 4

    def CreateClone(self):
        return exp_hans_indicator_cloud_system_tm_plus_strategy()
