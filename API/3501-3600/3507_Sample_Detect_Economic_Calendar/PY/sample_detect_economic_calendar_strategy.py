import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, TimeSpan, Math
from System.Globalization import CultureInfo, DateTimeStyles
from StockSharp.Messages import DataType, Level1Fields, Sides
from StockSharp.Algo.Strategies import Strategy


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

        self._events = []
        self._best_bid = None
        self._best_ask = None
        self._armed_event = None
        self._armed_at = None
        self._buy_stop = None
        self._sell_stop = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1)]

    def OnReseted(self):
        super(sample_detect_economic_calendar_strategy, self).OnReseted()
        self._events = []
        self._best_bid = None
        self._best_ask = None
        self._clear_pending()
        self._reset_protection()

    def OnStarted2(self, time):
        super(sample_detect_economic_calendar_strategy, self).OnStarted2(time)
        self._parse_calendar()
        self.SubscribeLevel1().Bind(self._process_level1).Start()

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None and float(bid) > 0:
            self._best_bid = float(bid)
        if ask is not None and float(ask) > 0:
            self._best_ask = float(ask)

        if self._best_bid is None or self._best_ask is None:
            return

        now = message.ServerTime

        if self.Position != 0:
            self._manage_open_position()
            return

        if not bool(self._trade_news.Value):
            self._clear_pending()
            return

        if self._armed_event is not None and self._is_expired(now):
            self._clear_pending()

        if self._armed_event is None:
            self._try_arm(now)

        if self._armed_event is None:
            return

        if self._buy_stop is not None and self._best_ask >= self._buy_stop:
            self._execute(Sides.Buy, self._best_ask)
            return

        if self._sell_stop is not None and self._best_bid <= self._sell_stop:
            self._execute(Sides.Sell, self._best_bid)

    def _try_arm(self, now):
        currency = str(self._base_currency.Value)
        lead = int(self._lead_minutes.Value)
        post = int(self._post_minutes.Value)

        candidates = [
            e for e in self._events
            if not e["consumed"] and e["high"] and e["currency"].lower() == currency.lower()
            and now >= e["time"].AddMinutes(-lead) and now <= e["time"].AddMinutes(post)
        ]

        if not candidates:
            return

        event = min(candidates, key=lambda e: e["time"])
        point = self._point()
        self._armed_event = event
        self._armed_at = now
        self._buy_stop = self._best_ask + int(self._buy_distance.Value) * point
        self._sell_stop = self._best_bid - int(self._sell_distance.Value) * point

    def _is_expired(self, now):
        if self._armed_event is None or self._armed_at is None:
            return True

        post_expired = now > self._armed_event["time"].AddMinutes(int(self._post_minutes.Value))
        expiry = int(self._expiry_minutes.Value)
        life_expired = expiry > 0 and now > self._armed_at.AddMinutes(expiry)
        return post_expired or life_expired

    def _execute(self, side, price):
        volume = self._calculate_volume()
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._armed_event["consumed"] = True
        self._clear_pending(keep_consumed=True)

        point = self._point()
        sl = int(self._stop_loss.Value)
        tp = int(self._take_profit.Value)
        self._entry_price = price
        self._best_price = price
        self._stop_price = (price - sl * point if side == Sides.Buy else price + sl * point) if sl > 0 else None
        self._take_price = (price + tp * point if side == Sides.Buy else price - tp * point) if tp > 0 else None

    def _manage_open_position(self):
        point = self._point()
        trail = int(self._trailing_stop.Value)

        if self.Position > 0:
            price = self._best_bid
            self._best_price = price if self._best_price is None else max(self._best_price, price)
            if trail > 0 and price - self._entry_price >= trail * point:
                candidate = price - trail * point
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate
            if ((self._stop_price is not None and price <= self._stop_price) or
                    (self._take_price is not None and price >= self._take_price)):
                self.SellMarket(Math.Abs(self.Position))
                self._reset_protection()
        else:
            price = self._best_ask
            self._best_price = price if self._best_price is None else min(self._best_price, price)
            if trail > 0 and self._entry_price - price >= trail * point:
                candidate = price + trail * point
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate
            if ((self._stop_price is not None and price >= self._stop_price) or
                    (self._take_price is not None and price <= self._take_price)):
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_protection()

    def _calculate_volume(self):
        if not bool(self._use_money_management.Value) or int(self._stop_loss.Value) <= 0:
            return self._normalize_volume(float(self._order_volume.Value))

        balance = 0.0
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else self.Portfolio.BeginValue
            balance = float(value) if value is not None else 0.0

        if balance <= 0:
            return self._normalize_volume(float(self._order_volume.Value))

        risk_money = balance * float(self._risk_percent.Value) / 100.0
        step_price = float(self.Security.StepPrice) if self.Security is not None and self.Security.StepPrice is not None else 0.0
        point = self._point()
        multiplier = float(self.Security.Multiplier) if self.Security is not None and self.Security.Multiplier is not None else 1.0
        loss_per_unit = int(self._stop_loss.Value) * step_price if step_price > 0 else int(self._stop_loss.Value) * point * multiplier

        if loss_per_unit <= 0:
            return self._normalize_volume(float(self._order_volume.Value))

        return self._normalize_volume(risk_money / loss_per_unit)

    def _normalize_volume(self, volume):
        if self.Security is not None:
            if self.Security.MaxVolume is not None and float(self.Security.MaxVolume) > 0:
                volume = min(volume, float(self.Security.MaxVolume))
            if self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
                volume = max(volume, float(self.Security.MinVolume))
            if self.Security.VolumeStep is not None and float(self.Security.VolumeStep) > 0:
                step = float(self.Security.VolumeStep)
                volume = math.floor(volume / step) * step
        return volume if volume > 0 else float(self._order_volume.Value)

    def _point(self):
        point = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        return point if point > 0 else 1.0

    def _parse_calendar(self):
        self._events = []
        formats = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm", "dd.MM.yyyy HH:mm"]

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
                "high": importance in ("high", "nfp"),
                "title": ";".join(parts[3:]).strip(),
                "consumed": False,
            })

    def _clear_pending(self, keep_consumed=False):
        if not keep_consumed and self._armed_event is not None:
            self._armed_event["consumed"] = True
        self._armed_event = None
        self._armed_at = None
        self._buy_stop = None
        self._sell_stop = None

    def _reset_protection(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._best_price = None

    def CreateClone(self):
        return sample_detect_economic_calendar_strategy()
