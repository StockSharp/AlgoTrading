import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class outside_bar_reversal_strategy(Strategy):
    """
    Outside Bar Reversal strategy.
    An outside bar's high and low both exceed the previous candle's. While flat, a bullish outside bar after a bearish candle buys
    and a bearish one after a bullish candle sells. The position closes when a close breaks through the outside bar's opposite
    extreme, and a percent stop from the entry price, watched between candles, limits the loss.
    """

    def __init__(self):
        super(outside_bar_reversal_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_candle = None
        self._exit_level = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(outside_bar_reversal_strategy, self).OnReseted()
        self._prev_candle = None
        self._exit_level = Decimal(0)

    def OnStarted2(self, time):
        super(outside_bar_reversal_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._exit_level = Decimal(0)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        previous = self._prev_candle
        self._prev_candle = candle

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close < self._exit_level:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close > self._exit_level:
                self.BuyMarket(-self.Position)
            return

        is_outside_bar = candle.HighPrice > previous.HighPrice and candle.LowPrice < previous.LowPrice
        if not is_outside_bar:
            return

        after_decline = previous.ClosePrice < previous.OpenPrice
        after_rally = previous.ClosePrice > previous.OpenPrice

        if close > candle.OpenPrice and after_decline:
            self.BuyMarket(self.Volume)
            self._exit_level = candle.LowPrice
        elif close < candle.OpenPrice and after_rally:
            self.SellMarket(self.Volume)
            self._exit_level = candle.HighPrice

    def CreateClone(self):
        return outside_bar_reversal_strategy()
