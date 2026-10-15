import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_range_strategy(Strategy):
    """
    Trades strict N-interval Close movement beyond Wilder ATR at every Nth bar.
    Exits on any ready bar's adverse SMA crossing or frozen entry-ATR protection.
    """

    def __init__(self):
        super(atr_range_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("ATRPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 5).SetGreaterThanZero().SetDisplay("Lookback Period", "N intervals for movement and N-bar entry-check cadence", "Entry")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Frozen entry ATR distance; zero disables only protection", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _reset_state(self):
        self._closes = []
        self._bar_count = 0
        self._previous_close = None
        self._previous_mean = Decimal.Zero
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnReseted(self):
        super(atr_range_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(atr_range_strategy, self).OnStarted2(time)
        self._reset_state()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, atr, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        self._closes.append(candle.ClosePrice)
        lookback = self._lookback_period.Value
        if len(self._closes) > lookback + 1:
            self._closes.pop(0)
        if not ma_value.Indicator.IsFormed or not atr_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = candle.ClosePrice
        mean = ma_value.GetValue[Decimal](None)
        downward_cross = self._previous_close is not None and self._previous_close >= self._previous_mean and close < mean
        upward_cross = self._previous_close is not None and self._previous_close <= self._previous_mean and close > mean
        self._previous_close = close
        self._previous_mean = mean
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        # Exits do not wait for the next N-bar entry checkpoint.
        if self.Position > 0 and downward_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and upward_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and self._bar_count % lookback == 0 and len(self._closes) == lookback + 1:
            movement = close - self._closes[0]
            atr = atr_value.GetValue[Decimal](None)
            if Math.Abs(movement) > atr:
                self._enter(Sides.Buy if movement > 0 else Sides.Sell, atr)

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
        order.Comment = "ATR range entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return atr_range_strategy()
