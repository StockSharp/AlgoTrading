import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class adx_stochastic_strategy(Strategy):
    """
    ADX Stochastic strategy.
    Bullish means +DI above -DI and bearish means -DI above +DI. While ADX is above AdxThreshold, %K below StochOversold in a bullish
    trend goes long and %K above StochOverbought in a bearish trend goes short, reversing an opposite position; %K is the stochastic over
    StochPeriod candles smoothed over StochK candles. The position closes once ADX falls below AdxThreshold, and a percent stop limits the loss.
    """

    def __init__(self):
        super(adx_stochastic_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level of a strong trend", "ADX")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(adx_stochastic_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, stochastic, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, stochastic)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, adx_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or not stochastic_value.IsFormed:
            return
        if adx_value.MovingAverage is None or adx_value.Dx.Plus is None or adx_value.Dx.Minus is None or stochastic_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        strength = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        k = stochastic_value.D
        threshold = Decimal(self._adx_threshold.Value)
        strong = strength > threshold

        if strong and k < Decimal(self._stoch_oversold.Value) and plus_di > minus_di and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif strong and k > Decimal(self._stoch_overbought.Value) and minus_di > plus_di and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and strength < threshold:
            self.SellMarket(self.Position)
        elif self.Position < 0 and strength < threshold:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return adx_stochastic_strategy()
