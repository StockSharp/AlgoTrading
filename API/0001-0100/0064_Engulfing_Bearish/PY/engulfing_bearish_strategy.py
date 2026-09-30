import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class engulfing_bearish_strategy(Strategy):
    """
    Short-only bearish body engulfing after optional consecutive bullish candles.
    Protection sits above the higher of the two pattern highs.
    """

    def __init__(self):
        super(engulfing_bearish_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Engulfing timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetRange(0.0, 99.0).SetDisplay("Stop above pattern high (%)", "Buffer above the higher of the two pattern highs; zero places the stop at the high.", "Protection")
        self._require_uptrend = self.Param("RequireUptrend", True).SetDisplay("Require Uptrend", "Require consecutive bullish candles immediately before the engulfing candle.", "Pattern")
        self._uptrend_bars = self.Param("UptrendBars", 3).SetRange(1, 100).SetDisplay("Uptrend Bars", "Number of prior consecutive bullish candles, including the engulfed candle.", "Pattern")
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _clear_state(self):
        self._recent = []
        self._pattern_stop = None
        self._entry_order = None
        self._exit_order = None

    def OnReseted(self):
        super(engulfing_bearish_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(engulfing_bearish_strategy, self).OnStarted2(time)
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
        if (self.Position < 0 and self._pattern_stop is not None and
                executable_ask >= self._pattern_stop and not self._is_pending(self._exit_order)):
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))
            self._recent = []
            self._pattern_stop = None

    def _append(self, candle):
        self._recent.append(candle)
        if len(self._recent) > self._uptrend_bars.Value:
            self._recent.pop(0)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self.Position < 0:
            # Bar-high fallback covers gaps or missing executable quote updates.
            self._check_stop(candle.HighPrice)
            if self.Position < 0 and not self._is_pending(self._exit_order):
                self._append(candle)
            return
        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order):
            self._append(candle)
            return
        previous = self._recent[-1] if self._recent else None
        uptrend = (not self._require_uptrend.Value or
                   len(self._recent) >= self._uptrend_bars.Value and
                   all(bar.ClosePrice > bar.OpenPrice for bar in self._recent[-self._uptrend_bars.Value:]))
        engulfing = (previous is not None and previous.ClosePrice > previous.OpenPrice and
                     candle.ClosePrice < candle.OpenPrice and
                     candle.OpenPrice >= previous.ClosePrice and
                     candle.ClosePrice <= previous.OpenPrice)
        if engulfing and uptrend and self.IsFormedAndOnlineAndAllowTrading():
            pattern_high = Math.Max(previous.HighPrice, candle.HighPrice)
            self._pattern_stop = pattern_high * (Decimal(1) + Decimal(self._stop_loss_percent.Value) / Decimal(100))
            self._entry_order = self.SellMarket(self.Volume)
        self._append(candle)

    def CreateClone(self):
        return engulfing_bearish_strategy()
