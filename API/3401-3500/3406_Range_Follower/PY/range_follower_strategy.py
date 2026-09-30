import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides, Unit, OrderStates, ITickTradeMessage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageTrueRange

_tick_price = clr.GetClrType(ITickTradeMessage).GetProperty("Price")
_tick_volume = clr.GetClrType(ITickTradeMessage).GetProperty("Volume")


class range_follower_strategy(Strategy):
    def __init__(self):
        super(range_follower_strategy, self).__init__()
        self.Volume = Decimal(0.1)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._trigger_percent = self.Param("TriggerPercent", 60.0).SetRange(10.0, 90.0)

        self._reset_state()
        self.Trades.TradeAdded += self._process_trade
        self.OrderRegistering += self._track_exit

    def GetWorkingSecurities(self):
        return [
            (self.Security, self._candle_type.Value),
            (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1))),
            (self.Security, DataType.Level1),
        ]

    def OnReseted(self):
        super(range_follower_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._daily_atr = None
        self._best_bid = None
        self._best_ask = None
        self._take_distance = None
        self._stop_distance = None
        self._entry_order = None
        self._exit_order = None
        self._entry_side = Sides.Buy
        self._entry_volume = Decimal.Zero
        self._entry_value = Decimal.Zero
        self._reset_session(None)

    def OnStarted2(self, time):
        super(range_follower_strategy, self).OnStarted2(time)
        atr = AverageTrueRange()
        atr.Length = 20
        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).BindEx(atr, self._process_daily).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_level1).Start()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_working).Start()

    def _process_daily(self, candle, value):
        if candle.State != CandleStates.Finished or not value.IsFormed:
            return
        self._daily_atr = value.GetValue[Decimal](None)

    def _process_level1(self, message):
        bid = message.Changes[Level1Fields.BestBidPrice] if message.Changes.ContainsKey(Level1Fields.BestBidPrice) else None
        ask = message.Changes[Level1Fields.BestAskPrice] if message.Changes.ContainsKey(Level1Fields.BestAskPrice) else None
        if bid is not None and bid > 0:
            self._best_bid = bid
        if ask is not None and ask > 0:
            self._best_ask = ask
        if self._best_bid is not None and self._best_ask is not None:
            if self._session_date == message.ServerTime.Date:
                self._session_high = max(self._session_high, self._best_bid, self._best_ask)
                self._session_low = min(self._session_low, self._best_bid, self._best_ask)
            if self.Position > 0:
                self._apply_protection(self._best_bid, self._best_bid)
            elif self.Position < 0:
                self._apply_protection(self._best_ask, self._best_ask)
        self._evaluate_quote()

    def _process_working(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        high = candle.HighPrice
        low = candle.LowPrice

        if self._session_date != date:
            if self._session_date is not None and self.Position != 0:
                self._flatten()
            self._reset_session(date)
            self._session_high = high
            self._session_low = low
            self._session_atr = self._daily_atr
            if self._session_atr is not None:
                self._skip_today = high - low > self._session_atr * Decimal(self._trigger_percent.Value) / Decimal(100)
        else:
            self._session_high = max(self._session_high, high)
            self._session_low = min(self._session_low, low)

        if self.Position != 0:
            self._apply_protection(high, low)

        self._evaluate_quote()

    def _evaluate_quote(self):
        if (self._session_atr is None or self._session_date is None or
                self.CurrentTime.Date != self._session_date or self._traded_today or
                self._skip_today or self.Position != 0 or self._best_bid is None or self._best_ask is None or
                self._session_low == 0 or self._session_high == 0):
            return

        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order) or not self.IsFormedAndOnlineAndAllowTrading():
            return

        trigger = self._session_atr * Decimal(self._trigger_percent.Value) / Decimal(100)
        residual = self._session_atr - trigger
        long_distance = self._best_bid - self._session_low
        short_distance = self._session_high - self._best_ask

        if long_distance <= trigger and short_distance <= trigger:
            return

        if long_distance >= short_distance:
            self._enter(Sides.Buy, trigger, residual)
        else:
            self._enter(Sides.Sell, trigger, residual)

    def _is_pending(self, order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _enter(self, side, trigger, residual):
        if self._take_distance is None:
            self._take_distance = Unit(residual)
            self._stop_distance = Unit(trigger)
            self.StartProtection(self._take_distance, self._stop_distance, useMarketOrders=True, isLocalStop=True)
        else:
            # Preserve Unit references held by the native controller; update only between flat sessions.
            self._take_distance.Value = residual
            self._stop_distance.Value = trigger
        self._traded_today = True
        self._entry_side = side
        self._entry_volume = self._entry_value = Decimal.Zero
        self._entry_order = self.BuyMarket() if side == Sides.Buy else self.SellMarket()

    def _track_exit(self, order):
        if self.Position != 0 and order.Side == (Sides.Sell if self.Position > 0 else Sides.Buy):
            self._exit_order = order

    def _process_trade(self, trade):
        price = _tick_price.GetValue(trade.Trade)
        volume = _tick_volume.GetValue(trade.Trade)
        if trade.Order.Side == self._entry_side:
            self._entry_volume += volume
            self._entry_value += price * volume
        elif self._entry_volume > 0:
            closed = Math.Min(self._entry_volume, volume)
            self._entry_value -= self._entry_value / self._entry_volume * closed
            self._entry_volume -= closed
        if self._entry_volume > 0:
            entry = self._entry_value / self._entry_volume
            direction = Decimal(1) if self._entry_side == Sides.Buy else Decimal(-1)
            self._stop_price = entry - direction * self._stop_distance.Value
            self._take_price = entry + direction * self._take_distance.Value
        else:
            self._entry_value = Decimal.Zero
            self._stop_price = self._take_price = None

    def _apply_protection(self, high, low):
        if self.Position > 0 and ((self._stop_price is not None and low <= self._stop_price) or
                                  (self._take_price is not None and high >= self._take_price)):
            self._flatten()
        elif self.Position < 0 and ((self._stop_price is not None and high >= self._stop_price) or
                                    (self._take_price is not None and low <= self._take_price)):
            self._flatten()

    def _flatten(self):
        if self._is_pending(self._exit_order):
            return
        if self.Position > 0:
            self._exit_order = self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))

    def _reset_session(self, date):
        self._session_date = date
        self._session_atr = None
        self._session_high = Decimal.Zero
        self._session_low = Decimal.Zero
        self._traded_today = False
        self._skip_today = False
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return range_follower_strategy()
