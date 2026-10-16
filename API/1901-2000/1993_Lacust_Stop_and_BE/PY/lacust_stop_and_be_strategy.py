import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderTypes, Sides, OrderStates
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class lacust_stop_and_be_strategy(Strategy):
    """Filled-price SL/TP, breakeven and monotonic trailing on executable bid/ask."""

    def __init__(self):
        super(lacust_stop_and_be_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay(
            "Candle Type", "Signal candles", "General")
        self._stop_loss = self.Param("StopLoss", 400.0).SetNotNegative().SetDisplay(
            "Stop Loss", "Initial distance in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0).SetNotNegative().SetDisplay(
            "Take Profit", "Target distance in price units", "Risk")
        self._trailing_start = self.Param("TrailingStart", 300.0).SetNotNegative().SetDisplay(
            "Trailing Start", "Profit at which trailing starts", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 200.0).SetGreaterThanZero().SetDisplay(
            "Trailing Stop", "Trailing distance from favorable quote", "Risk")
        self._breakeven_gain = self.Param("BreakevenGain", 250.0).SetNotNegative().SetDisplay(
            "Breakeven Gain", "Profit at which breakeven activates", "Risk")
        self._breakeven = self.Param("Breakeven", 100.0).SetNotNegative().SetDisplay(
            "Breakeven", "Profit locked after breakeven", "Risk")
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._extreme = None
        self._tracked_volume = Decimal(0)
        self._pending_order = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _clear_tracking(self):
        self._entry_price = self._stop_price = self._take_price = self._extreme = None
        self._tracked_volume = Decimal(0)

    def OnReseted(self):
        super(lacust_stop_and_be_strategy, self).OnReseted()
        self._clear_tracking()
        self._pending_order = None

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(lacust_stop_and_be_strategy, self).OnStarted2(time)
        self._clear_tracking()
        self._pending_order = None
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            subscription = Subscription(DataType.Level1, self.Security)
            subscription.MarketData.BuildField = field
            self.SubscribeLevel1(subscription).Bind(self.ProcessQuote).Start()

    def OnNewPosition(self, position):
        super(lacust_stop_and_be_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(lacust_stop_and_be_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(lacust_stop_and_be_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        volume = position.CurrentValue if position.CurrentValue is not None else Decimal(0)
        if volume == 0:
            self._clear_tracking()
            return
        average = position.AveragePrice
        if average is None or average <= 0:
            return
        if self._entry_price == average and self._tracked_volume == volume:
            return
        if (self._entry_price == average and self._tracked_volume * volume > 0
                and abs(volume) < abs(self._tracked_volume)):
            self._tracked_volume = volume
            return
        self._entry_price = average
        self._tracked_volume = volume
        self._extreme = average
        stop = Decimal(self._stop_loss.Value)
        take = Decimal(self._take_profit.Value)
        self._stop_price = (average - stop if volume > 0 else average + stop) if stop > 0 else None
        self._take_price = (average + take if volume > 0 else average - take) if take > 0 else None

    def _pending(self):
        return self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished or self.Position != 0:
            return
        if not self.IsFormedAndOnlineAndAllowTrading() or self._pending():
            return
        if candle.ClosePrice > candle.OpenPrice:
            self._pending_order = self.BuyMarket(self.Volume)
        elif candle.ClosePrice < candle.OpenPrice:
            self._pending_order = self.SellMarket(self.Volume)

    def _tighten_stop(self, is_long, candidate):
        if self._stop_price is None:
            self._stop_price = candidate
        elif is_long:
            self._stop_price = max(self._stop_price, candidate)
        else:
            self._stop_price = min(self._stop_price, candidate)

    def ProcessQuote(self, quote):
        if self.Position == 0 or self._entry_price is None:
            return
        if not self.IsFormedAndOnlineAndAllowTrading() or self._pending():
            return
        is_long = self.Position > 0
        field = Level1Fields.BestBidPrice if is_long else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Decimal(quote.Changes[field])
        if price <= 0:
            return
        entry = self._entry_price
        best = self._extreme if self._extreme is not None else entry
        best = max(best, price) if is_long else min(best, price)
        self._extreme = best
        gain = (best - entry) if is_long else (entry - best)
        if gain >= Decimal(self._breakeven_gain.Value):
            candidate = entry + Decimal(self._breakeven.Value) if is_long else entry - Decimal(self._breakeven.Value)
            self._tighten_stop(is_long, candidate)
        if gain >= Decimal(self._trailing_start.Value):
            trailing = Decimal(self._trailing_stop.Value)
            self._tighten_stop(is_long, best - trailing if is_long else best + trailing)
        hit_stop = self._stop_price is not None and (price <= self._stop_price if is_long else price >= self._stop_price)
        hit_take = self._take_price is not None and (price >= self._take_price if is_long else price <= self._take_price)
        if not (hit_stop or hit_take):
            return
        exit_order = Order()
        exit_order.Security = self.Security
        exit_order.Portfolio = self.Portfolio
        exit_order.Type = OrderTypes.Market
        exit_order.Side = Sides.Sell if is_long else Sides.Buy
        exit_order.Volume = abs(self.Position)
        exit_order.Comment = "Lacust stop / breakeven / trailing" if hit_stop else "Lacust take profit"
        self._pending_order = exit_order
        self.RegisterOrder(exit_order)

    def CreateClone(self):
        return lacust_stop_and_be_strategy()
