import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class morning_star_strategy(Strategy):
    """Long-only Morning Star with a middle-candle-low stop and confirmation-high target."""

    def __init__(self):
        super(morning_star_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Morning Star pattern timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetRange(0.0, 99.0).SetDisplay("Stop below middle low (%)", "Buffer below the middle candle's low; zero places the stop at that low", "Protection")
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _clear_state(self):
        self._recent = []
        self._stop_price = None
        self._target_price = None
        self._entry_order = None
        self._exit_order = None

    def OnReseted(self):
        super(morning_star_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(morning_star_strategy, self).OnStarted2(time)
        self._clear_state()
        bids = Subscription(DataType.Level1, self.Security)
        bids.MarketData.BuildField = Level1Fields.BestBidPrice
        self.SubscribeLevel1(bids).Bind(self._process_bid).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    @staticmethod
    def _is_pending(order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_bid(self, message):
        if message.Changes.ContainsKey(Level1Fields.BestBidPrice):
            bid = message.Changes[Level1Fields.BestBidPrice]
            if bid > 0:
                self._check_exit(bid, bid)

    def _check_exit(self, low, high):
        if self.Position <= 0 or self._is_pending(self._exit_order):
            return False
        # With only a completed bar's range, prefer the adverse stop when both levels touched.
        if ((self._stop_price is not None and low <= self._stop_price) or
                (self._target_price is not None and high > self._target_price)):
            self._exit_order = self.SellMarket(self.Position)
            self._recent = []
            self._stop_price = None
            self._target_price = None
            return True
        return False

    def _append(self, candle):
        self._recent.append(candle)
        if len(self._recent) > 2:
            self._recent.pop(0)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self.Position > 0 and self._check_exit(candle.LowPrice, candle.HighPrice):
            return
        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order):
            self._append(candle)
            return
        if self.Position == 0 and len(self._recent) == 2 and self.IsFormedAndOnlineAndAllowTrading():
            first, middle = self._recent
            first_body = first.OpenPrice - first.ClosePrice
            middle_body = Math.Abs(middle.ClosePrice - middle.OpenPrice)
            third_body = candle.ClosePrice - candle.OpenPrice
            first_midpoint = (first.HighPrice + first.LowPrice) / Decimal(2)
            if (first_body > 0 and middle_body < first_body / Decimal(2) and
                    third_body >= first_body / Decimal(2) and candle.ClosePrice > first_midpoint):
                self._stop_price = middle.LowPrice * (Decimal(1) - Decimal(self._stop_loss_percent.Value) / Decimal(100))
                self._target_price = candle.HighPrice
                self._entry_order = self.BuyMarket(self.Volume)
        self._append(candle)

    def CreateClone(self):
        return morning_star_strategy()
