import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class bollinger_band_width_strategy(Strategy):
    """
    Trades strict absolute Bollinger width expansion in the price/middle direction,
    closes on strict contraction, and protects actual fills with frozen entry ATR.
    """

    def __init__(self):
        super(bollinger_band_width_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bb_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Wilder ATR lookback for protection", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Frozen entry ATR stop distance; zero disables it", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._previous_width = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(bollinger_band_width_strategy, self).OnReseted()
        self._previous_width = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(bollinger_band_width_strategy, self).OnStarted2(time)
        self._previous_width = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = Decimal(self._bb_deviation.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, atr, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, bb_value, atr_value):
        if candle.State != CandleStates.Finished or not bb_value.Indicator.IsFormed or not atr_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        width = bb_value.UpBand - bb_value.LowBand
        middle = bb_value.MovingAverage
        previous = self._previous_width
        self._previous_width = width
        # A formed zero-width bar is a valid baseline, not an uninitialized sentinel.
        if previous is None:
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        # Equal width is neutral; the middle is an entry-only filter.
        if self.Position != 0 and width < previous:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and width > previous:
            atr = atr_value.GetValue[Decimal](None)
            if candle.ClosePrice > middle:
                self._enter(Sides.Buy, atr)
            elif candle.ClosePrice < middle:
                self._enter(Sides.Sell, atr)

    def _enter(self, side, atr):
        distance = atr * Decimal(self._atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
        # Keep the same Unit held by native cached protection controllers.
        self._stop_distance.Value = distance
        if not self._protection_started and distance > 0:
            self.StartProtection(Unit(), self._stop_distance, useMarketOrders=True, isLocalStop=True)
            self._protection_started = True
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = self.Volume
        order.Comment = "Bollinger width entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return bollinger_band_width_strategy()
