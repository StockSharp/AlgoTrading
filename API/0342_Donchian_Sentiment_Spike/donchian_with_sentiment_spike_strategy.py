import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_with_sentiment_spike_strategy(Strategy):
    """Donchian with Sentiment Spike strategy.
    Entry condition:
    Long: Price > Max(High, N) && Sentiment_Score > Avg(Sentiment, M) + k*StdDev(Sentiment, M)
    Short: Price < Min(Low, N) && Sentiment_Score < Avg(Sentiment, M) - k*StdDev(Sentiment, M)
    Exit condition:
    Long: Price < (Max(High, N) + Min(Low, N))/2
    Short: Price > (Max(High, N) + Min(Low, N))/2
    """

    def __init__(self):
        super(donchian_with_sentiment_spike_strategy, self).__init__()

        # Donchian channel period.
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Donchian channel period", "Donchian Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Sentiment averaging period.
        self._sentiment_period = self.Param("SentimentPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Sentiment Period", "Sentiment averaging period", "Sentiment Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Sentiment standard deviation multiplier.
        self._sentiment_multiplier = self.Param("SentimentMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sentiment StdDev Multiplier", "Multiplier for sentiment standard deviation", "Sentiment Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop-loss percentage.
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Type of candles to use.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._sentiment_history = []
        self._sentiment_average = 0.0
        self._sentiment_std_dev = 0.0
        self._current_sentiment = 0.0
        self._mid_channel = 0.0
        self._is_long = False
        self._is_short = False

    @property
    def donchian_period(self):
        return self._donchian_period.Value

    @donchian_period.setter
    def donchian_period(self, value):
        self._donchian_period.Value = value

    @property
    def sentiment_period(self):
        return self._sentiment_period.Value

    @sentiment_period.setter
    def sentiment_period(self, value):
        self._sentiment_period.Value = value

    @property
    def sentiment_multiplier(self):
        return self._sentiment_multiplier.Value

    @sentiment_multiplier.setter
    def sentiment_multiplier(self, value):
        self._sentiment_multiplier.Value = value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(donchian_with_sentiment_spike_strategy, self).OnStarted(time)

        # Initialize flags
        self._is_long = False
        self._is_short = False
        self._mid_channel = 0.0
        self._sentiment_history.clear()
        self._sentiment_average = 0.0
        self._sentiment_std_dev = 0.0
        self._current_sentiment = 0.0

        # Create Donchian Channel indicator
        donchian = DonchianChannels()
        donchian.Length = self.donchian_period

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, donchian_value):
        """Process each candle and Donchian Channel values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update sentiment data (in a real system, this would come from external source)
        self.UpdateSentiment(candle)

        # Extract Donchian Channel values
        try:
            upper_band = float(donchian_value.UpperBand)
            lower_band = float(donchian_value.LowerBand)
            middle_band = float(donchian_value.Middle)
        except Exception:
            return

        # Store middle band for exit conditions
        self._mid_channel = middle_band

        # Calculate sentiment thresholds
        bullish_threshold = self._sentiment_average + self.sentiment_multiplier * self._sentiment_std_dev
        bearish_threshold = self._sentiment_average - self.sentiment_multiplier * self._sentiment_std_dev

        price = candle.ClosePrice

        # Trading logic
        # Entry conditions
        # Long entry: Price breaks above upper band with positive sentiment spike
        if price > upper_band and self._current_sentiment > bullish_threshold and not self._is_long and self.Position <= 0:
            self.LogInfo(f"Long signal: Price {price} > Upper Band {upper_band}, Sentiment {self._current_sentiment} > Threshold {bullish_threshold}")
            self.BuyMarket(self.Volume)
            self._is_long = True
            self._is_short = False
        # Short entry: Price breaks below lower band with negative sentiment spike
        elif price < lower_band and self._current_sentiment < bearish_threshold and not self._is_short and self.Position >= 0:
            self.LogInfo(f"Short signal: Price {price} < Lower Band {lower_band}, Sentiment {self._current_sentiment} < Threshold {bearish_threshold}")
            self.SellMarket(self.Volume)
            self._is_short = True
            self._is_long = False

        # Exit conditions
        # Exit long: Price falls below middle band
        if self._is_long and price < self._mid_channel and self.Position > 0:
            self.LogInfo(f"Exit long: Price {price} < Middle Band {self._mid_channel}")
            self.SellMarket(Math.Abs(self.Position))
            self._is_long = False
        # Exit short: Price rises above middle band
        elif self._is_short and price > self._mid_channel and self.Position < 0:
            self.LogInfo(f"Exit short: Price {price} > Middle Band {self._mid_channel}")
            self.BuyMarket(Math.Abs(self.Position))
            self._is_short = False

    def UpdateSentiment(self, candle):
        """Update sentiment score based on candle data (simulation).
        In a real implementation, this would fetch data from an external source."""
        # Simple sentiment simulation based on price action
        # In reality, this would come from social media or news sentiment API

        body_size = abs(candle.ClosePrice - candle.OpenPrice)
        total_size = candle.HighPrice - candle.LowPrice

        if total_size == 0:
            sentiment = 0
        else:
            body_ratio = body_size / total_size

            # Bullish candle with strong body
            if candle.ClosePrice > candle.OpenPrice:
                sentiment = body_ratio * 2  # 0 to 2 scale
            # Bearish candle with strong body
            else:
                sentiment = -body_ratio * 2  # -2 to 0 scale

            # Add some randomness
            sentiment += (random.random() - 0.5) * 0.5

        # Ensure sentiment is within -2 to 2 range
        sentiment = max(min(sentiment, 2), -2)

        self._current_sentiment = sentiment

        # Add to history
        self._sentiment_history.append(self._current_sentiment)
        if len(self._sentiment_history) > self.sentiment_period:
            self._sentiment_history.pop(0)

        # Calculate average
        self._sentiment_average = sum(self._sentiment_history) / len(self._sentiment_history) if self._sentiment_history else 0

        # Calculate standard deviation
        if len(self._sentiment_history) > 1:
            sum_squared = sum((value - self._sentiment_average) ** 2 for value in self._sentiment_history)
            self._sentiment_std_dev = Math.Sqrt(sum_squared / (len(self._sentiment_history) - 1))
        else:
            self._sentiment_std_dev = 0.5  # Default value until we have enough data

        self.LogInfo(f"Sentiment: {self._current_sentiment}, Avg: {self._sentiment_average}, StdDev: {self._sentiment_std_dev}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_with_sentiment_spike_strategy()

