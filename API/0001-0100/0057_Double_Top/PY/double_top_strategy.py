import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class double_top_strategy(Strategy):
    """
    Short-only double top: two confirmed pivot highs followed by a bearish candle.
    A pattern-high stop watches executable best asks and finished-bar highs.
    """

    def __init__(self):
        super(double_top_strategy, self).__init__()
        self._distance = self.Param("Distance", 5).SetRange(3, 100).SetDisplay("Distance", "Minimum bars between confirmed pivot highs", "Pattern")
        self._similarity_percent = self.Param("SimilarityPercent", 2.0).SetRange(0.1, 5.0).SetDisplay("Similarity %", "Maximum relative difference between pivot highs", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetRange(0.0, 99.0).SetDisplay("Stop above highs (%)", "Percentage buffer above the higher pattern high; zero places it at the high.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe for pivot highs and bearish confirmation", "General")
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _clear_state(self):
        self._two_back = None
        self._previous = None
        self._last_pivot = None
        self._candidate_high = None
        self._candidate_expires = 0
        self._pattern_stop = None
        self._bar = 0
        self._entry_order = None
        self._exit_order = None

    def OnReseted(self):
        super(double_top_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(double_top_strategy, self).OnStarted2(time)
        self._clear_state()
        asks = Subscription(DataType.Level1, self.Security)
        asks.MarketData.BuildField = Level1Fields.BestAskPrice
        self.SubscribeLevel1(asks).Bind(self._process_ask).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _is_pending(self, order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_ask(self, message):
        if message.Changes.ContainsKey(Level1Fields.BestAskPrice):
            ask = message.Changes[Level1Fields.BestAskPrice]
            if ask is not None and ask > Decimal(0):
                self._check_stop(ask)

    def _check_stop(self, executable_ask):
        if self.Position < 0 and self._pattern_stop is not None and executable_ask >= self._pattern_stop and not self._is_pending(self._exit_order):
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))
            # Never reuse an exited pattern for another entry.
            self._two_back = None
            self._previous = None
            self._last_pivot = None
            self._candidate_high = None

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar += 1
        older = self._two_back
        middle = self._previous
        self._two_back = middle
        self._previous = candle
        if self.Position < 0:
            # A bar-low fallback handles absent/stale quotes or gaps; fills can slip.
            self._check_stop(candle.HighPrice)
            return
        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order):
            return
        if older is not None and middle is not None and middle.HighPrice > older.HighPrice and middle.HighPrice >= candle.HighPrice:
            pivot = (self._bar - 1, middle.HighPrice)
            first = self._last_pivot
            if first is not None and pivot[0] - first[0] >= self._distance.Value and Math.Abs(pivot[1] - first[1]) * Decimal(100) <= first[1] * Decimal(self._similarity_percent.Value):
                self._candidate_high = max(first[1], pivot[1])
                self._candidate_expires = self._bar + self._distance.Value
            self._last_pivot = pivot
        high = self._candidate_high
        if high is None:
            return
        if self._bar > self._candidate_expires or candle.HighPrice > high:
            self._candidate_high = None
            return
        if candle.ClosePrice >= candle.OpenPrice or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._pattern_stop = high * (Decimal(1) + Decimal(self._stop_loss_percent.Value) / Decimal(100))
        self._entry_order = self.SellMarket(self.Volume)
        self._candidate_high = None
        self._last_pivot = None

    def CreateClone(self):
        return double_top_strategy()
