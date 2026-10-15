import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import VolumeWeightedAveragePrice, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy, StrategyHelper

class vwap_breakout_strategy(Strategy):
    """
    Trades actual same-UTC-session Close crossings of cumulative candle VWAP.
    The opposite crossing reverses the position; actual-fill percent protection flattens it.
    """

    def __init__(self):
        super(vwap_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._vwap = None
        self._session_date = None
        self._previous_close = None
        self._previous_vwap = Decimal.Zero
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
        self._session_date = None
        self._previous_close = None
        self._previous_vwap = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(vwap_breakout_strategy, self).OnReseted()
        self._clear_signal_state()
        self._vwap = None

    def OnStarted2(self, time):
        super(vwap_breakout_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self._vwap = VolumeWeightedAveragePrice()
        self.Indicators.Add(self._vwap)
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        date = candle.OpenTime.ToUniversalTime().Date
        if self._session_date != date:
            self._session_date = date
            # Reset BEFORE this day's first final bar; a reset jump is not a price crossing.
            self._vwap.Reset()
            self._previous_close = None
        value = self._vwap.Process(CandleIndicatorValue(self._vwap, candle))
        if value.IsEmpty or not self._vwap.IsFormed:
            return
        vwap = value.GetValue[Decimal](None)
        upward_cross = self._previous_close is not None and self._previous_close <= self._previous_vwap and candle.ClosePrice > vwap
        downward_cross = self._previous_close is not None and self._previous_close >= self._previous_vwap and candle.ClosePrice < vwap
        # Seed the first valid session value and advance history during pending orders.
        self._previous_close = candle.ClosePrice
        self._previous_vwap = vwap
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if upward_cross and self.Position <= 0:
            self.BuyMarket(StrategyHelper.ReversalVolume(self))
        elif downward_cross and self.Position >= 0:
            self.SellMarket(StrategyHelper.ReversalVolume(self))

    def CreateClone(self):
        return vwap_breakout_strategy()
