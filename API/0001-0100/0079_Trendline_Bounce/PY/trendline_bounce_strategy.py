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

class trendline_bounce_strategy(Strategy):
    """
    Trendline Bounce strategy.
    Support is the regression line of the lows of the previous TrendlinePeriod candles and resistance that of their highs,
    both extended to the current candle. While flat, a bullish candle whose low comes within BounceThresholdPercent of a rising
    support and that closes above the moving average buys; a bearish candle at a falling resistance closing below it sells.
    A cross of the moving average or a percent stop closes the position.
    """

    def __init__(self):
        super(trendline_bounce_strategy, self).__init__()
        self._trendline_period = self.Param("TrendlinePeriod", 20).SetRange(2, 1000).SetDisplay("Trendline Period", "Previous candles the trendlines are fitted to", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._bounce_threshold_percent = self.Param("BounceThresholdPercent", 0.5).SetNotNegative().SetDisplay("Bounce Threshold %", "How close to a trendline the candle must come", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._highs = []
        self._lows = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trendline_bounce_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(trendline_bounce_strategy, self).OnStarted2(time)

        self._highs = []
        self._lows = []

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

        # The lines are fitted to the candles before this one.
        period = self._trendline_period.Value
        ready = len(self._highs) == period
        if ready:
            support_slope, support = self._fit_and_extend(self._lows)
            resistance_slope, resistance = self._fit_and_extend(self._highs)

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)
        if len(self._highs) > period:
            self._highs.pop(0)
            self._lows.pop(0)

        if not ready or not sma_value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if self.Position > 0:
            if close < ma:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close > ma:
                self.BuyMarket(-self.Position)
            return

        threshold = Decimal(self._bounce_threshold_percent.Value) / Decimal(100)

        if support_slope > 0 and candle.LowPrice <= support * (Decimal(1) + threshold) and close > candle.OpenPrice and close > ma:
            self.BuyMarket(self.Volume)
        elif resistance_slope < 0 and candle.HighPrice >= resistance * (Decimal(1) - threshold) and close < candle.OpenPrice and close < ma:
            self.SellMarket(self.Volume)

    @staticmethod
    def _fit_and_extend(values):
        # Least squares over x = 0..n-1, extended to x = n.
        n = len(values)
        sum_x = Decimal(0)
        sum_y = Decimal(0)
        sum_xy = Decimal(0)
        sum_x2 = Decimal(0)
        for i in range(n):
            x = Decimal(i)
            sum_x += x
            sum_y += values[i]
            sum_xy += x * values[i]
            sum_x2 += x * x
        count = Decimal(n)
        slope = (count * sum_xy - sum_x * sum_y) / (count * sum_x2 - sum_x * sum_x)
        intercept = (sum_y - slope * sum_x) / count
        return slope, intercept + slope * count

    def CreateClone(self):
        return trendline_bounce_strategy()
