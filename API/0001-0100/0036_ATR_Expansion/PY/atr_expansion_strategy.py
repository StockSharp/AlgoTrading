import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_expansion_strategy(Strategy):
    """
    Strategy that trades on volatility expansion as measured by ATR.
    Enters on any one-bar increase in formed ATR in the price/SMA direction,
    exits when volatility contracts.
    """

    def __init__(self):
        super(atr_expansion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for MA calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Frozen entry ATR stop distance; zero disables it", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_atr = Decimal.Zero
        self._has_prev = False
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
        super(atr_expansion_strategy, self).OnReseted()
        self._prev_atr = Decimal.Zero
        self._has_prev = False
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(atr_expansion_strategy, self).OnStarted2(time)

        self._prev_atr = Decimal.Zero
        self._has_prev = False
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, sma, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, atr_value, sma_value):
        if candle.State != CandleStates.Finished or not atr_value.Indicator.IsFormed or not sma_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        atr = atr_value.GetValue[Decimal](None)
        mean = sma_value.GetValue[Decimal](None)
        if not self._has_prev:
            self._has_prev = True
            self._prev_atr = atr
            return
        previous = self._prev_atr
        self._prev_atr = atr
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        # Any contraction closes, including zero ATR; price/MA is only an entry filter.
        if self.Position != 0 and atr < previous:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and atr > previous:
            if candle.ClosePrice > mean:
                self._enter(Sides.Buy, atr)
            elif candle.ClosePrice < mean:
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
        order.Comment = "ATR expansion entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return atr_expansion_strategy()
