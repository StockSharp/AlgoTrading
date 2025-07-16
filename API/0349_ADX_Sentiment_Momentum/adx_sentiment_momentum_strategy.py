import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

import random

class adx_sentiment_momentum_strategy(Strategy):
    """ADX strategy with Sentiment Momentum filter."""

    def __init__(self):
        """Initialize adx_sentiment_momentum_strategy."""
        super(adx_sentiment_momentum_strategy, self).__init__()

        # ADX Period.
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(5, 30) \
            .SetCanOptimize(True) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")

        # ADX Threshold for strong trend.
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetRange(15.0, 35.0) \
            .SetCanOptimize(True) \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")

        # Period for sentiment momentum calculation.
        self._sentiment_period = self.Param("SentimentPeriod", 5) \
            .SetRange(3, 10) \
            .SetCanOptimize(True) \
            .SetDisplay("Sentiment Period", "Period for sentiment momentum calculation", "Sentiment")

        # Stop loss percentage.
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetCanOptimize(True) \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")

        # Candle type for strategy calculation.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx = None
        self._prev_sentiment = 0
        self._current_sentiment = 0
        self._sentiment_momentum = 0

    @property
    def AdxPeriod(self):
        """ADX Period."""
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        """ADX Threshold for strong trend."""
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def SentimentPeriod(self):
        """Period for sentiment momentum calculation."""
        return self._sentiment_period.Value

    @SentimentPeriod.setter
    def SentimentPeriod(self, value):
        self._sentiment_period.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(adx_sentiment_momentum_strategy, self).OnStarted(time)

        # Create ADX Indicator
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        # Initialize sentiment values
        self._prev_sentiment = 0
        self._current_sentiment = 0
        self._sentiment_momentum = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(2, UnitTypes.Percent),   # Take profit 2%
            Unit(self.StopLoss, UnitTypes.Percent)  # Stop loss based on parameter
        )

    def ProcessCandle(self, candle, adx_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Simulate sentiment data and calculate momentum
        self.UpdateSentiment(candle)

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        typed_adx = adx_value
        adx_main = typed_adx.MovingAverage
        di_plus = typed_adx.Dx.Plus
        di_minus = typed_adx.Dx.Minus

        # Entry logic based on ADX and sentiment momentum
        if adx_main > self.AdxThreshold and di_plus > di_minus and self._sentiment_momentum > 0 and self.Position <= 0:
            # Strong uptrend with positive sentiment momentum - Long entry
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: ADX={0}, +DI={1}, -DI={2}, Sentiment Momentum={3}".format(
                adx_main, di_plus, di_minus, self._sentiment_momentum))
        elif adx_main > self.AdxThreshold and di_minus > di_plus and self._sentiment_momentum < 0 and self.Position >= 0:
            # Strong downtrend with negative sentiment momentum - Short entry
            self.SellMarket(self.Volume)
            self.LogInfo("Sell Signal: ADX={0}, +DI={1}, -DI={2}, Sentiment Momentum={3}".format(
                adx_main, di_plus, di_minus, self._sentiment_momentum))

        # Exit logic
        if adx_main < 20 and self.Position != 0:
            # Exit when trend weakens (ADX below 20)
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: ADX={0}".format(adx_main))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: ADX={0}".format(adx_main))

    def UpdateSentiment(self, candle):
        # This is a placeholder for real sentiment analysis data
        # In a real implementation, this would connect to a sentiment data provider

        # Update sentiment values
        self._prev_sentiment = self._current_sentiment

        # Simulate sentiment based on price action and some randomness
        self._current_sentiment = self.SimulateSentiment(candle)

        # Calculate momentum as the change in sentiment
        self._sentiment_momentum = self._current_sentiment - self._prev_sentiment

    def SimulateSentiment(self, candle):
        # Base sentiment on price movement (up = positive sentiment, down = negative sentiment)
        price_up = candle.OpenPrice < candle.ClosePrice
        price_change = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice

        # Calculate base sentiment from price change
        base_sentiment = price_change * 10  # Scale up for easier interpretation

        # Add noise to simulate real-world sentiment data
        noise = random.random() * 0.2 - 0.1

        # Sometimes sentiment can diverge from price action
        if random.random() > 0.7:
            noise *= 2  # Occasionally larger divergences

        return base_sentiment + noise

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_sentiment_momentum_strategy()
