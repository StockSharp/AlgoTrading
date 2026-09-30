import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class williams_percent_r_strategy(Strategy):
    """
    Strategy based on Williams %R indicator.
    Buys when Williams %R drops below the oversold level (-80),
    sells when it rises above the overbought level (-20).
    Closes at the neutral midpoint (-50) and protects actual fills with a native percent stop.
    """

    def __init__(self):
        super(williams_percent_r_strategy, self).__init__()
        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "Period for Williams %R calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_wr = Decimal.Zero
        self._has_prev_values = False
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
        super(williams_percent_r_strategy, self).OnReseted()
        self._prev_wr = Decimal.Zero
        self._has_prev_values = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(williams_percent_r_strategy, self).OnStarted2(time)

        williams = WilliamsR()
        williams.Length = self._period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(williams, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, wr):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_wr = wr
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_wr = wr
            return

        if self._prev_wr >= -80 and wr < -80 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._prev_wr <= -20 and wr > -20 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and wr >= -50:
            self.SellMarket(self.Position)
        elif self.Position < 0 and wr <= -50:
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_wr = wr

    def CreateClone(self):
        return williams_percent_r_strategy()
