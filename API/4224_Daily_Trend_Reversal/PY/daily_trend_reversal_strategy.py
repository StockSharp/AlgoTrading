import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class daily_trend_reversal_strategy(Strategy):
    """
    Port of the MetaTrader strategy dailyTrendReversal_D1.
    Combines daily open/high/low levels with a multi-step filter and CCI trend confirmation.
    Applies strict session control, optional reversal exits, and a configurable daily profit stop.
    """

    FLAT = 0
    UP = 1
    DOWN = 2

    def __init__(self):
        super(daily_trend_reversal_strategy, self).__init__()
        self._enable_auto_trading = self.Param("EnableAutoTrading", True) \
            .SetDisplay("Auto Trading", "Enable automated entries inside the session", "Trading")
        self._enable_reversal = self.Param("EnableReversal", True) \
            .SetDisplay("Reversal Exit", "Close positions on confirmed opposite trend", "Trading")
        self._trend_steps = self.Param("TrendSteps", 3) \
            .SetDisplay("Trend Steps", "Number of filters used for daily direction", "Trend Filter")
        self._take_profit_pips = self.Param("TakeProfitPips", 30.0) \
            .SetDisplay("Take Profit (pips)", "Distance to fixed take profit (0 disables)", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0) \
            .SetDisplay("Stop Loss (pips)", "Distance to protective stop loss (0 disables)", "Risk")
        self._profit_stop = self.Param("ProfitStop", 100.0) \
            .SetDisplay("Profit Stop", "Daily profit target that pauses trading", "Risk")
        self._gmt_diff = self.Param("GmtDiff", 0) \
            .SetDisplay("GMT Diff", "Chart time minus GMT in hours", "Session")
        self._gmt_start_hour = self.Param("GmtStartHour", 5) \
            .SetDisplay("Start Hour", "Session start hour in GMT", "Session")
        self._gmt_end_hour = self.Param("GmtEndHour", 14) \
            .SetDisplay("End Hour", "Session end hour for new trades (GMT)", "Session")
        self._gmt_closing_hour = self.Param("GmtClosingHour", 18) \
            .SetDisplay("Closing Hour", "Session close hour for active trades (GMT)", "Session")
        self._holding_hours = self.Param("HoldingHours", 10) \
            .SetDisplay("Holding Hours", "Maximum holding time for positions", "Risk")
        self._risk_pips = self.Param("RiskPips", 30) \
            .SetDisplay("Risk (pips)", "Risk filter threshold used by trend steps", "Trend Filter")
        self._cci_period = self.Param("CciPeriod", 15) \
            .SetDisplay("CCI Period", "Length of the Commodity Channel Index", "Trend Filter")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")

        self._cci_history = []
        self._current_day = None
        self._daily_open = 0.0
        self._daily_high = 0.0
        self._daily_low = 0.0
        self._day_pnl_base = 0.0
        self._trading_suspended = False
        self._last_close = 0.0
        self._last_candle_time = None
        self._pip_size = 0.0
        self._ten_pips = 0.0

        self._long_entry_price = 0.0
        self._long_entry_time = None
        self._long_tp_price = 0.0
        self._long_stop_price = 0.0
        self._long_break_even = False

        self._short_entry_price = 0.0
        self._short_entry_time = None
        self._short_tp_price = 0.0
        self._short_stop_price = 0.0
        self._short_break_even = False

        self._cci_formed = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(daily_trend_reversal_strategy, self).OnReseted()
        self._cci_history = []
        self._current_day = None
        self._daily_open = 0.0
        self._daily_high = 0.0
        self._daily_low = 0.0
        self._day_pnl_base = 0.0
        self._trading_suspended = False
        self._last_close = 0.0
        self._last_candle_time = None
        self._long_entry_price = 0.0
        self._long_entry_time = None
        self._long_tp_price = 0.0
        self._long_stop_price = 0.0
        self._long_break_even = False
        self._short_entry_price = 0.0
        self._short_entry_time = None
        self._short_tp_price = 0.0
        self._short_stop_price = 0.0
        self._short_break_even = False
        self._cci_formed = False

    def OnStarted(self, time):
        super(daily_trend_reversal_strategy, self).OnStarted(time)

        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.0001

        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)

        if decimals >= 3:
            self._pip_size = step * 10.0
        else:
            self._pip_size = step if step > 0 else 0.0001

        self._ten_pips = 10.0 * self._pip_size
        self._day_pnl_base = float(self.PnL)
        self._trading_suspended = False

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        self._last_candle_time = candle.CloseTime
        self._last_close = float(candle.ClosePrice)
        cci_value = float(cci_value)

        self._update_daily_levels(candle)
        self._update_cci_history(cci_value)

        risk_distance = max(0, self._risk_pips.Value) * self._pip_size
        trend = self._get_directional_trend(candle, risk_distance)
        range_trend = self._get_range_trend()
        cci_trend = self._get_cci_trend()

        self._manage_existing_positions(candle, range_trend, cci_trend, risk_distance)
        self._handle_profit_stop()

        if not self._can_open_positions(candle):
            return

        if not self._cci_formed:
            return

        self._evaluate_entries(candle, trend, range_trend, cci_trend)

    def _evaluate_entries(self, candle, trend, range_trend, cci_trend):
        price = float(candle.ClosePrice)

        if (trend == self.UP and range_trend == self.UP and cci_trend == self.UP
                and price > self._daily_open and self.Position <= 0):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        if (trend == self.DOWN and range_trend == self.DOWN and cci_trend == self.DOWN
                and price < self._daily_open and self.Position >= 0):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def _manage_existing_positions(self, candle, range_trend, cci_trend, risk_distance):
        if self.Position > 0:
            self._manage_long(candle, range_trend, cci_trend, risk_distance)
        elif self.Position < 0:
            self._manage_short(candle, range_trend, cci_trend, risk_distance)

    def _manage_long(self, candle, range_trend, cci_trend, risk_distance):
        price = float(candle.ClosePrice)

        if self._long_tp_price > 0 and price >= self._long_tp_price:
            self.SellMarket()
            return

        if self._long_stop_price > 0 and price <= self._long_stop_price:
            self.SellMarket()
            return

        holding_exceeded = False
        if self._holding_hours.Value > 0 and self._long_entry_time is not None:
            diff = candle.CloseTime - self._long_entry_time
            if diff >= TimeSpan.FromHours(self._holding_hours.Value):
                holding_exceeded = True

        closing_reached = False
        if self._gmt_closing_hour.Value > 0:
            closing_reached = self._is_after_or_equal_hour(candle.CloseTime, self._gmt_closing_hour.Value + self._gmt_diff.Value)

        if (holding_exceeded or closing_reached) and self._long_entry_price != 0:
            if price > self._long_entry_price:
                self.SellMarket()
                return
            if not self._long_break_even:
                self._long_break_even = True

        if self._long_break_even and self._long_entry_price != 0 and price >= self._long_entry_price:
            self.SellMarket()
            self._long_break_even = False
            return

        if self._enable_reversal.Value and self._daily_open != 0:
            step1 = self._trend_steps.Value >= 0 and (price - self._daily_low > risk_distance)
            step2 = self._trend_steps.Value >= 2 and (self._daily_high - self._daily_open >= risk_distance) and (self._daily_open - price <= self._ten_pips)

            if price < self._daily_open and (step1 or step2) and range_trend == self.DOWN and cci_trend == self.DOWN:
                self.SellMarket()

    def _manage_short(self, candle, range_trend, cci_trend, risk_distance):
        price = float(candle.ClosePrice)

        if self._short_tp_price > 0 and price <= self._short_tp_price:
            self.BuyMarket()
            return

        if self._short_stop_price > 0 and price >= self._short_stop_price:
            self.BuyMarket()
            return

        holding_exceeded = False
        if self._holding_hours.Value > 0 and self._short_entry_time is not None:
            diff = candle.CloseTime - self._short_entry_time
            if diff >= TimeSpan.FromHours(self._holding_hours.Value):
                holding_exceeded = True

        closing_reached = False
        if self._gmt_closing_hour.Value > 0:
            closing_reached = self._is_after_or_equal_hour(candle.CloseTime, self._gmt_closing_hour.Value + self._gmt_diff.Value)

        if (holding_exceeded or closing_reached) and self._short_entry_price != 0:
            if price < self._short_entry_price:
                self.BuyMarket()
                return
            if not self._short_break_even:
                self._short_break_even = True

        if self._short_break_even and self._short_entry_price != 0 and price <= self._short_entry_price:
            self.BuyMarket()
            self._short_break_even = False
            return

        if self._enable_reversal.Value and self._daily_open != 0:
            step1 = self._trend_steps.Value >= 0 and (self._daily_high - price > risk_distance)
            step2 = self._trend_steps.Value >= 2 and (self._daily_open - self._daily_low >= risk_distance) and (price - self._daily_open <= self._ten_pips)

            if price > self._daily_open and (step1 or step2) and range_trend == self.UP and cci_trend == self.UP:
                self.BuyMarket()

    def _handle_profit_stop(self):
        if self._profit_stop.Value <= 0 or self._trading_suspended:
            return

        realized = float(self.PnL) - self._day_pnl_base
        if realized >= self._profit_stop.Value:
            self._trading_suspended = True
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

    def _can_open_positions(self, candle):
        if not self._enable_auto_trading.Value or self._trading_suspended:
            return False
        if self._current_day is None:
            return False
        dow = candle.OpenTime.DayOfWeek
        from System import DayOfWeek
        if dow == DayOfWeek.Saturday or dow == DayOfWeek.Sunday:
            return False
        if not self._is_within_trading_window(candle.OpenTime):
            return False
        return True

    def _update_daily_levels(self, candle):
        day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._daily_open = float(candle.OpenPrice)
            self._daily_high = float(candle.HighPrice)
            self._daily_low = float(candle.LowPrice)
            self._day_pnl_base = float(self.PnL)
            self._long_break_even = False
            self._short_break_even = False
        else:
            self._daily_high = max(self._daily_high, float(candle.HighPrice))
            self._daily_low = min(self._daily_low, float(candle.LowPrice))

    def _update_cci_history(self, cci_value):
        if self._cci_formed or len(self._cci_history) >= 2:
            self._cci_formed = True
        self._cci_history.insert(0, cci_value)
        if len(self._cci_history) > 3:
            self._cci_history.pop()

    def _get_cci_trend(self):
        if len(self._cci_history) < 3:
            return self.FLAT
        current = self._cci_history[0]
        previous = self._cci_history[1]
        older = self._cci_history[2]
        if current >= previous and previous >= older:
            return self.UP
        if current <= previous and previous <= older:
            return self.DOWN
        return self.FLAT

    def _get_directional_trend(self, candle, risk_distance):
        if self._current_day is None:
            return self.FLAT
        price = float(candle.ClosePrice)
        ts = self._trend_steps.Value

        if price > self._daily_open:
            step1 = ts >= 0 and (self._daily_high - price > risk_distance)
            step2 = ts >= 2 and (self._daily_open - self._daily_low >= risk_distance) and (price - self._daily_open <= self._ten_pips)
            step3 = ts >= 3 and (price - self._daily_open <= self._ten_pips) and (float(candle.ClosePrice) > float(candle.OpenPrice))
            if step1 or step2 or step3:
                return self.UP
        elif price < self._daily_open:
            step1 = ts >= 0 and (price - self._daily_low > risk_distance)
            step2 = ts >= 2 and (self._daily_high - self._daily_open >= risk_distance) and (self._daily_open - price <= self._ten_pips)
            step3 = ts >= 3 and (self._daily_open - price <= self._ten_pips) and (float(candle.ClosePrice) < float(candle.OpenPrice))
            if step1 or step2 or step3:
                return self.DOWN
        return self.FLAT

    def _get_range_trend(self):
        up_distance = self._daily_high - self._daily_open
        down_distance = self._daily_open - self._daily_low
        if up_distance > down_distance:
            return self.UP
        if up_distance < down_distance:
            return self.DOWN
        return self.FLAT

    def _is_within_trading_window(self, time):
        hour = time.Hour
        start = self._normalize_hour(self._gmt_start_hour.Value + self._gmt_diff.Value)
        end = self._normalize_hour(self._gmt_end_hour.Value + self._gmt_diff.Value)
        if start == end:
            return False
        if start < end:
            return hour >= start and hour < end
        else:
            return hour >= start or hour < end

    def _is_after_or_equal_hour(self, time, target_hour):
        hour = time.Hour
        normalized = self._normalize_hour(target_hour)
        return hour >= normalized

    def _normalize_hour(self, hour):
        normalized = hour % 24
        return normalized + 24 if normalized < 0 else normalized

    def OnPositionReceived(self, position):
        super(daily_trend_reversal_strategy, self).OnPositionReceived(position)

        tp_pips = self._take_profit_pips.Value
        sl_pips = self._stop_loss_pips.Value

        if self.Position > 0:
            self._long_entry_price = self._last_close
            self._long_entry_time = self._last_candle_time
            self._long_tp_price = self._long_entry_price + tp_pips * self._pip_size if tp_pips > 0 else 0.0
            self._long_stop_price = self._long_entry_price - sl_pips * self._pip_size if sl_pips > 0 else 0.0
            self._long_break_even = False
        else:
            self._long_entry_time = None
            self._long_tp_price = 0.0
            self._long_stop_price = 0.0
            if self.Position <= 0:
                self._long_entry_price = 0.0

        if self.Position < 0:
            self._short_entry_price = self._last_close
            self._short_entry_time = self._last_candle_time
            self._short_tp_price = self._short_entry_price - tp_pips * self._pip_size if tp_pips > 0 else 0.0
            self._short_stop_price = self._short_entry_price + sl_pips * self._pip_size if sl_pips > 0 else 0.0
            self._short_break_even = False
        else:
            self._short_entry_time = None
            self._short_tp_price = 0.0
            self._short_stop_price = 0.0
            if self.Position >= 0:
                self._short_entry_price = 0.0

    def CreateClone(self):
        return daily_trend_reversal_strategy()
