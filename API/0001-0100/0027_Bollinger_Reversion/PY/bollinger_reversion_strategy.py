import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class bollinger_reversion_strategy(Strategy):
    """
    Bollinger Bands mean reversion strategy.
    Enters against outside closes, exits on return inside, protects fills with frozen ATR.
    """

    def __init__(self):
        super(bollinger_reversion_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetRange(5, 50).SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetRange(0.5, 4.0).SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR lookback for local protection.", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop.", "Protection")

        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_reversion_strategy, self).OnReseted()
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(bollinger_reversion_strategy, self).OnStarted2(time)

        bb = BollingerBands()
        bb.Length = self._bollinger_period.Value
        bb.Width = Decimal(self._bollinger_deviation.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, atr, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, bb_val, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed or not atr_value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return

        if bb_val.UpBand is None or bb_val.LowBand is None:
            return

        upper = bb_val.UpBand
        lower = bb_val.LowBand
        close = candle.ClosePrice
        if self.Position > 0 and close >= lower:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close <= upper:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and close < lower:
            self._enter(Sides.Buy, atr_value.GetValue[Decimal](None))
        elif self.Position == 0 and close > upper:
            self._enter(Sides.Sell, atr_value.GetValue[Decimal](None))

    def _enter(self, side, atr):
        distance = atr * Decimal(self._atr_multiplier.Value)
        if self._stop_distance is None:
            self._stop_distance = Unit(distance)
        # Flat entries update the same Unit retained by cached native position controllers.
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
        order.Comment = "Bollinger reversion entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return bollinger_reversion_strategy()
