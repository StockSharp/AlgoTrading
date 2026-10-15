import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class obv_divergence_strategy(Strategy):
    """
    OBV (On-Balance Volume) Divergence strategy.
    Bullish divergence: the low falls below the lows of the previous DivergencePeriod candles while OBV stays above its value
    at the earlier low. Bearish divergence: the high rises above their highs while OBV stays below its value at the earlier high.
    A divergence opens a position while flat; it closes when the close crosses back over the moving average or at the percent stop.
    """

    def __init__(self):
        super(obv_divergence_strategy, self).__init__()
        self._divergence_period = self.Param("DivergencePeriod", 5).SetGreaterThanZero().SetDisplay("Divergence Period", "Previous candles the new extreme is compared with", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for SMA exit signal", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._history = []
        self._obv = Decimal(0)
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(obv_divergence_strategy, self).OnReseted()
        self._history = []
        self._obv = Decimal(0)
        self._prev_close = None

    def OnStarted2(self, time):
        super(obv_divergence_strategy, self).OnStarted2(time)

        self._history = []
        self._obv = Decimal(0)
        self._prev_close = None

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        if self._prev_close is not None:
            if close > self._prev_close:
                self._obv += candle.TotalVolume
            elif close < self._prev_close:
                self._obv -= candle.TotalVolume
        self._prev_close = close

        # Compare this candle with the DivergencePeriod candles before it.
        period = self._divergence_period.Value
        previous = list(self._history)
        self._history.append((candle.HighPrice, candle.LowPrice, self._obv))
        if len(self._history) > period:
            self._history.pop(0)

        if len(previous) < period or not sma_value.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = sma_value.GetValue[Decimal](None)

        if self.Position > 0:
            if close > ma:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close < ma:
                self.BuyMarket(-self.Position)
            return

        lowest = min(previous, key=lambda c: c[1])
        highest = max(previous, key=lambda c: c[0])

        bullish = candle.LowPrice < lowest[1] and self._obv > lowest[2]
        bearish = candle.HighPrice > highest[0] and self._obv < highest[2]

        if bullish and not bearish:
            self.BuyMarket(self.Volume)
        elif bearish and not bullish:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return obv_divergence_strategy()
