import clr

clr.AddReference("Ecng.Common")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateOnly, DateTime, DateTimeKind, DayOfWeek, Decimal, TimeSpan, TimeZoneInfo, ArgumentException
from Ecng.Common import TimeHelper
from StockSharp.Messages import DataType, CandleStates, OrderStates, OrderTypes, Sides, Level1Fields, ITickTradeMessage
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class power_hour_money_strategy(Strategy):
    def __init__(self):
        super(power_hour_money_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying causal calendar opens and closes", "General")
        self._session_start = self.Param("SessionStart", TimeSpan.FromMinutes(570)) \
            .SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)) \
            .SetDisplay("Session Start", "Inclusive New York wall-clock time", "Sessions")
        self._session_end = self.Param("SessionEnd", TimeSpan.FromMinutes(690)) \
            .SetRange(TimeSpan.FromTicks(1), TimeSpan.FromDays(1)) \
            .SetDisplay("Session End", "Exclusive New York wall-clock time", "Sessions")
        self._is_all_sessions = self.Param("IsAllSessions", False) \
            .SetDisplay("All Sessions", "Allow entries throughout the day", "Sessions")
        self._is_end_of_day_close_enabled = self.Param("IsEndOfDayCloseEnabled", False) \
            .SetDisplay("End of Day Close", "Close at the cutoff and block further entries until New York midnight", "Protection")
        self._end_of_day_time = self.Param("EndOfDayTime", TimeSpan.FromMinutes(1005)) \
            .SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1)) \
            .SetDisplay("End of Day Time", "New York wall-clock cutoff", "Protection")
        self._is_trailing_enabled = self.Param("IsTrailingEnabled", False) \
            .SetDisplay("Trailing Enabled", "Protect both sides on real executable Level1 quotes", "Protection")
        self._long_trailing_percent = self.Param("LongTrailingPercent", Decimal.Parse("2")).SetGreaterThanZero() \
            .SetDisplay("Long Trailing Percent", "Percent below the highest observed bid since entry", "Protection")
        self._short_trailing_percent = self.Param("ShortTrailingPercent", Decimal.Parse("2")).SetGreaterThanZero() \
            .SetDisplay("Short Trailing Percent", "Percent above the lowest observed ask since entry", "Protection")
        self._reset_state()
        self.OrderRegistering += self._observe_order
        self.OwnTradeReceived += self._observe_fill

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._month = self._week = self._day = self._hour = None
        self._month_open = self._week_open = self._day_open = self._hour_open = None
        self._pending_order = None
        self._held_side = None
        self._extreme = Decimal.Zero

    def _observe_order(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(power_hour_money_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(power_hour_money_strategy, self).OnStarted2(time)
        if self._session_start.Value < TimeSpan.Zero or self._session_start.Value >= self._session_end.Value \
                or self._session_end.Value > TimeSpan.FromDays(1) or self._end_of_day_time.Value < TimeSpan.Zero \
                or self._end_of_day_time.Value >= TimeSpan.FromDays(1) \
                or self._long_trailing_percent.Value <= 0 or self._long_trailing_percent.Value >= 100 \
                or self._short_trailing_percent.Value <= 0 or self._short_trailing_percent.Value >= 100:
            raise ArgumentException("Sessions must be ordered within a day and trailing percentages within (0, 100).")
        self._reset_state()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_quote).Start()
        if self._is_end_of_day_close_enabled.Value:
            self.StartTimer(TimeSpan.FromMinutes(1), self._try_end_of_day)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _is_pending(self):
        order = self._pending_order
        return order is not None and order.State != OrderStates.Done and order.State != OrderStates.Failed

    def _submit(self, side, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        order.Comment = comment
        self.RegisterOrder(order)

    def _try_end_of_day(self):
        if not self._is_end_of_day_close_enabled.Value \
                or TimeZoneInfo.ConvertTimeFromUtc(self.CurrentTime, TimeHelper.Est).TimeOfDay < self._end_of_day_time.Value:
            return False
        if self.Position != 0 and not self._is_pending() and self.IsFormedAndOnlineAndAllowTrading():
            self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), "Power Hour end-of-day close")
        return True

    def _observe_fill(self, subscription, trade):
        # Reflection reads the explicit CLR interface member for every tick-message implementation.
        price = clr.GetClrType(ITickTradeMessage).GetProperty("Price").GetValue(trade.Trade)
        side = Sides.Buy if self.Position > 0 else Sides.Sell if self.Position < 0 else None
        if side != self._held_side:
            self._extreme = Decimal.Zero if side is None else price
        elif side is not None:
            self._extreme = max(self._extreme, price) if side == Sides.Buy else min(self._extreme, price)
        self._held_side = side

    def _observe_quote(self, quote):
        if self._try_end_of_day() or self._held_side is None or self.Position == 0:
            return
        side = self._held_side
        field = Level1Fields.BestBidPrice if side == Sides.Buy else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = quote.Changes[field]
        if price <= 0:
            return
        self._extreme = max(self._extreme, price) if side == Sides.Buy else min(self._extreme, price)
        percent = self._long_trailing_percent.Value if side == Sides.Buy else self._short_trailing_percent.Value
        stop = self._extreme * (Decimal(1) - percent / Decimal(100)) if side == Sides.Buy \
            else self._extreme * (Decimal(1) + percent / Decimal(100))
        triggered = price <= stop if side == Sides.Buy else price >= stop
        if self._is_trailing_enabled.Value and not self._is_pending() and self.IsFormedAndOnlineAndAllowTrading() and triggered:
            self._submit(Sides.Sell if side == Sides.Buy else Sides.Buy, abs(self.Position), "Power Hour trailing close")

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime, TimeHelper.Est)
        day = DateOnly.FromDateTime(local)
        month = DateOnly(local.Year, local.Month, 1)
        week = day.AddDays(-((int(local.DayOfWeek) + 6) % 7))
        hour = DateTime(candle.OpenTime.Year, candle.OpenTime.Month, candle.OpenTime.Day, candle.OpenTime.Hour, 0, 0, DateTimeKind.Utc)
        midnight = local.TimeOfDay == TimeSpan.Zero
        if self._month != month:
            self._month_open = candle.OpenPrice if self._month is not None or local.Day == 1 and midnight else None
            self._month = month
        if self._week != week:
            self._week_open = candle.OpenPrice if self._week is not None or local.DayOfWeek == DayOfWeek.Monday and midnight else None
            self._week = week
        if self._day != day:
            self._day_open = candle.OpenPrice if self._day is not None or midnight else None
            self._day = day
        if self._hour != hour:
            self._hour_open = candle.OpenPrice if self._hour is not None or candle.OpenTime == hour else None
            self._hour = hour
        if self._try_end_of_day() or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading() \
                or any(value is None for value in (self._month_open, self._week_open, self._day_open, self._hour_open)):
            return
        now = TimeZoneInfo.ConvertTimeFromUtc(self.CurrentTime, TimeHelper.Est).TimeOfDay
        if not self._is_all_sessions.Value and (now < self._session_start.Value or now >= self._session_end.Value):
            return
        close = candle.ClosePrice
        opens = (self._month_open, self._week_open, self._day_open, self._hour_open)
        if all(close > value for value in opens) and self.Position <= 0:
            self._submit(Sides.Buy, self.Volume + abs(self.Position), "Power Hour consensus entry")
        elif all(close < value for value in opens) and self.Position >= 0:
            self._submit(Sides.Sell, self.Volume + abs(self.Position), "Power Hour consensus entry")

    def CreateClone(self):
        return power_hour_money_strategy()
