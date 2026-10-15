import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AccumulationDistributionLine
from StockSharp.Algo.Strategies import Strategy

class ad_strategy(Strategy):
    """
    Trades strict native A/D changes confirmed by the Close/price-SMA direction.
    Fully exits on a strict adverse A/D step or actual-fill percent protection.
    """

    def __init__(self):
        super(ad_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._previous_ad = None
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_signal_state(self):
        self._previous_ad = None
        self._pending_order = None

    def OnReseted(self):
        super(ad_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(ad_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        ad = AccumulationDistributionLine()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, ad, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, ma_value, ad_value):
        if candle.State != CandleStates.Finished or not ad_value.Indicator.IsFormed or ad_value.IsEmpty:
            return
        ad = ad_value.GetValue[Decimal](None)
        previous_ad = self._previous_ad
        # Preserve real zero values and advance A/D during SMA warmup and pending orders.
        self._previous_ad = ad
        if not ma_value.Indicator.IsFormed or ma_value.IsEmpty or previous_ad is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        rising = ad > previous_ad
        falling = ad < previous_ad
        # Unchanged A/D is neutral, never a substitute for a strict decline.
        if self.Position > 0 and falling:
            self.SellMarket(self.Position)
        elif self.Position < 0 and rising:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0:
            mean = ma_value.GetValue[Decimal](None)
            if rising and candle.ClosePrice > mean:
                self.BuyMarket(self.Volume)
            elif falling and candle.ClosePrice < mean:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return ad_strategy()
