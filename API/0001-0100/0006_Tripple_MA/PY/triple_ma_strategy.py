import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class triple_ma_strategy(Strategy):
    """
    Strategy based on Triple Moving Average crossover.
    Enters long when the short MA is above both the middle and the long MA, and short when it is below both.
    A cross of the short and the middle MA closes the position, and a percent stop protects it.
    """

    def __init__(self):
        super(triple_ma_strategy, self).__init__()
        self._short_ma_period = self.Param("ShortMaPeriod", 5).SetGreaterThanZero().SetDisplay("Short MA Period", "Period for short moving average", "Indicators")
        self._middle_ma_period = self.Param("MiddleMaPeriod", 20).SetGreaterThanZero().SetDisplay("Middle MA Period", "Period for middle moving average", "Indicators")
        self._long_ma_period = self.Param("LongMaPeriod", 50).SetGreaterThanZero().SetDisplay("Long MA Period", "Period for long moving average", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")


    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(triple_ma_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(triple_ma_strategy, self).OnStarted2(time)

        short_ma = ExponentialMovingAverage()
        short_ma.Length = self._short_ma_period.Value
        middle_ma = ExponentialMovingAverage()
        middle_ma.Length = self._middle_ma_period.Value
        long_ma = ExponentialMovingAverage()
        long_ma.Length = self._long_ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ma, middle_ma, long_ma, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, middle_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, short_val, middle_val, long_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_setup = short_val > middle_val and short_val > long_val
        short_setup = short_val < middle_val and short_val < long_val
        position = self.Position

        if position > 0:
            # The short MA has crossed below the middle one.
            if short_val < middle_val:
                self.SellMarket(self.Volume + position if short_setup else position)
        elif position < 0:
            if short_val > middle_val:
                self.BuyMarket(self.Volume - position if long_setup else -position)
        elif long_setup:
            self.BuyMarket(self.Volume)
        elif short_setup:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return triple_ma_strategy()
