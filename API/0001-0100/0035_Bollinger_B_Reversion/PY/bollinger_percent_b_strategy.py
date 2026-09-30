import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import BollingerPercentB
from StockSharp.Algo.Strategies import Strategy

class bollinger_percent_b_strategy(Strategy):
    """
    Bollinger %B strategy.
    Buys when %B < 0 (below lower band), sells when %B > 1 (above upper band).
    """

    def __init__(self):
        super(bollinger_percent_b_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bb_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
        self._exit_value = self.Param("ExitValue", 0.5).SetRange(0.0, 1.0).SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._pending_order = None
        self._previous_percent_b = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_percent_b_strategy, self).OnReseted()
        self._pending_order = None
        self._previous_percent_b = None

    def OnStarted2(self, time):
        super(bollinger_percent_b_strategy, self).OnStarted2(time)

        self._pending_order = None
        self._previous_percent_b = None

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        bb = BollingerPercentB()
        bb.Length = self._bb_period.Value
        bb.StdDevMultiplier = Decimal(self._bb_deviation.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle, True).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished or not value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Native %B uses percent units and is empty for collapsed bands.
        # Use an explicitly neutral 0.5 for collapsed bands, not directional zero.
        pct = Decimal(0.5) if value.IsEmpty else value.GetValue[Decimal](None) / Decimal(100)
        previous = self._previous_percent_b
        self._previous_percent_b = pct
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        level = Decimal(self._exit_value.Value)
        upward_cross = previous is not None and previous < level and pct >= level
        downward_cross = previous is not None and previous > level and pct <= level
        if self.Position > 0 and upward_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and downward_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and pct < 0:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and pct > 1:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return bollinger_percent_b_strategy()
