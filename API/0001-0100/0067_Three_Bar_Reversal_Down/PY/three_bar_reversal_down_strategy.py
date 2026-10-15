import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class three_bar_reversal_down_strategy(Strategy):
    """
    Short-only three-bar reversal after an optional net uptrend.
    A pattern-high stop and an opposite three-bar reversal close the full position.
    """

    def __init__(self):
        super(three_bar_reversal_down_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Three-bar pattern timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetRange(0.0, 99.0).SetDisplay("Stop above pattern high (%)", "Buffer above the highest of all three pattern highs; zero places the stop at that high.", "Protection")
        self._require_uptrend = self.Param("RequireUptrend", True).SetDisplay("Require Uptrend", "Require a net Close rise before the signal candle.", "Pattern")
        self._uptrend_length = self.Param("UptrendLength", 5).SetRange(2, 100).SetDisplay("Uptrend Length", "Number of preceding candles from first Close to last Close, including the two bullish pattern bars.", "Pattern")
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
        super(three_bar_reversal_down_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(three_bar_reversal_down_strategy, self).OnStarted2(time)
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
        if (self.Position >= 0 or self._pattern_stop is None or
                executable_ask < self._pattern_stop or self._is_pending(self._exit_order)):
            return False
        self._exit_order = self.BuyMarket(Math.Abs(self.Position))
        self._recent = []
        self._pattern_stop = None
        return True

    def _append(self, candle):
        self._recent.append(candle)
        if len(self._recent) > self._uptrend_length.Value:
            self._recent.pop(0)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self.Position < 0 and self._check_stop(candle.HighPrice):
            return
        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order):
            self._append(candle)
            return
        previous = self._recent[-1] if len(self._recent) >= 1 else None
        older = self._recent[-2] if len(self._recent) >= 2 else None
        down_pattern = (older is not None and previous.ClosePrice > previous.OpenPrice and
                        older.ClosePrice > older.OpenPrice and previous.HighPrice > older.HighPrice and
                        candle.ClosePrice < candle.OpenPrice and candle.ClosePrice < previous.LowPrice)
        up_pattern = (older is not None and previous.ClosePrice < previous.OpenPrice and
                      older.ClosePrice < older.OpenPrice and previous.LowPrice < older.LowPrice and
                      candle.ClosePrice > candle.OpenPrice and candle.ClosePrice > previous.HighPrice)
        if self.Position < 0 and up_pattern and self.IsFormedAndOnlineAndAllowTrading():
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))
            self._recent = []
            self._pattern_stop = None
        elif (self.Position == 0 and down_pattern and
              (not self._require_uptrend.Value or
               len(self._recent) >= self._uptrend_length.Value and
               self._recent[-1].ClosePrice > self._recent[-self._uptrend_length.Value].ClosePrice) and
              self.IsFormedAndOnlineAndAllowTrading()):
            pattern_high = Math.Max(Math.Max(older.HighPrice, previous.HighPrice), candle.HighPrice)
            self._pattern_stop = pattern_high * (Decimal(1) + Decimal(self._stop_loss_percent.Value) / Decimal(100))
            self._entry_order = self.SellMarket(self.Volume)
        self._append(candle)

    def CreateClone(self):
        return three_bar_reversal_down_strategy()
