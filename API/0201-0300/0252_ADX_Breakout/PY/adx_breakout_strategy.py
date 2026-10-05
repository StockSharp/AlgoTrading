import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class adx_breakout_strategy(Strategy):
    """
    ADX Breakout strategy.
    The bands lie Multiplier standard deviations around the average of the last AvgPeriod ADX values, the current one included.
    ADX above the upper band on a rising candle goes long and on a falling candle goes short, reversing an opposite position.
    A position closes once ADX falls back below its average, and a percent stop limits the loss.
    """

    def __init__(self):
        super(adx_breakout_strategy, self).__init__()
        self._a_d_x_period = self.Param("ADXPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "Indicators")
        self._avg_period = self.Param("AvgPeriod", 20).SetGreaterThanZero().SetDisplay("Average Period", "Values of ADX the average and the standard deviation span", "Indicators")
        self._multiplier = self.Param("Multiplier", 0.1).SetGreaterThanZero().SetDisplay("Multiplier", "Standard deviations between the average and a band", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._values = []

    def OnReseted(self):
        super(adx_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adx_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._a_d_x_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

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
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        value = adx_value.MovingAverage

        period = self._avg_period.Value
        self._values.append(value)
        if len(self._values) > period:
            self._values.pop(0)

        if len(self._values) < period:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        total = Decimal(0)
        for item in self._values:
            total += item
        mean = total / Decimal(period)
        squares = Decimal(0)
        for item in self._values:
            squares += (item - mean) * (item - mean)
        deviation = Decimal(Math.Sqrt(Decimal.ToDouble(squares / Decimal(period))))
        multiplier = Decimal(self._multiplier.Value)
        upper = mean + multiplier * deviation

        if value > upper and candle.ClosePrice > candle.OpenPrice and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif value > upper and candle.ClosePrice < candle.OpenPrice and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and value < mean:
            self.SellMarket(self.Position)
        elif self.Position < 0 and value < mean:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return adx_breakout_strategy()
