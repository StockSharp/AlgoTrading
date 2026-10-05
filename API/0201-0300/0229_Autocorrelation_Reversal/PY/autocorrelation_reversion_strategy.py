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

class autocorrelation_reversion_strategy(Strategy):
    """
    Autocorrelation Reversal strategy.
    The autocorrelation is the lag-one autocorrelation of the close-to-close changes over the last AutoCorrPeriod closes. Below
    AutoCorrThreshold a close under the AutoCorrPeriod simple moving average goes long and a close above it goes short, reversing an opposite
    position. A long closes once the close is above the average or the autocorrelation rises above the threshold, a short mirrors it,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(autocorrelation_reversion_strategy, self).__init__()
        self._auto_corr_period = self.Param("AutoCorrPeriod", 20).SetGreaterThanZero().SetDisplay("Autocorrelation Period", "Closes the autocorrelation and the average span", "Indicators")
        self._auto_corr_threshold = self.Param("AutoCorrThreshold", -0.3).SetDisplay("Autocorrelation Threshold", "Autocorrelation below which the market reverts", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []

    def OnReseted(self):
        super(autocorrelation_reversion_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(autocorrelation_reversion_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._auto_corr_period.Value

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
        period = self._auto_corr_period.Value

        self._closes.append(close)
        if len(self._closes) > period:
            self._closes.pop(0)

        if len(self._closes) < period or not sma_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        autocorrelation = self._calculate_autocorrelation()
        sma = sma_value.GetValue[Decimal](None)
        threshold = Decimal(self._auto_corr_threshold.Value)
        reverting = autocorrelation < threshold

        if reverting and close < sma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif reverting and close > sma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (close > sma or autocorrelation > threshold):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close < sma or autocorrelation > threshold):
            self.BuyMarket(-self.Position)

    def _calculate_autocorrelation(self):
        changes = [self._closes[i] - self._closes[i - 1] for i in range(1, len(self._closes))]

        total = Decimal(0)
        for change in changes:
            total += change
        mean = total / Decimal(len(changes))

        numerator = Decimal(0)
        denominator = Decimal(0)
        for i in range(len(changes)):
            deviation = changes[i] - mean
            denominator += deviation * deviation
            if i > 0:
                numerator += (changes[i - 1] - mean) * deviation

        if denominator == 0:
            return Decimal(0)
        return numerator / denominator

    def CreateClone(self):
        return autocorrelation_reversion_strategy()
