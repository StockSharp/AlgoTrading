import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class nday_breakout_strategy(Strategy):
    """
    N-day high/low breakout strategy.
    Enters long when price pierces the high of the previous N candles while closing above the moving average,
    and short when it pierces their low while closing below it.
    Exits when the close crosses back through the moving average, on the opposite signal, or at the percent stop.
    """

    def __init__(self):
        super(nday_breakout_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Number of candles that form the high/low range", "Strategy Parameters") \
            .SetOptimize(10, 30, 5)
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average that filters entries and triggers exits", "Strategy Parameters") \
            .SetOptimize(10, 30, 5)
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetOptimize(1.0, 3.0, 0.5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        self._highest = None
        self._lowest = None
        self._ma = None
        # Channel of the candles before the current one; a candle never breaks out of a range it is part of.
        self._channel_high = None
        self._channel_low = None

    def OnReseted(self):
        super(nday_breakout_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._ma = None
        self._channel_high = None
        self._channel_low = None

    def OnStarted2(self, time):
        super(nday_breakout_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._lookback_period.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback_period.Value
        self._ma = SimpleMovingAverage()
        self._ma.Length = self._ma_period.Value
        self._channel_high = None
        self._channel_low = None

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles; an hourly candle alone would let it act once an hour.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ma_value = process_value(self._ma, candle.ClosePrice, candle.OpenTime, True)

        # The breakout compares the candle with the range of the candles before it.
        channel_high = self._channel_high
        channel_low = self._channel_low

        high_value = process_value(self._highest, candle.HighPrice, candle.OpenTime, True)
        low_value = process_value(self._lowest, candle.LowPrice, candle.OpenTime, True)

        if self._highest.IsFormed and self._lowest.IsFormed:
            self._channel_high = to_decimal(high_value)
            self._channel_low = to_decimal(low_value)

        if channel_high is None or channel_low is None or not self._ma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = to_decimal(ma_value)
        close = candle.ClosePrice
        position = self.Position

        if candle.HighPrice > channel_high and close > ma and position <= 0:
            self.BuyMarket(self.Volume + abs(position))
        elif candle.LowPrice < channel_low and close < ma and position >= 0:
            self.SellMarket(self.Volume + abs(position))
        elif position > 0 and close < ma:
            self.SellMarket(position)
        elif position < 0 and close > ma:
            self.BuyMarket(-position)

    def CreateClone(self):
        return nday_breakout_strategy()
