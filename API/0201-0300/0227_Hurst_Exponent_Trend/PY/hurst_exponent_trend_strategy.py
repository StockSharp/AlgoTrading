import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import HurstExponent, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hurst_exponent_trend_strategy(Strategy):
    """
    Hurst Exponent Trend strategy.
    A HurstPeriod Hurst exponent above HurstThreshold marks a trending market. Then a close above the MaPeriod simple moving average goes long
    and a close below it goes short, reversing an opposite position. A long closes once the close is below the average or the exponent
    drops below the threshold, a short mirrors it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(hurst_exponent_trend_strategy, self).__init__()
        self._hurst_period = self.Param("HurstPeriod", 100).SetGreaterThanZero().SetDisplay("Hurst Period", "Period of the Hurst exponent", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the simple moving average", "Indicators")
        self._hurst_threshold = self.Param("HurstThreshold", 0.55).SetDisplay("Hurst Threshold", "Hurst exponent level of a trending market", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hurst_exponent_trend_strategy, self).OnStarted2(time)

        hurst = HurstExponent()
        hurst.Length = self._hurst_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hurst, sma, self._process_candle).Start()

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
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, hurst)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, hurst_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not hurst_value.IsFormed or not sma_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hurst = hurst_value.GetValue[Decimal](None)
        sma = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        threshold = Decimal(self._hurst_threshold.Value)
        trending = hurst > threshold

        if trending and close > sma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif trending and close < sma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (close < sma or hurst < threshold):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close > sma or hurst < threshold):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hurst_exponent_trend_strategy()
