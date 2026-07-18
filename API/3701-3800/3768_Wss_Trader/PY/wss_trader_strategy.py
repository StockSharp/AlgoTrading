import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class wss_trader_strategy(Strategy):
    """Port of the 'Wss_trader' MetaTrader strategy built around Camarilla and classic
    pivot levels. Uses dual timeframe: working candle for trading, daily candle for
    pivot calculation. Time-filtered breakout entries with trailing stop."""

    def __init__(self):
        super(wss_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Working Candle", "Primary candle type for trading logic", "General")
        self._daily_candle_type = self.Param("DailyCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Daily Candle", "Daily candle type used for pivot calculation", "General")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Hour of day when trading becomes active (0-23)", "Session")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Hour of day after which trading is disabled (0-23)", "Session")
        self._metric_points = self.Param("MetricPoints", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Metric Points", "Distance from pivot to entry levels in price steps", "Levels")
        self._trailing_points = self.Param("TrailingPoints", 20) \
            .SetDisplay("Trailing Points", "Trailing stop offset in price steps (0 disables trailing)", "Risk")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Order volume for entries", "Orders")

        self._previous_daily_candle_high = 0.0
        self._previous_daily_candle_low = 0.0
        self._previous_daily_candle_close = 0.0
        self._has_previous_daily = False
        self._price_step = 0.0

        self._long_entry_level = 0.0
        self._short_entry_level = 0.0
        self._long_stop_level = 0.0
        self._short_stop_level = 0.0
        self._long_target_level = 0.0
        self._short_target_level = 0.0

        self._previous_close = 0.0
        self._has_previous_close = False
        self._levels_ready = False
        self._can_trade = True
        self._last_candle_open_time = None

        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DailyCandleType(self):
        return self._daily_candle_type.Value

    @DailyCandleType.setter
    def DailyCandleType(self, value):
        self._daily_candle_type.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def MetricPoints(self):
        return self._metric_points.Value

    @property
    def TrailingPoints(self):
        return self._trailing_points.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    def OnReseted(self):
        super(wss_trader_strategy, self).OnReseted()

        self._previous_daily_candle_high = 0.0
        self._previous_daily_candle_low = 0.0
        self._previous_daily_candle_close = 0.0
        self._has_previous_daily = False
        self._price_step = 0.0

        self._long_entry_level = 0.0
        self._short_entry_level = 0.0
        self._long_stop_level = 0.0
        self._short_stop_level = 0.0
        self._long_target_level = 0.0
        self._short_target_level = 0.0

        self._previous_close = 0.0
        self._has_previous_close = False
        self._levels_ready = False
        self._can_trade = True
        self._last_candle_open_time = None

        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_target = 0.0
        self._short_target = 0.0

    def OnStarted2(self, time):
        super(wss_trader_strategy, self).OnStarted2(time)

        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            step = 0.0001
        self._price_step = float(step)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        daily_subscription = self.SubscribeCandles(self.DailyCandleType)
        daily_subscription.Bind(self._process_daily_candle).Start()

    def _process_daily_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._has_previous_daily:
            self._calculate_pivot_levels(
                self._previous_daily_candle_high,
                self._previous_daily_candle_low,
                self._previous_daily_candle_close
            )
            self._levels_ready = True

        self._previous_daily_candle_high = float(candle.HighPrice)
        self._previous_daily_candle_low = float(candle.LowPrice)
        self._previous_daily_candle_close = float(candle.ClosePrice)
        self._has_previous_daily = True

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._last_candle_open_time != candle.OpenTime:
            self._can_trade = True
            self._last_candle_open_time = candle.OpenTime

        within_hours = self._is_within_trading_hours(candle.CloseTime)
        if not within_hours:
            if self.Position != 0:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._reset_position_state()

            self._previous_close = float(candle.ClosePrice)
            self._has_previous_close = True
            return

        self._manage_positions(candle)

        if not self._levels_ready:
            self._previous_close = float(candle.ClosePrice)
            self._has_previous_close = True
            return

        if not self._has_previous_close:
            self._previous_close = float(candle.ClosePrice)
            self._has_previous_close = True
            return

        if not self._can_trade or self.Position != 0:
            self._previous_close = float(candle.ClosePrice)
            return

        close = float(candle.ClosePrice)

        # Long breakout: previous close was below entry level, current crosses above
        if self._previous_close < self._long_entry_level and close >= self._long_entry_level:
            self.BuyMarket()
            self._can_trade = False
            self._long_entry_price = close
            self._long_stop = self._round_price(self._long_stop_level)
            self._long_target = self._round_price(self._long_target_level)
            self._previous_close = close
            return

        # Short breakout: previous close was above entry level, current crosses below
        if self._previous_close > self._short_entry_level and close <= self._short_entry_level:
            self.SellMarket()
            self._can_trade = False
            self._short_entry_price = close
            self._short_stop = self._round_price(self._short_stop_level)
            self._short_target = self._round_price(self._short_target_level)
            self._previous_close = close
            return

        self._previous_close = close

    def _manage_positions(self, candle):
        if self.Position > 0:
            self._manage_long_position(candle)
        elif self.Position < 0:
            self._manage_short_position(candle)

    def _manage_long_position(self, candle):
        stop = self._long_stop
        target = self._long_target
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        close = float(candle.ClosePrice)

        if stop > 0 and low <= stop:
            self.SellMarket()
            self._reset_long_state()
            return

        if target > 0 and high >= target:
            self.SellMarket()
            self._reset_long_state()
            return

        trailing_distance = self._convert_points_to_price(self.TrailingPoints)
        if trailing_distance <= 0 or self._long_entry_price <= 0:
            return

        if close - self._long_entry_price >= trailing_distance:
            new_stop = self._round_price(close - trailing_distance)
            if new_stop > self._long_stop:
                self._long_stop = new_stop
                if low <= self._long_stop:
                    self.SellMarket()
                    self._reset_long_state()

    def _manage_short_position(self, candle):
        stop = self._short_stop
        target = self._short_target
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if stop > 0 and high >= stop:
            self.BuyMarket()
            self._reset_short_state()
            return

        if target > 0 and low <= target:
            self.BuyMarket()
            self._reset_short_state()
            return

        trailing_distance = self._convert_points_to_price(self.TrailingPoints)
        if trailing_distance <= 0 or self._short_entry_price <= 0:
            return

        if self._short_entry_price - close >= trailing_distance:
            new_stop = self._round_price(close + trailing_distance)
            if self._short_stop == 0 or new_stop < self._short_stop:
                self._short_stop = new_stop
                if high >= self._short_stop:
                    self.BuyMarket()
                    self._reset_short_state()

    def _reset_position_state(self):
        self._reset_long_state()
        self._reset_short_state()

    def _reset_long_state(self):
        self._long_entry_price = 0.0
        self._long_stop = 0.0
        self._long_target = 0.0

    def _reset_short_state(self):
        self._short_entry_price = 0.0
        self._short_stop = 0.0
        self._short_target = 0.0

    def _calculate_pivot_levels(self, high, low, close):
        pivot = (high + low + close) / 3.0
        metric_distance = float(self.MetricPoints) * self._price_step
        double_metric = 2.0 * metric_distance
        twenty_points = 20.0 * self._price_step
        range_val = (high - low) * 1.1 / 2.0

        lwb = self._round_price(pivot + metric_distance)
        lwr = self._round_price(pivot - metric_distance)
        lrr = self._round_price(pivot - double_metric)

        rtl = self._round_price(max(close + range_val, lrr - twenty_points))
        rts = self._round_price(min(close - range_val, lrr - twenty_points))

        self._long_entry_level = lwb
        self._short_entry_level = lwr
        self._long_stop_level = lwr
        self._short_stop_level = lwb
        self._long_target_level = rtl
        self._short_target_level = rts

    def _round_price(self, price):
        if self._price_step > 0:
            return round(price / self._price_step) * self._price_step
        return price

    def _convert_points_to_price(self, points):
        if points <= 0:
            return 0.0
        return float(points) * self._price_step

    def _is_within_trading_hours(self, time):
        hour = time.Hour
        start = max(0, min(23, self.StartHour))
        end = max(0, min(23, self.EndHour))

        if start <= end:
            return hour >= start and hour <= end
        return hour >= start or hour <= end

    def CreateClone(self):
        return wss_trader_strategy()
