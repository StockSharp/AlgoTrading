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

class z_score_reversal_strategy(Strategy):
    """
    ZScore Reversal strategy.
    The Z-Score is the distance of the close from the LookbackPeriod simple moving average in standard deviations over the same candles.
    A Z-Score below minus ZScoreThreshold goes long and one above ZScoreThreshold goes short, reversing an opposite position.
    A long closes once the Z-Score crosses above zero and a short once it crosses below, and a percent stop limits the loss.
    """

    def __init__(self):
        super(z_score_reversal_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Period of the moving average and the standard deviation", "Indicators")
        self._z_score_threshold = self.Param("ZScoreThreshold", 2.0).SetGreaterThanZero().SetDisplay("Z-Score Threshold", "Z-Score distance from zero that opens a position", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(z_score_reversal_strategy, self).OnStarted2(time)

        ma = SimpleMovingAverage()
        ma.Length = self._lookback_period.Value
        stdev = StandardDeviation()
        stdev.Length = self._lookback_period.Value

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

        deviation = std_dev_value.GetValue[Decimal](None)
        if deviation == 0:
            return

        z_score = (candle.ClosePrice - ma_value.GetValue[Decimal](None)) / deviation
        threshold = Decimal(self._z_score_threshold.Value)

        if z_score < -threshold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif z_score > threshold and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and z_score > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0 and z_score < 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return z_score_reversal_strategy()
