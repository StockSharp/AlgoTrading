import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class momentum_percentage_strategy(Strategy):
    """
    Signed percentage momentum crossings with SMA confirmation and actual-fill percent protection.
    """

    def __init__(self):
        super(momentum_percentage_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 10).SetDisplay("Momentum Period", "Momentum period", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20).SetDisplay("SMA Period", "SMA trend filter period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._threshold_percent = self.Param("ThresholdPercent", 5.0).SetGreaterThanZero() \
            .SetDisplay("Threshold (%)", "Symmetric percentage return breakout level.", "Signal")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_mom = Decimal.Zero
        self._has_prev = False
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_percentage_strategy, self).OnReseted()
        self._prev_mom = Decimal.Zero
        self._has_prev = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(momentum_percentage_strategy, self).OnStarted2(time)
        mom = RateOfChange()
        mom.Length = self._momentum_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mom, sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mom)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, mom, sma):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if sma == 0:
            return
        if not self._has_prev:
            self._has_prev = True
            self._prev_mom = mom
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_mom = mom
            return
        price = candle.ClosePrice
        threshold = Decimal(self._threshold_percent.Value)
        if self._prev_mom <= threshold and mom > threshold and price > sma and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._prev_mom >= -threshold and mom < -threshold and price < sma and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        self._prev_mom = mom

    def CreateClone(self):
        return momentum_percentage_strategy()
