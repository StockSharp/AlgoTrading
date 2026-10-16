import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_activation_strategy(Strategy):
    """Exit-only manager: use reported position average and fresh executable quotes."""

    def __init__(self):
        super(trailing_stop_activation_strategy, self).__init__()
        self._trailing_stop = self.Param("TrailingStop", 500.0).SetGreaterThanZero().SetDisplay(
            "Trailing Stop", "Profit required to activate, and trailing price distance", "Risk")
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative().SetDisplay(
            "Initial Stop Loss", "Distance from filled entry; zero disables it", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay(
            "Candle Type", "Optional chart candle timeframe", "General")
        self._entry_price = None
        self._extreme_price = None
        self._stop_price = None
        self._tracked_volume = Decimal(0)
        self._pending_exit = None

    @property
    def TrailingStop(self):
        return self._trailing_stop.Value

    @TrailingStop.setter
    def TrailingStop(self, value):
        self._trailing_stop.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1), (self.Security, self.CandleType)]

    def OnReseted(self):
        super(trailing_stop_activation_strategy, self).OnReseted()
        self._entry_price = self._extreme_price = self._stop_price = None
        self._tracked_volume = Decimal(0)
        self._pending_exit = None

    def OnStarted2(self, time):
        super(trailing_stop_activation_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            subscription = Subscription(DataType.Level1, self.Security)
            subscription.MarketData.BuildField = field
            self.SubscribeLevel1(subscription).Bind(self._process_quote).Start()
        candles = self.SubscribeCandles(self.CandleType)
        candles.Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def OnNewPosition(self, position):
        super(trailing_stop_activation_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(trailing_stop_activation_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(trailing_stop_activation_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        volume = position.CurrentValue if position.CurrentValue is not None else Decimal(0)
        if volume == 0:
            self._entry_price = self._extreme_price = self._stop_price = None
            self._tracked_volume = Decimal(0)
            self._pending_exit = None
            return
        average = position.AveragePrice
        if average is None or average <= 0:
            return
        if self._entry_price == average and self._tracked_volume == volume:
            return
        # Preserve an achieved stop on a partial same-side reduction.
        if (self._entry_price == average and self._tracked_volume * volume > 0
                and abs(volume) < abs(self._tracked_volume)):
            self._tracked_volume = volume
            return
        self._entry_price = average
        self._tracked_volume = volume
        self._extreme_price = average
        distance = Decimal(self.StopLoss)
        self._stop_price = ((average - distance) if volume > 0 else (average + distance)) if distance > 0 else None

    def _process_quote(self, quote):
        if self.Position == 0 or self._entry_price is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_exit is not None and self._pending_exit.State not in (OrderStates.Done, OrderStates.Failed):
            return
        is_long = self.Position > 0
        field = Level1Fields.BestBidPrice if is_long else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Decimal(quote.Changes[field])
        if price <= 0:
            return
        entry = self._entry_price
        extreme = self._extreme_price if self._extreme_price is not None else entry
        extreme = max(extreme, price) if is_long else min(extreme, price)
        self._extreme_price = extreme
        distance = Decimal(self.TrailingStop)
        if (extreme - entry > distance) if is_long else (entry - extreme > distance):
            candidate = extreme - distance if is_long else extreme + distance
            if self._stop_price is None:
                self._stop_price = candidate
            else:
                self._stop_price = max(self._stop_price, candidate) if is_long else min(self._stop_price, candidate)

        if self._stop_price is None or not (price <= self._stop_price if is_long else price >= self._stop_price):
            return
        exit_order = Order()
        exit_order.Security = self.Security
        exit_order.Portfolio = self.Portfolio
        exit_order.Type = OrderTypes.Market
        exit_order.Side = Sides.Sell if is_long else Sides.Buy
        exit_order.Volume = abs(self.Position)
        exit_order.Comment = "Trailing/initial stop activation"
        self._pending_exit = exit_order
        self.RegisterOrder(exit_order)

    def CreateClone(self):
        return trailing_stop_activation_strategy()
