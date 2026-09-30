import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields, Sides
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class doji_reversal_strategy(Strategy):
    """Reverse the prior two-close move on a doji; exit at its far extreme or percent protection."""

    def __init__(self):
        super(doji_reversal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Doji and prior-close timeframe", "General")
        self._doji_threshold = self.Param("DojiThreshold", 0.1).SetRange(0.0, 1.0).SetDisplay("Doji Threshold", "Strict upper bound for absolute body divided by high-low range", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection")
        self._clear_state()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order
        if ((self.Position > 0 and order.Side == Sides.Sell) or
                (self.Position < 0 and order.Side == Sides.Buy)):
            self._target_high = None
            self._target_low = None

    def _clear_state(self):
        self._older = None
        self._previous = None
        self._target_high = None
        self._target_low = None
        self._pending_order = None

    def OnReseted(self):
        super(doji_reversal_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(doji_reversal_strategy, self).OnStarted2(time)
        self._clear_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _pending(self):
        return self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_quote(self, message):
        if self._pending():
            return
        if self.Position > 0 and self._target_high is not None and message.Changes.ContainsKey(Level1Fields.BestBidPrice):
            bid = message.Changes[Level1Fields.BestBidPrice]
            if bid > self._target_high:
                self.SellMarket(self.Position)
        elif self.Position < 0 and self._target_low is not None and message.Changes.ContainsKey(Level1Fields.BestAskPrice):
            ask = message.Changes[Level1Fields.BestAskPrice]
            if ask > 0 and ask < self._target_low:
                self.BuyMarket(Math.Abs(self.Position))

    def _is_doji(self, candle):
        candle_range = candle.HighPrice - candle.LowPrice
        return (candle_range > 0 and
                Math.Abs(candle.ClosePrice - candle.OpenPrice) / candle_range < Decimal(self._doji_threshold.Value))

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._pending() and self.Position > 0 and self._target_high is not None and candle.HighPrice > self._target_high:
            self.SellMarket(self.Position)
        elif not self._pending() and self.Position < 0 and self._target_low is not None and candle.LowPrice < self._target_low:
            self.BuyMarket(Math.Abs(self.Position))
        elif (not self._pending() and self.Position == 0 and self._older is not None and
                self._previous is not None and self._is_doji(candle) and self.IsFormedAndOnlineAndAllowTrading()):
            if self._previous.ClosePrice < self._older.ClosePrice:
                self._target_high = candle.HighPrice
                self._target_low = None
                self.BuyMarket(self.Volume)
            elif self._previous.ClosePrice > self._older.ClosePrice:
                self._target_low = candle.LowPrice
                self._target_high = None
                self.SellMarket(self.Volume)
        self._older = self._previous
        self._previous = candle

    def CreateClone(self):
        return doji_reversal_strategy()
