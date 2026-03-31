import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_sentiment_momentum_strategy(Strategy):
    """ADX trend strategy filtered by deterministic sentiment momentum."""

    def __init__(self):
        super(adx_sentiment_momentum_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetRange(15.0, 35.0) \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")

        self._sentiment_period = self.Param("SentimentPeriod", 5) \
            .SetRange(3, 10) \
            .SetDisplay("Sentiment Period", "Period for sentiment momentum calculation", "Sentiment")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx = None
        self._prev_sentiment = 0.0
        self._current_sentiment = 0.0
        self._sentiment_momentum = 0.0
        self._prev_di_plus = None
        self._prev_di_minus = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(adx_sentiment_momentum_strategy, self).OnReseted()
        self._adx = None
        self._prev_sentiment = 0.0
        self._current_sentiment = 0.0
        self._sentiment_momentum = 0.0
        self._prev_di_plus = None
        self._prev_di_minus = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adx_sentiment_momentum_strategy, self).OnStarted2(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        self.UpdateSentiment(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_main_val = adx_value.MovingAverage
        di_plus_val = adx_value.Dx.Plus
        di_minus_val = adx_value.Dx.Minus

        if adx_main_val is None or di_plus_val is None or di_minus_val is None:
            return

        adx_main = float(adx_main_val)
        di_plus = float(di_plus_val)
        di_minus = float(di_minus_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        adx_threshold = float(self._adx_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_cross = self._prev_di_plus is not None and self._prev_di_minus is not None and \
            self._prev_di_plus <= self._prev_di_minus and di_plus > di_minus
        bearish_cross = self._prev_di_plus is not None and self._prev_di_minus is not None and \
            self._prev_di_plus >= self._prev_di_minus and di_minus > di_plus
        strong_trend = adx_main >= adx_threshold

        if self._cooldown_remaining == 0 and strong_trend and bullish_cross and self._sentiment_momentum > 0 and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and strong_trend and bearish_cross and self._sentiment_momentum < 0 and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and (adx_main < 20.0 or self._sentiment_momentum < 0):
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (adx_main < 20.0 or self._sentiment_momentum > 0):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_di_plus = di_plus
        self._prev_di_minus = di_minus

    def UpdateSentiment(self, candle):
        self._prev_sentiment = self._current_sentiment
        self._current_sentiment = self.SimulateSentiment(candle)
        self._sentiment_momentum = self._current_sentiment - self._prev_sentiment

    def SimulateSentiment(self, candle):
        range_val = max(float(candle.HighPrice - candle.LowPrice), 1.0)
        body = float(candle.ClosePrice - candle.OpenPrice)
        body_ratio = body / range_val
        range_ratio = range_val / max(float(candle.OpenPrice), 1.0)
        sentiment_period = int(self._sentiment_period.Value)
        trend_factor = min(0.3, range_ratio * sentiment_period)

        sign = 1 if body > 0 else (-1 if body < 0 else 0)
        result = body_ratio + (sign * trend_factor)
        return max(-1.0, min(1.0, result))

    def CreateClone(self):
        return adx_sentiment_momentum_strategy()
