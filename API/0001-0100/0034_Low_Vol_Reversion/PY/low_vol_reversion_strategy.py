import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, OrderStates, OrderTypes, Sides, Level1Fields
from StockSharp.BusinessEntities import Subscription, Order
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class low_vol_reversion_strategy(Strategy):
    """
    Low volatility mean reversion strategy.
    Trades when ATR is below average, expecting price to revert to MA.
    """

    def __init__(self):
        super(low_vol_reversion_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_lookback = self.Param("AtrLookbackPeriod", 20).SetGreaterThanZero().SetDisplay("ATR Lookback", "Lookback period for ATR average calculation", "Indicators")
        self._atr_threshold = self.Param("AtrThresholdPercent", 75.0).SetNotNegative().SetDisplay("ATR Threshold %", "ATR threshold as percentage of average ATR", "Entry")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Stop Multiplier", "Frozen entry ATR stop distance; zero disables it", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_average = None
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
        super(low_vol_reversion_strategy, self).OnReseted()
        self._atr_average = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

    def OnStarted2(self, time):
        super(low_vol_reversion_strategy, self).OnStarted2(time)

        self._atr_average = None
        self._pending_order = None
        self._stop_distance = None
        self._protection_started = False

        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = self._atr_lookback.Value
        self._atr_average.Name = "ATR rolling mean"
        self.Indicators.Add(self._atr_average)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished or not atr_value.Indicator.IsFormed:
            return
        atr = atr_value.GetValue[Decimal](None)
        # Feed only fully formed ATR values, including the current sample in the window.
        indicator_input = DecimalIndicatorValue(self._atr_average, atr, candle.OpenTime)
        indicator_input.IsFinal = True
        average = self._atr_average.Process(indicator_input).GetValue[Decimal](None)
        if not self._atr_average.IsFormed or not sma_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        mean = sma_value.GetValue[Decimal](None)
        # The quiet-market entry filter never blocks a held position's mean-touch exit.
        if self.Position > 0 and candle.ClosePrice >= mean:
            self.SellMarket(self.Position)
        elif self.Position < 0 and candle.ClosePrice <= mean:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and atr > 0 and atr < average * Decimal(self._atr_threshold.Value) / Decimal(100):
            if candle.ClosePrice < mean:
                self._enter(Sides.Buy, atr)
            elif candle.ClosePrice > mean:
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
        order.Comment = "Low volatility entry"
        self.RegisterOrder(order)

    def CreateClone(self):
        return low_vol_reversion_strategy()
