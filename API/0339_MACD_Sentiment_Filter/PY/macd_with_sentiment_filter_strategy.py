import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
import random

class macd_with_sentiment_filter_strategy(Strategy):
    """
    MACD with Sentiment Filter strategy.
    Entry condition:
    Long: MACD > Signal && Sentiment_Score > Threshold
    Short: MACD < Signal && Sentiment_Score < -Threshold
    Exit condition:
    Long: MACD < Signal
    Short: MACD > Signal
    """

    def __init__(self):
        super(macd_with_sentiment_filter_strategy, self).__init__()

        # MACD Fast period.
        self._macd_fast = (
            self.Param("MacdFast", 12)
            .SetGreaterThanZero()
            .SetDisplay("MACD Fast", "Fast moving average period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(8, 20, 1)
        )

        # MACD Slow period.
        self._macd_slow = (
            self.Param("MacdSlow", 26)
            .SetGreaterThanZero()
            .SetDisplay("MACD Slow", "Slow moving average period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(20, 34, 2)
        )

        # MACD Signal period.
        self._macd_signal = (
            self.Param("MacdSignal", 9)
            .SetGreaterThanZero()
            .SetDisplay("MACD Signal", "Signal line period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(5, 13, 1)
        )

        # Sentiment threshold for entry signal.
        self._threshold = (
            self.Param("Threshold", 0.5)
            .SetGreaterThanZero()
            .SetDisplay("Sentiment Threshold", "Threshold for sentiment filter", "Sentiment Settings")
            .SetCanOptimize(True)
            .SetOptimize(0.2, 0.8, 0.1)
        )

        # Stop-loss percentage.
        self._stop_loss = (
            self.Param("StopLoss", 2.0)
            .SetGreaterThanZero()
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")
            .SetCanOptimize(True)
            .SetOptimize(1.0, 3.0, 0.5)
        )

        # Type of candles to use.
        self._candle_type = (
            self.Param("CandleType", tf(15))
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        )

        # Sentiment score from external data source (simplified with simulation for this example)
        self._sentiment_score = 0.0
        # Last MACD and Signal values stored from the previous candle
        self._prev_macd = 0.0
        self._prev_signal = 0.0

    @property
    def macd_fast(self):
        return self._macd_fast.Value

    @macd_fast.setter
    def macd_fast(self, value):
        self._macd_fast.Value = value

    @property
    def macd_slow(self):
        return self._macd_slow.Value

    @macd_slow.setter
    def macd_slow(self, value):
        self._macd_slow.Value = value

    @property
    def macd_signal(self):
        return self._macd_signal.Value

    @macd_signal.setter
    def macd_signal(self, value):
        self._macd_signal.Value = value

    @property
    def threshold(self):
        return self._threshold.Value

    @threshold.setter
    def threshold(self, value):
        self._threshold.Value = value

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

    def OnReseted(self):
        super(macd_with_sentiment_filter_strategy, self).OnReseted()
        # reset stored values
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._sentiment_score = 0.0

    def OnStarted(self, time):
        super(macd_with_sentiment_filter_strategy, self).OnStarted(time)

        # Create MACD indicator
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, macd_value):
        """Process each candle and MACD values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update sentiment score (in a real system this would come from external source)
        self.UpdateSentimentScore(candle)

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        # Store previous MACD values for state tracking
        prev_macd_over_signal = self._prev_macd > self._prev_signal
        curr_macd_over_signal = macd_value.Macd > macd_value.Signal

        # Update previous values for next candle
        self._prev_macd = macd_value.Macd
        self._prev_signal = macd_value.Signal

        # First candle, just store values
        if self.IsFirstRun():
            return

        # Entry conditions with sentiment filter
        if prev_macd_over_signal != curr_macd_over_signal:
            # MACD crossed above signal with positive sentiment - go long
            if curr_macd_over_signal and self._sentiment_score > self.threshold and self.Position <= 0:
                self.LogInfo("Long signal: MACD crossed above signal with positive sentiment")
                self.BuyMarket(self.Volume)
            # MACD crossed below signal with negative sentiment - go short
            elif not curr_macd_over_signal and self._sentiment_score < -self.threshold and self.Position >= 0:
                self.LogInfo("Short signal: MACD crossed below signal with negative sentiment")
                self.SellMarket(self.Volume)
        # Exit conditions (without sentiment filter)
        else:
            # MACD below signal - exit long position
            if not curr_macd_over_signal and self.Position > 0:
                self.LogInfo("Exit long: MACD below signal")
                self.SellMarket(abs(self.Position))
            # MACD above signal - exit short position
            elif curr_macd_over_signal and self.Position < 0:
                self.LogInfo("Exit short: MACD above signal")
                self.BuyMarket(abs(self.Position))

    def UpdateSentimentScore(self, candle):
        """Update sentiment score based on candle data (simulation)."""
        # Simple simulation of sentiment based on candle pattern
        # In reality, this would be a call to a sentiment API or database
        body_size = float(abs(candle.ClosePrice - candle.OpenPrice))
        total_size = float(candle.HighPrice - candle.LowPrice)

        if total_size == 0:
            return

        body_ratio = body_size / total_size

        # Bullish candle with strong body
        if candle.ClosePrice > candle.OpenPrice and body_ratio > 0.7:
            self._sentiment_score = min(self._sentiment_score + 0.2, 1.0)
        # Bearish candle with strong body
        elif candle.ClosePrice < candle.OpenPrice and body_ratio > 0.7:
            self._sentiment_score = max(self._sentiment_score - 0.2, -1.0)
        # Add random noise to sentiment
        else:
            self._sentiment_score += (random.random() - 0.5) * 0.1
            self._sentiment_score = max(min(self._sentiment_score, 1.0), -1.0)

        self.LogInfo("Updated sentiment score: {0}".format(self._sentiment_score))

    def IsFirstRun(self):
        """Check if this is the first run to avoid trading on first candle."""
        return self._prev_macd == 0 and self._prev_signal == 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_with_sentiment_filter_strategy()
