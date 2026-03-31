import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class smc_hilo_max_min_strategy(Strategy):
    """Breakout straddle strategy. At a specified hour, uses previous candle high/low
    as breakout levels. Enters long on upside breakout, short on downside breakout.
    Manages stop-loss, take-profit, and trailing stop on candle close checks."""

    def __init__(self):
        super(smc_hilo_max_min_strategy, self).__init__()

        self._set_hour = self.Param("SetHour", 15) \
            .SetDisplay("Trigger Hour", "Terminal hour when breakout levels are set", "Timing")
        self._take_profit_pips = self.Param("TakeProfitPips", 500.0) \
            .SetDisplay("Take Profit (pips)", "Distance from entry to the profit target", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 30.0) \
            .SetDisplay("Stop Loss (pips)", "Distance from entry to the protective stop", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._min_stop_distance_pips = self.Param("MinStopDistancePips", 0.0) \
            .SetDisplay("Min Stop Distance (pips)", "Broker minimum stop distance for level padding", "Timing")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles that define the hourly breakout window", "Timing")

        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_close = 0.0
        self._has_previous = False
        self._last_setup_date = None

        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False

        self._entry_price = 0.0
        self._stop_price = None
        self._target_price = None
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SetHour(self):
        return self._set_hour.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def MinStopDistancePips(self):
        return self._min_stop_distance_pips.Value

    def OnReseted(self):
        super(smc_hilo_max_min_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_close = 0.0
        self._has_previous = False
        self._last_setup_date = None
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_ready = False
        self._entry_price = 0.0
        self._stop_price = None
        self._target_price = None
        self._pip_size = 0.0

    def _update_pip_size(self):
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            return
        step_val = float(step)
        digits = self._get_decimal_digits(step_val)
        if digits == 3 or digits == 5:
            self._pip_size = step_val * 10.0
        else:
            self._pip_size = step_val

    def _get_decimal_digits(self, value):
        value = abs(value)
        digits = 0
        while value != int(value) and digits < 10:
            value *= 10.0
            digits += 1
        return digits

    def _to_price(self, pips):
        return float(pips) * self._pip_size

    def OnStarted2(self, time):
        super(smc_hilo_max_min_strategy, self).OnStarted2(time)

        self._update_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _normalize_hour(self, hour):
        if hour < 0:
            hour = 0
        return ((hour % 24) + 24) % 24

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_pip_size()

        hour = candle.OpenTime.Hour
        current_date = candle.OpenTime.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # At the set hour, establish breakout levels from previous candle
        if self._has_previous:
            set_hour = self._normalize_hour(self.SetHour)
            if self._last_setup_date != current_date and hour == set_hour:
                self._setup_levels(close, current_date)

            # Cancel levels 2 hours after setup hour
            cancel_hour = self._normalize_hour(self.SetHour + 2)
            if self._last_setup_date == current_date and hour == cancel_hour:
                self._levels_ready = False

        # Manage existing position: check SL/TP/trailing
        if self.Position != 0:
            self._manage_position(close, high, low)

        # Try to enter on breakout
        if self._levels_ready and self.Position == 0:
            if close >= self._buy_level and self._buy_level > 0:
                self.BuyMarket()
                self._entry_price = close
                self._set_long_protection(close)
                self._levels_ready = False
            elif close <= self._sell_level and self._sell_level > 0:
                self.SellMarket()
                self._entry_price = close
                self._set_short_protection(close)
                self._levels_ready = False

        # Store for next candle
        self._previous_high = high
        self._previous_low = low
        self._previous_close = close
        self._has_previous = True

    def _setup_levels(self, current_close, current_date):
        if self.Position != 0:
            return

        prev_high = self._previous_high
        prev_low = self._previous_low

        if prev_high <= 0 or prev_low <= 0:
            return

        min_distance = self._to_price(self.MinStopDistancePips)

        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            step = 0.0001
        step_val = float(step)

        buy_trigger = prev_high
        if min_distance > 0:
            distance = prev_high - current_close
            if distance < min_distance:
                buy_trigger += min_distance - distance

        sell_trigger = prev_low
        if min_distance > 0:
            distance = current_close - prev_low
            if distance < min_distance:
                sell_trigger -= min_distance - distance

        self._buy_level = buy_trigger + step_val
        self._sell_level = sell_trigger - step_val
        self._levels_ready = True
        self._last_setup_date = current_date

    def _set_long_protection(self, entry_price):
        sl_dist = self._to_price(self.StopLossPips)
        tp_dist = self._to_price(self.TakeProfitPips)

        if sl_dist > 0:
            self._stop_price = self._previous_low - sl_dist
        else:
            self._stop_price = None

        if tp_dist > 0:
            self._target_price = entry_price + tp_dist
        else:
            self._target_price = None

    def _set_short_protection(self, entry_price):
        sl_dist = self._to_price(self.StopLossPips)
        tp_dist = self._to_price(self.TakeProfitPips)

        if sl_dist > 0:
            self._stop_price = self._previous_high + sl_dist
        else:
            self._stop_price = None

        if tp_dist > 0:
            self._target_price = entry_price - tp_dist
        else:
            self._target_price = None

    def _manage_position(self, close, high, low):
        if self.Position > 0:
            # Check stop loss
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_position()
                return
            # Check take profit
            if self._target_price is not None and high >= self._target_price:
                self.SellMarket()
                self._reset_position()
                return
            # Trailing stop
            self._update_long_trailing(close)
        elif self.Position < 0:
            # Check stop loss
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_position()
                return
            # Check take profit
            if self._target_price is not None and low <= self._target_price:
                self.BuyMarket()
                self._reset_position()
                return
            # Trailing stop
            self._update_short_trailing(close)

    def _update_long_trailing(self, close):
        trailing_pips = float(self.TrailingStopPips)
        if trailing_pips <= 0:
            return
        if self._entry_price <= 0:
            return

        distance = self._to_price(trailing_pips)
        if distance <= 0:
            return

        profit = close - self._entry_price
        if profit <= distance:
            return

        new_stop = close - distance
        if self._stop_price is None or new_stop > self._stop_price:
            self._stop_price = new_stop

    def _update_short_trailing(self, close):
        trailing_pips = float(self.TrailingStopPips)
        if trailing_pips <= 0:
            return
        if self._entry_price <= 0:
            return

        distance = self._to_price(trailing_pips)
        if distance <= 0:
            return

        profit = self._entry_price - close
        if profit <= distance:
            return

        new_stop = close + distance
        if self._stop_price is None or new_stop < self._stop_price:
            self._stop_price = new_stop

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._target_price = None

    def CreateClone(self):
        return smc_hilo_max_min_strategy()
