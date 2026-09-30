import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, Level1Fields, OrderStates, OrderTypes, Sides
from StockSharp.Algo.Indicators import RateOfChange, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription, Order

class roc_impulse_strategy(Strategy):
    """
    Signed percentage ROC breakouts, zero exits and native actual-fill ATR protection.
    """

    def __init__(self):
        super(roc_impulse_strategy, self).__init__()
        self._roc_period = self.Param("RocPeriod", 12) \
            .SetDisplay("ROC Period", "Lookback for percentage rate of change.", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR lookback.", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Entry ATR stop distance; zero disables it.", "Protection")
        self._threshold_percent = self.Param("ThresholdPercent", 0.5).SetGreaterThanZero().SetDisplay("Threshold (%)", "Symmetric percentage ROC breakout level.", "Signal")

        self._reset_state()
        self.OrderRegistering += self._track_pending
        self.Trades.TradeAdded += self._process_entry_fill

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(roc_impulse_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._prev_roc = Decimal.Zero
        self._has_prev_values = False
        self._stop_distance = None
        self._protection_started = False
        self._requested_distance = Decimal.Zero
        self._entry_order = None
        self._pending_order = None

    def OnStarted2(self, time):
        super(roc_impulse_strategy, self).OnStarted2(time)

        roc = RateOfChange()
        roc.Length = self._roc_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, roc)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, roc, atr):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_roc = roc
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_roc = roc
            return

        threshold = Decimal(self._threshold_percent.Value)
        if self._prev_roc <= threshold and roc > threshold and self.Position <= 0:
            self._enter(Sides.Buy, atr)
        elif self._prev_roc >= -threshold and roc < -threshold and self.Position >= 0:
            self._enter(Sides.Sell, atr)
        elif self.Position > 0 and roc <= 0:
            self.SellMarket(self.Position)
        elif self.Position < 0 and roc >= 0:
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_roc = roc

    def _enter(self, side, atr):
        self._requested_distance = atr * Decimal(self._atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(self._requested_distance)
        if self.Position == 0:
            self._stop_distance.Value = self._requested_distance
        if not self._protection_started and self._requested_distance > 0:
            self.StartProtection(Unit(), self._stop_distance, useMarketOrders=True, isLocalStop=True)
            self._protection_started = True
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = self.Volume + Math.Abs(self.Position)
        order.Comment = "ROC signal"
        self._entry_order = order
        self.RegisterOrder(order)

    def _process_entry_fill(self, trade):
        # Keep the old distance until an actual reversal fill changes the position's direction.
        if (trade.Order == self._entry_order and self.Position != 0
                and (self.Position > 0) == (trade.Order.Side == Sides.Buy)):
            self._stop_distance.Value = self._requested_distance

    def CreateClone(self):
        return roc_impulse_strategy()
