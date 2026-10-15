import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class atr_exhaustion_strategy(Strategy):
    """
    ATR Exhaustion strategy.
    An ATR spike is an ATR above AtrMultiplier times its own AtrAvgPeriod moving average. While flat, a bullish candle on a spike
    buys when the price moving average has been falling and a bearish one sells when it has been rising.
    The only exit is a trailing percent stop.
    """

    def __init__(self):
        super(atr_exhaustion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR", "Indicators")
        self._atr_avg_period = self.Param("AtrAvgPeriod", 20).SetGreaterThanZero().SetDisplay("ATR Average Period", "Period of the moving average of ATR", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "How many times its average ATR must exceed", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Price moving average that defines the prior move", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Trailing stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_average = None
        self._prev_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_exhaustion_strategy, self).OnReseted()
        self._atr_average = None
        self._prev_ma = None

    def OnStarted2(self, time):
        super(atr_exhaustion_strategy, self).OnStarted2(time)

        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = self._atr_avg_period.Value
        self._prev_ma = None

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), isStopTrailing=True, useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished or not atr_value.IsFormed:
            return

        atr = atr_value.GetValue[Decimal](None)
        atr_average = process_value(self._atr_average, atr, candle.OpenTime, True)

        if not sma_value.IsFormed:
            return

        ma = sma_value.GetValue[Decimal](None)
        prev_ma = self._prev_ma
        self._prev_ma = ma

        if not self._atr_average.IsFormed or prev_ma is None or self.Position != 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if atr <= to_decimal(atr_average) * Decimal(self._atr_multiplier.Value):
            return

        close = candle.ClosePrice
        if close > candle.OpenPrice and ma < prev_ma:
            self.BuyMarket(self.Volume)
        elif close < candle.OpenPrice and ma > prev_ma:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return atr_exhaustion_strategy()
