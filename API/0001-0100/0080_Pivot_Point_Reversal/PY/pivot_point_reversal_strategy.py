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

class pivot_point_reversal_strategy(Strategy):
    """
    Pivot Point Reversal strategy.
    Each day the classic floor pivots come from the previous day's high, low and close: P = (H + L + C) / 3, R1 = 2P - L, S1 = 2P - H.
    While flat, a bullish candle that dips to S1 and closes above it buys, and a bearish candle that reaches R1 and closes below it sells.
    The position closes when the close reaches the central pivot or at the percent stop.
    """

    def __init__(self):
        super(pivot_point_reversal_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._day = None
        self._day_high = Decimal(0)
        self._day_low = Decimal(0)
        self._day_close = Decimal(0)
        self._levels = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pivot_point_reversal_strategy, self).OnReseted()
        self._day = None
        self._day_high = Decimal(0)
        self._day_low = Decimal(0)
        self._day_close = Decimal(0)
        self._levels = None

    def OnStarted2(self, time):
        super(pivot_point_reversal_strategy, self).OnStarted2(time)

        self._day = None
        self._levels = None

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
            # A new day takes its pivots from the day that has just ended.
            if self._day is not None:
                pivot = (self._day_high + self._day_low + self._day_close) / Decimal(3)
                self._levels = (pivot, Decimal(2) * pivot - self._day_low, Decimal(2) * pivot - self._day_high)
            self._day = day
            self._day_high = candle.HighPrice
            self._day_low = candle.LowPrice
        else:
            self._day_high = Math.Max(self._day_high, candle.HighPrice)
            self._day_low = Math.Min(self._day_low, candle.LowPrice)
        self._day_close = candle.ClosePrice

        if self._levels is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        pivot, r1, s1 = self._levels
        close = candle.ClosePrice

        if self.Position > 0:
            if close >= pivot:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if close <= pivot:
                self.BuyMarket(-self.Position)
        elif close > candle.OpenPrice and candle.LowPrice <= s1 and close > s1:
            self.BuyMarket(self.Volume)
        elif close < candle.OpenPrice and candle.HighPrice >= r1 and close < r1:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return pivot_point_reversal_strategy()
