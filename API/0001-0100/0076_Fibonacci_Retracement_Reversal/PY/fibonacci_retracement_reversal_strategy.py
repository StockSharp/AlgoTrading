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

class fibonacci_retracement_reversal_strategy(Strategy):
    """
    Fibonacci Retracement Reversal strategy.
    The swing is the highest high and lowest low of the previous SwingLookbackPeriod candles; it rose when the low came first.
    In a rising swing a bullish candle closing within FibLevelBuffer percent of the 61.8% or 78.6% retracement buys,
    in a falling swing a bearish candle near those levels sells. The target is the swing's 50% level, and a percent stop protects the trade.
    """

    def __init__(self):
        super(fibonacci_retracement_reversal_strategy, self).__init__()
        self._swing_lookback_period = self.Param("SwingLookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Swing Lookback", "Previous candles that form the swing", "Indicators")
        self._fib_level_buffer = self.Param("FibLevelBuffer", 0.5).SetNotNegative().SetDisplay("Level Buffer %", "Distance from a retracement level that counts as a test, in percent", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candles = []
        self._target = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibonacci_retracement_reversal_strategy, self).OnReseted()
        self._candles = []
        self._target = Decimal(0)

    def OnStarted2(self, time):
        super(fibonacci_retracement_reversal_strategy, self).OnStarted2(time)

        self._candles = []
        self._target = Decimal(0)

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

        # The swing is formed by the candles before this one.
        period = self._swing_lookback_period.Value
        swing = list(self._candles)
        self._candles.append((candle.HighPrice, candle.LowPrice))
        if len(self._candles) > period:
            self._candles.pop(0)

        if len(swing) < period or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close >= self._target:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close <= self._target:
                self.BuyMarket(-self.Position)
            return

        high = max(c[0] for c in swing)
        low = min(c[1] for c in swing)
        high_index = next(i for i, c in enumerate(swing) if c[0] == high)
        low_index = next(i for i, c in enumerate(swing) if c[1] == low)
        price_range = high - low
        if price_range <= 0:
            return

        middle = low + price_range / Decimal(2)
        buffer = Decimal(self._fib_level_buffer.Value) / Decimal(100)

        def near(level):
            return Math.Abs(close - level) <= level * buffer

        if low_index < high_index:
            # Rising swing: buy a bullish candle at a deep pullback.
            if close > candle.OpenPrice and close < middle and (near(high - price_range * Decimal(0.618)) or near(high - price_range * Decimal(0.786))):
                self.BuyMarket(self.Volume)
                self._target = middle
        elif high_index < low_index:
            # Falling swing: sell a bearish candle at a deep rebound.
            if close < candle.OpenPrice and close > middle and (near(low + price_range * Decimal(0.618)) or near(low + price_range * Decimal(0.786))):
                self.SellMarket(self.Volume)
                self._target = middle

    def CreateClone(self):
        return fibonacci_retracement_reversal_strategy()
