import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, TimeZoneInfo, DateTime, DateTimeKind, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides, ITickTradeMessage, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

_tick_price = clr.GetClrType(ITickTradeMessage).GetProperty("Price")
_tick_volume = clr.GetClrType(ITickTradeMessage).GetProperty("Volume")


class silver_midnight_candle_color_strategy(Strategy):
    """Previous New York day's midnight H1 colour, 01:00 entry and actual-fill tick SL/TP."""

    def __init__(self):
        super(silver_midnight_candle_color_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._take_profit_long_ticks = self.Param("TakeProfitLongTicks", 57).SetNotNegative()
        self._take_profit_short_ticks = self.Param("TakeProfitShortTicks", 48).SetNotNegative()
        self._stop_loss_ticks = self.Param("StopLossTicks", 200).SetNotNegative()
        self._new_york = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
        self._reset_state()
        self.Trades.TradeAdded += self._process_trade

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(silver_midnight_candle_color_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._midnight = {}
        self._entered_day = None
        self._entry_side = Sides.Buy
        self._entry_value = Decimal.Zero
        self._entry_volume = Decimal.Zero
        self._bid = Decimal.Zero
        self._ask = Decimal.Zero
        self._exit_pending = False

        self._exit_order = None

    def OnStarted2(self, time):
        super(silver_midnight_candle_color_strategy, self).OnStarted2(time)
        if self._candle_type.Value != DataType.TimeFrame(TimeSpan.FromHours(1)):
            raise ValueError("The midnight colour is defined by a one-hour candle.")
        if self.Security.PriceStep is None or self.Security.PriceStep <= 0:
            raise ValueError("A positive security price step is required for tick SL/TP.")
        self._reset_state()
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._process_candle).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _to_new_york(self, utc):
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), self._new_york)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        local = self._to_new_york(candle.OpenTime)
        if local.TimeOfDay == TimeSpan.Zero:
            self._midnight[local.Date] = Sides.Buy if candle.ClosePrice > candle.OpenPrice else Sides.Sell
        for day in list(self._midnight):
            if day < local.Date.AddDays(-1):
                del self._midnight[day]

    def _process_trade(self, trade):
        price = _tick_price.GetValue(trade.Trade)
        volume = _tick_volume.GetValue(trade.Trade)
        if trade.Order.Side == self._entry_side:
            # Read the interface contract, including explicitly implemented CLR properties.
            self._entry_value += price * volume
            self._entry_volume += volume
        elif self._entry_volume > 0:
            closed = Math.Min(volume, self._entry_volume)
            self._entry_value -= self._entry_value / self._entry_volume * closed
            self._entry_volume -= closed
            if self._entry_volume == 0:
                self._entry_value = Decimal.Zero
        if self.Position == 0:
            self._exit_pending = False

    def _process_quote(self, quote):
        if quote.Changes.ContainsKey(Level1Fields.BestBidPrice):
            self._bid = quote.Changes[Level1Fields.BestBidPrice]
        if quote.Changes.ContainsKey(Level1Fields.BestAskPrice):
            self._ask = quote.Changes[Level1Fields.BestAskPrice]
        if self.Position != 0:
            self._check_protection()
            return
        local = self._to_new_york(quote.ServerTime)
        side = self._midnight.get(local.Date.AddDays(-1))
        if (local.Hour != 1 or local.Minute != 0 or self._entered_day == local.Date or side is None or
                not self.IsFormedAndOnlineAndAllowTrading()):
            return
        self._entered_day = local.Date
        self._entry_side = side
        self._entry_value = Decimal.Zero
        self._entry_volume = Decimal.Zero
        self._exit_pending = False
        if side == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

    def _check_protection(self):
        # A late entry fill can leave residual exposure after a completed exit.
        if self._exit_order is not None and self._exit_order.State in (OrderStates.Done, OrderStates.Failed):
            self._exit_pending = False
            self._exit_order = None
        if self._exit_pending or self._entry_volume <= 0:
            return
        price = self._bid if self.Position > 0 else self._ask
        if price <= 0:
            return
        entry = self._entry_value / self._entry_volume
        step = self.Security.PriceStep
        target_ticks = self._take_profit_long_ticks.Value if self.Position > 0 else self._take_profit_short_ticks.Value
        direction = Decimal(1) if self.Position > 0 else Decimal(-1)
        target = entry + direction * Decimal(target_ticks) * step
        stop = entry - direction * Decimal(self._stop_loss_ticks.Value) * step
        take_hit = target_ticks > 0 and (price >= target if self.Position > 0 else price <= target)
        stop_hit = self._stop_loss_ticks.Value > 0 and (price <= stop if self.Position > 0 else price >= stop)
        if not take_hit and not stop_hit:
            return
        self._exit_pending = True
        if self.Position > 0:
            self._exit_order = self.SellMarket(self.Position)
        else:
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return silver_midnight_candle_color_strategy()
