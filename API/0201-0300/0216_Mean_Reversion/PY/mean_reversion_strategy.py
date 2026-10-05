import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class mean_reversion_strategy(Strategy):
    """
    Mean Reversion strategy.
    The bands lie DeviationMultiplier standard deviations around the MovingAveragePeriod simple moving average, both measured over the same
    candles. A close below the lower band goes long and a close above the upper band goes short, reversing an opposite position.
    A long closes once price closes above the average and a short once it closes below, and a percent stop limits the loss.
    """

    def __init__(self):
        super(mean_reversion_strategy, self).__init__()
        self._moving_average_period = self.Param("MovingAveragePeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the moving average and the standard deviation", "Indicators")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Deviation Multiplier", "Standard deviations between the average and a band", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(mean_reversion_strategy, self).OnStarted2(time)

        ma = SimpleMovingAverage()
        ma.Length = self._moving_average_period.Value
        stdev = StandardDeviation()
        stdev.Length = self._moving_average_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, stdev, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stdev)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ma_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return

        if not ma_value.IsFormed or not std_dev_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = ma_value.GetValue[Decimal](None)
        deviation = std_dev_value.GetValue[Decimal](None) * Decimal(self._deviation_multiplier.Value)
        upper = ma + deviation
        lower = ma - deviation
        close = candle.ClosePrice

        if close < lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close > ma:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close < ma:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return mean_reversion_strategy()
