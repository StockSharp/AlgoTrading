import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy, StrategyHelper

class vwma_strategy(Strategy):
    """
    Trades actual Close crossings of native rolling Close/volume VWMA.
    The opposite crossing reverses the position; actual-fill percent protection flattens it.
    """

    def __init__(self):
        super(vwma_strategy, self).__init__()
        self._vwma_period = self.Param("VWMAPeriod", 14).SetGreaterThanZero().SetDisplay("VWMA Period", "Current-inclusive rolling Close/volume mean length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._previous_close = None
        self._previous_vwma = Decimal.Zero
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
        self._previous_close = None
        self._previous_vwma = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(vwma_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(vwma_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self._vwma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(vwma, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if value.IsEmpty or not value.Indicator.IsFormed:
            # An undefined zero-volume window cannot connect crossing history across it.
            self._previous_close = None
            return
        vwma = value.GetValue[Decimal](None)
        upward_cross = self._previous_close is not None and self._previous_close <= self._previous_vwma and candle.ClosePrice > vwma
        downward_cross = self._previous_close is not None and self._previous_close >= self._previous_vwma and candle.ClosePrice < vwma
        # The first formed value seeds; history continues during pending orders.
        self._previous_close = candle.ClosePrice
        self._previous_vwma = vwma
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if upward_cross and self.Position <= 0:
            self.BuyMarket(StrategyHelper.ReversalVolume(self))
        elif downward_cross and self.Position >= 0:
            self.SellMarket(StrategyHelper.ReversalVolume(self))

    def CreateClone(self):
        return vwma_strategy()
