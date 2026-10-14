import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, ArgumentException, Decimal, TimeSpan
from StockSharp.Messages import CandleStates, DataType, ITickTradeMessage, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex, IIndicator
from StockSharp.Algo.Strategies import Strategy


class previous_day_high_low_long_strategy(Strategy):
    def __init__(self):
        super(previous_day_high_low_long_strategy, self).__init__()
        self._max_profit = self.Param("MaxProfit", Decimal(150)).SetGreaterThanZero() \
            .SetDisplay("Maximum Profit", "Target distance above actual entry price, in absolute price units", "Protection")
        self._max_stop_loss = self.Param("MaxStopLoss", Decimal(15)).SetGreaterThanZero() \
            .SetDisplay("Maximum Stop Loss", "Trailing distance below highest observed price, in absolute price units", "Protection")
        self._adx_length = self.Param("AdxLength", 11).SetGreaterThanZero() \
            .SetDisplay("ADX Length", "Native directional-strength period; current ADX must rise", "Indicators")
        self._session_start = self.Param("SessionStart", TimeSpan.FromMinutes(570)) \
            .SetDisplay("Session Start", "Inclusive UTC entry time", "Session")
        self._session_end = self.Param("SessionEnd", TimeSpan.FromHours(16)) \
            .SetDisplay("Session End", "Exclusive UTC entry time and mandatory close", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying UTC day levels and ADX", "General")
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
        self._day = self._previous_close = self._previous_adx = self._entry_date = None
        self._day_high = self._day_low = self._previous_day_high = self._previous_day_low = Decimal.Zero
        self._is_day_start_observed = self._is_previous_day_available = False
        self._filled_position = self._entry_price = self._extreme = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(previous_day_high_low_long_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(previous_day_high_low_long_strategy, self).OnStarted2(time)
        if self._max_profit.Value <= 0 or self._max_stop_loss.Value <= 0 or self._adx_length.Value <= 0 \
                or self._session_start.Value < TimeSpan.Zero or self._session_start.Value >= self._session_end.Value \
                or self._session_end.Value > TimeSpan.FromDays(1):
            raise ArgumentException("Risk distances and ADX period must be positive; session times must be ordered within a UTC day.")
        self._reset_state()
        quotes = Subscription(DataType.Level1, self.Security)
        quotes.MarketData.BuildField = Level1Fields.BestBidPrice
        self.SubscribeLevel1(quotes).Bind(self._observe_quote).Start()
        self.StartTimer(TimeSpan.FromMinutes(1), self._try_session_exit)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([adx]), self.OnProcess, True).Start()
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

    def _try_session_exit(self):
        date = self._entry_date
        is_over = date is not None and (self.CurrentTime.Date > date or self.CurrentTime.TimeOfDay >= self._session_end.Value)
        if not is_over or self.Position <= 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return False
        self._submit(Sides.Sell, self.Position, "Previous-day session close")
        return True

    def _observe_fill(self, subscription, trade):
        previous = self._filled_position
        price = self._tick_price.GetValue(trade.Trade)
        volume = self._tick_volume.GetValue(trade.Trade)
        self._filled_position += volume if trade.Order.Side == Sides.Buy else -volume
        if trade.Order.Side == Sides.Buy and self._filled_position > 0:
            self._entry_price = (self._entry_price * previous + price * volume) / self._filled_position
            self._extreme = price if previous == 0 else max(self._extreme, price)
        if self._filled_position == 0:
            self._entry_price = self._extreme = Decimal.Zero

    def _observe_price(self, price):
        if self._try_session_exit() or self.Position <= 0 or self._entry_price <= 0 \
                or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._extreme = max(self._extreme, price)
        comment = "Previous-day trailing stop" if price <= self._extreme - self._max_stop_loss.Value \
            else "Previous-day profit target" if price >= self._entry_price + self._max_profit.Value else None
        if comment is not None:
            self._submit(Sides.Sell, self.Position, comment)

    def _observe_quote(self, quote):
        if quote.Changes.ContainsKey(Level1Fields.BestBidPrice) and quote.Changes[Level1Fields.BestBidPrice] > 0:
            self._observe_price(quote.Changes[Level1Fields.BestBidPrice])

    def _observe_day(self, candle):
        date = candle.OpenTime.Date
        if self._day != date:
            self._is_previous_day_available = self._is_day_start_observed and self._day == date.AddDays(-1)
            self._previous_day_high = self._day_high
            self._previous_day_low = self._day_low
            self._day = date
            self._day_high = candle.HighPrice
            self._day_low = candle.LowPrice
            self._is_day_start_observed = candle.OpenTime.TimeOfDay == TimeSpan.Zero
        else:
            self._day_high = max(self._day_high, candle.HighPrice)
            self._day_low = min(self._day_low, candle.LowPrice)

    def OnProcess(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        previous_close = self._previous_close
        self._previous_close = candle.ClosePrice
        self._observe_day(candle)
        previous_adx = self._previous_adx
        current_adx = values[0].MovingAverage if values[0].IsFormed else None
        if current_adx is not None:
            self._previous_adx = current_adx
        if self.Position > 0:
            self._observe_price(candle.ClosePrice)
            return
        if self.Position != 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading() \
                or not self._is_previous_day_available or previous_close is None or previous_adx is None \
                or current_adx is None or current_adx <= previous_adx \
                or self.CurrentTime.TimeOfDay < self._session_start.Value or self.CurrentTime.TimeOfDay >= self._session_end.Value:
            return
        high_cross = previous_close <= self._previous_day_high and candle.ClosePrice > self._previous_day_high
        low_cross = previous_close <= self._previous_day_low and candle.ClosePrice > self._previous_day_low
        if not high_cross and not low_cross:
            return
        self._entry_date = self.CurrentTime.Date
        self._submit(Sides.Buy, self.Volume, "Previous-day ADX breakout")

    def CreateClone(self):
        return previous_day_high_low_long_strategy()
