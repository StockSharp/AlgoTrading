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

class gap_fill_reversal_strategy(Strategy):
    """
    Gap Fill Reversal strategy.
    A gap is an open at least MinGapPercent away from the previous close. When the same candle trades back to the previous close,
    filling the gap, the position turns against the gap: short after a gap up, long after a gap down. A percent stop limits the loss.
    """

    def __init__(self):
        super(gap_fill_reversal_strategy, self).__init__()
        self._min_gap_percent = self.Param("MinGapPercent", 0.02).SetGreaterThanZero().SetDisplay("Min Gap %", "Minimum gap between the previous close and the open", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gap_fill_reversal_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(gap_fill_reversal_strategy, self).OnStarted2(time)

        self._prev_close = None

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

        prev_close = self._prev_close
        self._prev_close = candle.ClosePrice

        if prev_close is None or prev_close <= 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        gap = (candle.OpenPrice - prev_close) / prev_close * Decimal(100)
        min_gap = Decimal(self._min_gap_percent.Value)

        if gap >= min_gap and candle.LowPrice <= prev_close and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif -gap >= min_gap and candle.HighPrice >= prev_close and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return gap_fill_reversal_strategy()
