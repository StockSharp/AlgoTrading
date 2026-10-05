import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_breakout_strategy(Strategy):
    """
    MACD Breakout strategy.
    The bands lie DeviationMultiplier standard deviations around the average of the last SmaPeriod MACD values, the current one included.
    MACD above the upper band goes long and MACD below the lower band goes short,
    reversing an opposite position. A long closes once MACD is back below its average and a short once it is back above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(macd_breakout_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero().SetDisplay("Average Period", "Values of MACD the average and the standard deviation span", "Indicators")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "Standard deviations between the average and a band", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._values = []

    def OnReseted(self):
        super(macd_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_ema_period.Value
        macd.Macd.LongMa.Length = self._slow_ema_period.Value
        macd.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        value = macd_value.Macd

        period = self._sma_period.Value
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
        multiplier = Decimal(self._deviation_multiplier.Value)
        upper = mean + multiplier * deviation
        lower = mean - multiplier * deviation

        if value > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif value < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and value < mean:
            self.SellMarket(self.Position)
        elif self.Position < 0 and value > mean:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return macd_breakout_strategy()
