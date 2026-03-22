import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class donchian_with_sentiment_spike_strategy(Strategy):
    """
    Donchian with Sentiment Spike strategy.
    """

    def __init__(self):
        super(donchian_with_sentiment_spike_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Donchian channel period", "Donchian Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._sentiment_period = self.Param("SentimentPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Sentiment Period", "Sentiment averaging period", "Sentiment Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._sentiment_multiplier = self.Param("SentimentMultiplier", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Sentiment StdDev Multiplier", "Multiplier for sentiment standard deviation", "Sentiment Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._sentiment_history = []
        self._sentiment_average = 0.0
        self._sentiment_std_dev = 0.0
        self._current_sentiment = 0.0

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def SentimentPeriod(self):
        return self._sentiment_period.Value

    @SentimentPeriod.setter
    def SentimentPeriod(self, value):
        self._sentiment_period.Value = value

    @property
    def SentimentMultiplier(self):
        return self._sentiment_multiplier.Value

    @SentimentMultiplier.setter
    def SentimentMultiplier(self, value):
        self._sentiment_multiplier.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(donchian_with_sentiment_spike_strategy, self).OnReseted()
        self._sentiment_history = []
        self._sentiment_average = 0.0
        self._sentiment_std_dev = 0.0
        self._current_sentiment = 0.0

    def OnStarted(self, time):
        super(donchian_with_sentiment_spike_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = self.DonchianPeriod
        lowest = Lowest()
        lowest.Length = self.DonchianPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, upper, lower):
        if candle.State != CandleStates.Finished:
            return

        self.UpdateSentiment(candle)

        price = float(candle.ClosePrice)
        upper_val = float(upper)
        lower_val = float(lower)

        if self.Position != 0:
            return

        if price >= upper_val and self._current_sentiment > 0:
            self.BuyMarket()
        elif price <= lower_val and self._current_sentiment < 0:
            self.SellMarket()

    def UpdateSentiment(self, candle):
        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        total_size = float(candle.HighPrice) - float(candle.LowPrice)

        if total_size == 0:
            sentiment = 0.0
        else:
            body_ratio = body_size / total_size
            if float(candle.ClosePrice) > float(candle.OpenPrice):
                sentiment = body_ratio * 2.0
            else:
                sentiment = -body_ratio * 2.0

        sentiment = max(min(sentiment, 2.0), -2.0)
        self._current_sentiment = sentiment

        self._sentiment_history.append(self._current_sentiment)
        if len(self._sentiment_history) > self.SentimentPeriod:
            self._sentiment_history.pop(0)

        if len(self._sentiment_history) > 0:
            self._sentiment_average = sum(self._sentiment_history) / len(self._sentiment_history)
        else:
            self._sentiment_average = 0.0

        if len(self._sentiment_history) > 1:
            sum_sq = 0.0
            for v in self._sentiment_history:
                diff = v - self._sentiment_average
                sum_sq += diff * diff
            self._sentiment_std_dev = Math.Sqrt(sum_sq / (len(self._sentiment_history) - 1))
        else:
            self._sentiment_std_dev = 0.5

    def CreateClone(self):
        return donchian_with_sentiment_spike_strategy()
