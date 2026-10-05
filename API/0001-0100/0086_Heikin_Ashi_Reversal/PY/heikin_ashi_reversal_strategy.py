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

class heikin_ashi_reversal_strategy(Strategy):
    """
    Heikin Ashi Reversal strategy.
    Computes Heikin-Ashi candles from regular candles. A bullish Heikin-Ashi candle after bearish ones turns the position long,
    a bearish one after bullish ones turns it short; a percent stop limits the loss.
    """

    def __init__(self):
        super(heikin_ashi_reversal_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ha_open = None
        self._ha_close = Decimal(0)
        self._prev_bullish = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heikin_ashi_reversal_strategy, self).OnReseted()
        self._ha_open = None
        self._ha_close = Decimal(0)
        self._prev_bullish = None

    def OnStarted2(self, time):
        super(heikin_ashi_reversal_strategy, self).OnStarted2(time)

        self._ha_open = None
        self._ha_close = Decimal(0)
        self._prev_bullish = None

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

        ha_close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(4)
        if self._ha_open is None:
            ha_open = (candle.OpenPrice + candle.ClosePrice) / Decimal(2)
        else:
            ha_open = (self._ha_open + self._ha_close) / Decimal(2)

        self._ha_open = ha_open
        self._ha_close = ha_close

        # A Heikin-Ashi candle without a body keeps the previous color.
        if ha_close == ha_open:
            return

        is_bullish = ha_close > ha_open
        was_bullish = self._prev_bullish
        self._prev_bullish = is_bullish

        if was_bullish is None or was_bullish == is_bullish or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if is_bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif not is_bullish and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return heikin_ashi_reversal_strategy()
