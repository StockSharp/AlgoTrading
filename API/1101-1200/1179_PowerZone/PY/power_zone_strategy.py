import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import ArgumentException, Decimal, Math, TimeSpan
from StockSharp.Messages import CandleStates, DataType, ITickTradeMessage, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class power_zone_strategy(Strategy):
    def __init__(self):
        super(power_zone_strategy, self).__init__()
        self._periods = self.Param("Periods", 5).SetGreaterThanZero() \
            .SetDisplay("Periods", "Completed same-color confirmation candles after the opposite anchor", "Zones")
        self._threshold = self.Param("Threshold", Decimal.Zero).SetRange(Decimal.Zero, Decimal.MaxValue) \
            .SetDisplay("Threshold", "Minimum absolute percent move from anchor close to final confirmation close", "Zones")
        self._use_wicks = self.Param("UseWicks", False) \
            .SetDisplay("Use Wicks", "Use anchor high/low instead of body boundaries", "Zones")
        self._take_profit_factor = self.Param("TakeProfitFactor", Decimal.Parse("1.5")).SetGreaterThanZero() \
            .SetDisplay("Take Profit Factor", "Frozen entry-zone range multiple above/below actual fill basis", "Protection")
        self._stop_loss_factor = self.Param("StopLossFactor", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("Stop Loss Factor", "Frozen entry-zone range multiple below/above actual fill basis", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished zone confirmations and subsequent breakout decisions", "General")
        tick_type = clr.GetClrType(ITickTradeMessage)
        self._tick_price = tick_type.GetProperty("Price")
        self._tick_volume = tick_type.GetProperty("Volume")
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
        self._bars = []
        self._bull = self._bear = None
        self._bull_used = self._bear_used = False
        self._pending_order = None
        self._filled_position = self._entry_price = self._stop_distance = self._take_distance = Decimal.Zero

    def OnReseted(self):
        super(power_zone_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(power_zone_strategy, self).OnStarted2(time)
        if self._periods.Value <= 0 or self._threshold.Value < 0 \
                or self._take_profit_factor.Value <= 0 or self._stop_loss_factor.Value <= 0:
            raise ArgumentException("Periods and risk factors must be positive; movement threshold cannot be negative.")
        self._reset_state()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_quote).Start()
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

    def _observe_fill(self, subscription, trade):
        previous = self._filled_position
        price = self._tick_price.GetValue(trade.Trade)
        volume = self._tick_volume.GetValue(trade.Trade)
        signed = volume if trade.Order.Side == Sides.Buy else -volume
        self._filled_position += signed
        if self._filled_position == 0:
            self._entry_price = Decimal.Zero
        elif previous == 0 or Math.Sign(self._filled_position) != Math.Sign(previous):
            self._entry_price = price
        elif Math.Sign(signed) == Math.Sign(previous):
            self._entry_price = (self._entry_price * abs(previous) + price * abs(signed)) / abs(self._filled_position)

    def _try_risk_exit(self, price):
        if self.Position == 0 or self._entry_price <= 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return False
        profit = (price - self._entry_price) * Math.Sign(self.Position)
        comment = "PowerZone stop loss" if profit <= -self._stop_distance \
            else "PowerZone take profit" if profit >= self._take_distance else None
        if comment is None:
            return False
        self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), comment)
        return True

    def _observe_quote(self, quote):
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if quote.Changes.ContainsKey(field) and quote.Changes[field] > 0:
            self._try_risk_exit(quote.Changes[field])

    def _confirm_zones(self):
        if len(self._bars) < self._periods.Value + 2:
            return
        anchor = self._bars[0]
        confirmation = self._bars[1:-1]
        bull = anchor[3] < anchor[0] and all(bar[3] > bar[0] for bar in confirmation)
        bear = anchor[3] > anchor[0] and all(bar[3] < bar[0] for bar in confirmation)
        if not bull and not bear or anchor[3] == 0 \
                or Decimal(100) * abs(confirmation[-1][3] - anchor[3]) / abs(anchor[3]) < self._threshold.Value:
            return
        high = anchor[1] if self._use_wicks.Value else max(anchor[0], anchor[3])
        low = anchor[2] if self._use_wicks.Value else min(anchor[0], anchor[3])
        if high <= low:
            return
        if bull:
            self._bull = (high, low)
            self._bull_used = False
        else:
            self._bear = (high, low)
            self._bear_used = False

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bars.append((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice))
        if len(self._bars) > self._periods.Value + 2:
            self._bars.pop(0)
        self._confirm_zones()
        if self._try_risk_exit(candle.ClosePrice) or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        side = None
        width = Decimal.Zero
        if not self._bull_used and self._bull is not None and candle.ClosePrice > self._bull[0] and self.Position <= 0:
            side = Sides.Buy
            width = self._bull[0] - self._bull[1]
            self._bull_used = True
        elif not self._bear_used and self._bear is not None and candle.ClosePrice < self._bear[1] and self.Position >= 0:
            side = Sides.Sell
            width = self._bear[0] - self._bear[1]
            self._bear_used = True
        if side is not None:
            self._stop_distance = width * self._stop_loss_factor.Value
            self._take_distance = width * self._take_profit_factor.Value
            self._submit(side, self.Volume + abs(self.Position), "PowerZone breakout entry")

    def CreateClone(self):
        return power_zone_strategy()
