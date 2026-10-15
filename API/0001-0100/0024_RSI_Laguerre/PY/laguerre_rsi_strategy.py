import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import LaguerreRSI
from StockSharp.Algo.Strategies import Strategy

class laguerre_rsi_strategy(Strategy):
    """
    Native four-stage Laguerre RSI with midpoint exits and actual-fill percent protection.
    """

    def __init__(self):
        super(laguerre_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._gamma = self.Param("Gamma", 0.7).SetRange(0.000001, 0.999999) \
            .SetDisplay("Gamma", "Four-stage Laguerre smoothing coefficient.", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_rsi = Decimal.Zero
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
        super(laguerre_rsi_strategy, self).OnReseted()
        self._prev_rsi = Decimal.Zero
        self._has_prev = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(laguerre_rsi_strategy, self).OnStarted2(time)
        rsi = LaguerreRSI()
        rsi.Gamma = Decimal(self._gamma.Value)
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_val
        if not self._has_prev:
            self._has_prev = True
            self._prev_rsi = rsi
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_rsi = rsi
            return
        if self._prev_rsi < 30 and rsi >= 30 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._prev_rsi > 70 and rsi <= 70 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and rsi >= 50:
            self.SellMarket(self.Position)
        elif self.Position < 0 and rsi <= 50:
            self.BuyMarket(Math.Abs(self.Position))
        self._prev_rsi = rsi

    def CreateClone(self):
        return laguerre_rsi_strategy()
