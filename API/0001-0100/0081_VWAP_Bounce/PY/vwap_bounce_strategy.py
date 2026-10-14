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

class vwap_bounce_strategy(Strategy):
    """
    VWAP Bounce strategy.
    The VWAP restarts every UTC day from the typical price of each candle weighted by its volume. A bullish candle closing below
    the VWAP turns the position long and a bearish candle closing above it turns it short; a percent stop limits the loss.
    """

    def __init__(self):
        super(vwap_bounce_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._day = None
        self._price_volume = Decimal(0)
        self._volume = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_bounce_strategy, self).OnReseted()
        self._day = None
        self._price_volume = Decimal(0)
        self._volume = Decimal(0)

    def OnStarted2(self, time):
        super(vwap_bounce_strategy, self).OnStarted2(time)

        self._day = None
        self._price_volume = Decimal(0)
        self._volume = Decimal(0)

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

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._price_volume = Decimal(0)
            self._volume = Decimal(0)

        typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._price_volume += typical * candle.TotalVolume
        self._volume += candle.TotalVolume

        if self._volume <= 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        vwap = self._price_volume / self._volume
        close = candle.ClosePrice

        if close > candle.OpenPrice and close < vwap and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif close < candle.OpenPrice and close > vwap and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return vwap_bounce_strategy()
