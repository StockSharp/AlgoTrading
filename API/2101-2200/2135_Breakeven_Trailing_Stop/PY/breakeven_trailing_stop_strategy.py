import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class breakeven_trailing_stop_strategy(Strategy):
    """One initial long; price-unit breakeven and trailing manager for either held side."""

    def __init__(self):
        super(breakeven_trailing_stop_strategy, self).__init__()
        self._breakeven_plus = self.Param("BreakevenPlus", 5.0).SetGreaterThanZero().SetDisplay(
            "Breakeven Plus", "Price profit required before breakeven", "Risk")
        self._breakeven_step = self.Param("BreakevenStep", 2.0).SetNotNegative().SetDisplay(
            "Breakeven Step", "Price offset beyond average fill", "Risk")
        self._trailing_plus = self.Param("TrailingPlus", 3.0).SetGreaterThanZero().SetDisplay(
            "Trailing Plus", "Distance beyond current stop before trailing", "Risk")
        self._trailing_step = self.Param("TrailingStep", 1.0).SetGreaterThanZero().SetDisplay(
            "Trailing Step", "Distance behind executable price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay(
            "Candle Type", "Timeframe of entry candles", "General")
        self._reset_state()

    @property
    def BreakevenPlus(self): return self._breakeven_plus.Value
    @property
    def BreakevenStep(self): return self._breakeven_step.Value
    @property
    def TrailingPlus(self): return self._trailing_plus.Value
    @property
    def TrailingStep(self): return self._trailing_step.Value
    @property
    def CandleType(self): return self._candle_type.Value

    def _reset_state(self):
        self._initial_entry_attempted = False
        self._breakeven_reached = False
        self._entry_price = None
        self._stop_price = None
        self._observed_volume = Decimal(0)
        self._pending_exit = None

    def OnReseted(self):
        super(breakeven_trailing_stop_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(breakeven_trailing_stop_strategy, self).OnStarted2(time)
        self._reset_state()
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            subscription = Subscription(DataType.Level1, self.Security)
            subscription.MarketData.BuildField = field
            self.SubscribeLevel1(subscription).Bind(self._process_quote).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or self._initial_entry_attempted:
            return
        self._initial_entry_attempted = True
        if self.Position == 0 and self.IsFormedAndOnlineAndAllowTrading():
            self.BuyMarket(self.Volume)

    def OnNewPosition(self, position):
        super(breakeven_trailing_stop_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(breakeven_trailing_stop_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(breakeven_trailing_stop_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        volume = Decimal(position.CurrentValue) if position.CurrentValue is not None else Decimal(0)
        if volume == 0:
            self._entry_price = self._stop_price = None
            self._observed_volume = Decimal(0)
            self._breakeven_reached = False
            self._pending_exit = None
            return
        average = position.AveragePrice
        if average is None or average <= 0:
            return
        average = Decimal(average)
        if average == self._entry_price and volume == self._observed_volume:
            return
        # An actual partial exit may change CurrentValue, not the average fill.
        if (average == self._entry_price and self._observed_volume * volume > 0
                and abs(volume) < abs(self._observed_volume)):
            self._observed_volume = volume
            return
        self._entry_price = average
        self._observed_volume = volume
        self._stop_price = None
        self._breakeven_reached = False

    def _process_quote(self, quote):
        if self.Position == 0 or self._entry_price is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_exit is not None and self._pending_exit.State not in (OrderStates.Done, OrderStates.Failed):
            return
        long_side = self.Position > 0
        side = Level1Fields.BestBidPrice if long_side else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(side):
            return
        price = Decimal(quote.Changes[side])
        if price <= 0:
            return
        entry = self._entry_price
        if not self._breakeven_reached:
            if (price >= entry + Decimal(self.BreakevenPlus)) if long_side else (price <= entry - Decimal(self.BreakevenPlus)):
                offset = Decimal(self.BreakevenStep)
                self._stop_price = entry + offset if long_side else entry - offset
                self._breakeven_reached = True
        elif self._stop_price is not None:
            previous = self._stop_price
            distance = Decimal(self.TrailingPlus)
            if (price >= previous + distance) if long_side else (price <= previous - distance):
                candidate = price - Decimal(self.TrailingStep) if long_side else price + Decimal(self.TrailingStep)
                self._stop_price = max(previous, candidate) if long_side else min(previous, candidate)
        if self._stop_price is None or not (price <= self._stop_price if long_side else price >= self._stop_price):
            return
        exit_order = Order()
        exit_order.Security = self.Security
        exit_order.Portfolio = self.Portfolio
        exit_order.Type = OrderTypes.Market
        exit_order.Side = Sides.Sell if long_side else Sides.Buy
        exit_order.Volume = abs(self.Position)
        exit_order.Comment = "Breakeven/trailing stop"
        self._pending_exit = exit_order
        self.RegisterOrder(exit_order)

    def CreateClone(self):
        return breakeven_trailing_stop_strategy()
