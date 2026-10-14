import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import ArgumentException, Decimal, Int32, Math, TimeSpan
from StockSharp.Messages import CandleStates, DataType, ITickTradeMessage, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class power_house_swift_edge_ai_v2_10_strategy(Strategy):
    def __init__(self):
        super(power_house_swift_edge_ai_v2_10_strategy, self).__init__()
        self._pivot_period = self.Param("PivotPeriod", 2).SetGreaterThanZero() \
            .SetDisplay("Pivot Period", "Strict neighboring bars on each side of a confirmed pivot", "Signals")
        self._momentum_period = self.Param("MomentumPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Finished-bar close lookback", "Signals")
        self._momentum_threshold = self.Param("MomentumThreshold", Decimal.Zero).SetRange(Decimal.Zero, Decimal.MaxValue) \
            .SetDisplay("Momentum Threshold", "Minimum signed close change in absolute price units", "Signals")
        self._min_signal_bars = self.Param("MinSignalBars", 5).SetRange(0, Int32.MaxValue) \
            .SetDisplay("Minimum Signal Bars", "Finished-bar interval between accepted entries; zero disables spacing", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", Decimal(50)).SetGreaterThanZero() \
            .SetDisplay("Stop Loss Points", "Fixed absolute price distance from the actual fill basis", "Protection")
        self._take_profit_points = self.Param("TakeProfitPoints", Decimal(100)).SetGreaterThanZero() \
            .SetDisplay("Take Profit Points", "Fixed absolute price distance from the actual fill basis", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying pivots, momentum and spacing", "General")
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
        self._pivot_high = self._pivot_low = None
        self._bar_count = 0
        self._last_signal = None
        self._pending_order = None
        self._filled_position = self._entry_price = Decimal.Zero

    def OnReseted(self):
        super(power_house_swift_edge_ai_v2_10_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(power_house_swift_edge_ai_v2_10_strategy, self).OnStarted2(time)
        if self._pivot_period.Value <= 0 or self._momentum_period.Value <= 0 or self._momentum_threshold.Value < 0 \
                or self._min_signal_bars.Value < 0 or self._stop_loss_points.Value <= 0 or self._take_profit_points.Value <= 0:
            raise ArgumentException("Pivot/momentum periods and point distances must be positive; thresholds and spacing cannot be negative.")
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
        comment = "SwiftEdge stop loss" if profit <= -self._stop_loss_points.Value \
            else "SwiftEdge take profit" if profit >= self._take_profit_points.Value else None
        if comment is None:
            return False
        self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), comment)
        return True

    def _observe_quote(self, quote):
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if quote.Changes.ContainsKey(field) and quote.Changes[field] > 0:
            self._try_risk_exit(quote.Changes[field])

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        period = self._pivot_period.Value
        momentum_period = self._momentum_period.Value
        width = 2 * period + 1
        self._bars.append((candle.HighPrice, candle.LowPrice, candle.ClosePrice))
        if len(self._bars) > max(width, momentum_period + 1):
            self._bars.pop(0)
        if len(self._bars) >= width:
            start = len(self._bars) - width
            center = start + period
            neighbors = [self._bars[index] for index in range(start, len(self._bars)) if index != center]
            if all(self._bars[center][0] > bar[0] for bar in neighbors):
                self._pivot_high = self._bars[center][0]
            if all(self._bars[center][1] < bar[1] for bar in neighbors):
                self._pivot_low = self._bars[center][1]
        if self._try_risk_exit(candle.ClosePrice) or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading() \
                or len(self._bars) <= momentum_period or len(self._bars) < 2 \
                or self._last_signal is not None and self._bar_count - self._last_signal < self._min_signal_bars.Value:
            return
        previous = self._bars[-2][2]
        momentum = candle.ClosePrice - self._bars[-momentum_period - 1][2]
        side = None
        if self._pivot_high is not None and previous <= self._pivot_high and candle.ClosePrice > self._pivot_high \
                and momentum > self._momentum_threshold.Value and self.Position <= 0:
            side = Sides.Buy
        elif self._pivot_low is not None and previous >= self._pivot_low and candle.ClosePrice < self._pivot_low \
                and momentum < -self._momentum_threshold.Value and self.Position >= 0:
            side = Sides.Sell
        if side is not None:
            self._last_signal = self._bar_count
            self._submit(side, self.Volume + abs(self.Position), "SwiftEdge pivot breakout")

    def CreateClone(self):
        return power_house_swift_edge_ai_v2_10_strategy()
