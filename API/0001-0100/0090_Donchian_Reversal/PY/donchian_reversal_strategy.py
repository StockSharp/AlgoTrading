import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class donchian_reversal_strategy(Strategy):
    """
    Donchian Reversal strategy.
    A breakout is a close beyond the channel of the Period candles before it. When the next close returns above the lower band
    that was broken the position turns long, and when it returns below a broken upper band it turns short. A percent stop limits the loss.
    """

    def __init__(self):
        super(donchian_reversal_strategy, self).__init__()
        self._period = self.Param("Period", 20).SetGreaterThanZero().SetDisplay("Period", "Donchian Channel period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candles = []
        # The previous close and the channel it was compared with.
        self._previous = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_reversal_strategy, self).OnReseted()
        self._candles = []
        self._previous = None

    def OnStarted2(self, time):
        super(donchian_reversal_strategy, self).OnStarted2(time)

        self._candles = []
        self._previous = None

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

        close = candle.ClosePrice
        previous = self._previous
        period = self._period.Value

        # The channel of the candles before this one, for the next candle to compare with.
        if len(self._candles) == period:
            self._previous = (close, max(c[0] for c in self._candles), min(c[1] for c in self._candles))
        else:
            self._previous = None

        self._candles.append((candle.HighPrice, candle.LowPrice))
        if len(self._candles) > period:
            self._candles.pop(0)

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        prior_close, prior_upper, prior_lower = previous

        if prior_close < prior_lower and close > prior_lower and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif prior_close > prior_upper and close < prior_upper and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return donchian_reversal_strategy()
