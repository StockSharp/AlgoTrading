import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.MatchingEngine")

from System import DateTime, TimeSpan, Math, Decimal
from System.Globalization import CultureInfo, DateTimeStyles
from StockSharp.Messages import DataType, Level1Fields, Sides, OrderTypes, OrderStates, ITickTradeMessage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.MatchingEngine import StopOrderCondition

_tick_price = clr.GetClrType(ITickTradeMessage).GetProperty("Price")
_tick_volume = clr.GetClrType(ITickTradeMessage).GetProperty("Volume")


class sample_detect_economic_calendar_strategy(Strategy):
    def __init__(self):
        super(sample_detect_economic_calendar_strategy, self).__init__()

        self._trade_news = self.Param("TradeNews", True)
        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._stop_loss = self.Param("StopLossPoints", 100).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPoints", 200).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPoints", 50).SetNotNegative()
        self._expiry_minutes = self.Param("ExpiryMinutes", 120).SetNotNegative()
        self._use_money_management = self.Param("UseMoneyManagement", False)
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero()
        self._buy_distance = self.Param("BuyDistancePoints", 10).SetGreaterThanZero()
        self._sell_distance = self.Param("SellDistancePoints", 10).SetGreaterThanZero()
        self._lead_minutes = self.Param("LeadMinutes", 15).SetNotNegative()
        self._post_minutes = self.Param("PostMinutes", 30).SetNotNegative()
        self._base_currency = self.Param("BaseCurrency", "USD")
        self._calendar_definition = self.Param("CalendarDefinition", "")

        self._pending_timer = None
        self._reset_state()
        self.Trades.TradeAdded += self._process_trade

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1), (self.Security, DataType.Ticks)]

    def OnReseted(self):
        super(sample_detect_economic_calendar_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        if self._pending_timer is not None:
            self._pending_timer.Dispose()
        self._pending_timer = None
        self._events = []
        self._best_bid = None
        self._best_ask = None
        self._armed_event = None
        self._buy_stop_order = None
        self._sell_stop_order = None
        self._winner = None
        self._exit_order = None
        self._cancel_pair = False
        self._cancel_requested = set()
        self._filled_position = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._trailing_price = None

    def OnStarted2(self, time):
        super(sample_detect_economic_calendar_strategy, self).OnStarted2(time)
        if self.Security.PriceStep is None or self.Security.PriceStep <= 0:
            raise ValueError("A positive security price step is required for news stops.")
        self._parse_calendar()
        # Real trades drive native stop matching; bid/ask updates need both one-sided bindings.
        self.Subscribe(Subscription(DataType.Ticks, self.Security))
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_level1).Start()

    def _process_level1(self, message):
        bid = message.Changes[Level1Fields.BestBidPrice] if message.Changes.ContainsKey(Level1Fields.BestBidPrice) else None
        ask = message.Changes[Level1Fields.BestAskPrice] if message.Changes.ContainsKey(Level1Fields.BestAskPrice) else None
        if bid is not None and bid > 0:
            self._best_bid = bid
        if ask is not None and ask > 0:
            self._best_ask = ask

        now = message.ServerTime
        self._process_pending(now)

        if self._best_bid is None or self._best_ask is None:
            return

        if self.Position != 0:
            self._manage_open_position()
            return

        exit_pending = self._exit_order is not None and self._exit_order.State in (OrderStates.Active, OrderStates.Pending)
        if (bool(self._trade_news.Value) and self._armed_event is None and not exit_pending
                and self.IsFormedAndOnlineAndAllowTrading()):
            self._try_arm(now)

    def _try_arm(self, now):
        currency = str(self._base_currency.Value)
        lead = int(self._lead_minutes.Value)
        post = int(self._post_minutes.Value)
        expiry = int(self._expiry_minutes.Value)

        candidates = [
            e for e in self._events
            if not e["consumed"] and e["high"] and e["currency"].lower() == currency.lower()
            and now >= e["time"].AddMinutes(-lead) and now < e["time"].AddMinutes(post)
            and (expiry == 0 or now < e["time"].AddMinutes(expiry))
        ]

        if not candidates:
            return

        event = min(candidates, key=lambda e: e["time"])
        point = self._point()
        volume = self._calculate_volume()
        if volume <= 0:
            raise ValueError("The configured volume limits admit no positive news order volume.")
        self._armed_event = event
        event["consumed"] = True
        self._cancel_pair = False
        self._winner = None
        self._cancel_requested.clear()
        self._buy_stop_order = self._create_stop(Sides.Buy, self._best_ask + Decimal(self._buy_distance.Value) * point, volume)
        self._sell_stop_order = self._create_stop(Sides.Sell, self._best_bid - Decimal(self._sell_distance.Value) * point, volume)
        self._pending_timer = self.StartTimer(TimeSpan.FromSeconds(1), self._timer_tick)
        self.RegisterOrder(self._buy_stop_order)
        self.RegisterOrder(self._sell_stop_order)

    def _create_stop(self, side, activation, volume):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Side = side
        order.Volume = volume
        order.Type = OrderTypes.Conditional
        condition = StopOrderCondition()
        condition.ActivationPrice = activation
        order.Condition = condition
        order.Comment = "Calendar " + self._armed_event["title"]
        return order

    def _timer_tick(self):
        self._process_pending(self.CurrentTime)

    def _is_expired(self, now):
        post_expired = now >= self._armed_event["time"].AddMinutes(int(self._post_minutes.Value))
        expiry = int(self._expiry_minutes.Value)
        life_expired = expiry > 0 and now >= self._armed_event["time"].AddMinutes(expiry)
        return post_expired or life_expired

    def _process_pending(self, now):
        if self._armed_event is None:
            return
        self._cancel_pair = (self._cancel_pair or not bool(self._trade_news.Value) or self._is_expired(now)
                             or self._buy_stop_order.State == OrderStates.Failed
                             or self._sell_stop_order.State == OrderStates.Failed)
        if self._cancel_pair or (self._winner is not None and self._winner != self._buy_stop_order):
            self._request_cancel(self._buy_stop_order)
        if self._cancel_pair or (self._winner is not None and self._winner != self._sell_stop_order):
            self._request_cancel(self._sell_stop_order)
        # A cancel request is not a terminal acknowledgement. Keep delayed registrations tracked.
        terminal = (OrderStates.Done, OrderStates.Failed)
        if self._buy_stop_order.State in terminal and self._sell_stop_order.State in terminal:
            self._armed_event = None
            self._buy_stop_order = self._sell_stop_order = self._winner = None
            if self._pending_timer is not None:
                self._pending_timer.Dispose()
                self._pending_timer = None

    def _request_cancel(self, order):
        if order.State == OrderStates.Active and order.TransactionId not in self._cancel_requested:
            self._cancel_requested.add(order.TransactionId)
            self.CancelOrder(order)

    def OnOrderRegistered(self, order):
        super(sample_detect_economic_calendar_strategy, self).OnOrderRegistered(order)
        if self._armed_event is not None and (order == self._buy_stop_order or order == self._sell_stop_order):
            self._process_pending(self.CurrentTime)

    def _process_trade(self, trade):
        price = _tick_price.GetValue(trade.Trade)
        volume = _tick_volume.GetValue(trade.Trade)
        signed = volume if trade.Order.Side == Sides.Buy else -volume
        previous = self._filled_position
        if previous == 0 or Math.Sign(previous) == Math.Sign(signed):
            self._entry_value += price * volume
            self._filled_position += signed
        else:
            self._entry_value -= self._entry_value / Math.Abs(previous) * Math.Min(volume, Math.Abs(previous))
            self._filled_position += signed
            if self._filled_position != 0 and Math.Sign(self._filled_position) != Math.Sign(previous):
                self._entry_value = price * Math.Abs(self._filled_position)
        if self._filled_position == 0:
            self._entry_value = Decimal.Zero
        if Math.Sign(previous) != Math.Sign(self._filled_position):
            self._trailing_price = None
        if trade.Order == self._buy_stop_order or trade.Order == self._sell_stop_order:
            if self._winner is None:
                self._winner = trade.Order
            self._request_cancel(self._sell_stop_order if trade.Order == self._buy_stop_order else self._buy_stop_order)

    def _manage_open_position(self):
        if (self._exit_order is not None and self._exit_order.State in (OrderStates.Pending, OrderStates.Active)) or self._filled_position == 0:
            return
        long_position = self.Position > 0
        direction = Decimal(1) if long_position else Decimal(-1)
        price = self._best_bid if long_position else self._best_ask
        entry = self._entry_value / Math.Abs(self._filled_position)
        point = self._point()
        trail = int(self._trailing_stop.Value)
        sl = int(self._stop_loss.Value)
        tp = int(self._take_profit.Value)
        stop = entry - direction * Decimal(sl) * point if sl > 0 else None
        if trail > 0 and direction * (price - entry) >= Decimal(trail) * point:
            candidate = price - direction * Decimal(trail) * point
            if self._trailing_price is None or direction * (candidate - self._trailing_price) > 0:
                self._trailing_price = candidate
        if self._trailing_price is not None and (stop is None or direction * (self._trailing_price - stop) > 0):
            stop = self._trailing_price
        stop_hit = stop is not None and direction * (price - stop) <= 0
        take_hit = tp > 0 and direction * (price - entry) >= Decimal(tp) * point
        if stop_hit or take_hit:
            self._cancel_pair = True
            self._process_pending(self.CurrentTime)
            self._exit_order = self.SellMarket(Math.Abs(self.Position)) if long_position else self.BuyMarket(Math.Abs(self.Position))

    def _calculate_volume(self):
        if not bool(self._use_money_management.Value) or int(self._stop_loss.Value) <= 0:
            return self._normalize_volume(Decimal(self._order_volume.Value))

        balance = Decimal.Zero
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else self.Portfolio.BeginValue
            balance = value if value is not None else Decimal.Zero

        if balance <= 0:
            return self._normalize_volume(Decimal(self._order_volume.Value))

        risk_money = balance * Decimal(self._risk_percent.Value) / Decimal(100)
        step_price = self.Security.StepPrice if self.Security.StepPrice is not None else Decimal.Zero
        point = self._point()
        multiplier = self.Security.Multiplier if self.Security.Multiplier is not None else Decimal(1)
        loss_per_unit = Decimal(self._stop_loss.Value) * (step_price if step_price > 0 else point * multiplier)

        if loss_per_unit <= 0:
            return self._normalize_volume(Decimal(self._order_volume.Value))

        return self._normalize_volume(risk_money / loss_per_unit)

    def _normalize_volume(self, volume):
        minimum = Math.Max(Decimal.Zero, self.Security.MinVolume if self.Security.MinVolume is not None else Decimal.Zero)
        maximum = self.Security.MaxVolume if self.Security.MaxVolume is not None and self.Security.MaxVolume > 0 else Decimal.MaxValue
        if self.Security.VolumeStep is not None and self.Security.VolumeStep > 0:
            step = self.Security.VolumeStep
            minimum = Math.Ceiling(minimum / step) * step
            if maximum != Decimal.MaxValue:
                maximum = Math.Floor(maximum / step) * step
            volume = Math.Floor(Math.Min(volume, maximum) / step) * step
        if minimum > maximum:
            raise ValueError("The security volume grid has no quantity within its min/max limits.")
        return Math.Max(minimum, Math.Min(volume, maximum))

    def _point(self):
        return self.Security.PriceStep

    def _parse_calendar(self):
        self._events = []
        formats = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm", "yyyy/MM/dd HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss"]

        for raw in str(self._calendar_definition.Value or "").replace("\r", "").split("\n"):
            if not raw.strip():
                continue
            parts = raw.split(";")
            if len(parts) < 4:
                continue

            parsed = DateTime()
            ok = False
            for fmt in formats:
                try:
                    parsed = DateTime.ParseExact(
                        parts[0].strip(), fmt, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
                    ok = True
                    break
                except Exception:
                    pass

            if not ok:
                continue

            importance = parts[2].strip().lower()
            self._events.append({
                "time": parsed,
                "currency": parts[1].strip(),
                "high": importance == "high",
                "title": ";".join(parts[3:]).strip(),
                "consumed": False,
            })

    def CreateClone(self):
        return sample_detect_economic_calendar_strategy()
