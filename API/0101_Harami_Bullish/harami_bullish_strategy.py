import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class harami_bullish_strategy(Strategy):
    """
    Harami Bullish pattern strategy.
    Strategy enters long position when a bullish harami pattern is detected.

    """

    def __init__(self):
        super(harami_bullish_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage below pattern's low", "Protection") \
            .SetRange(0.1, 5.0)

        # Internal state
        self._previousCandle = None
        self._patternDetected = False

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss as percentage below the pattern's low."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(harami_bullish_strategy, self).OnReseted()
        self._previousCandle = None
        self._patternDetected = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up subscriptions, protections, and charting.

        :param time: The time when the strategy started.
        """
        super(harami_bullish_strategy, self).OnStarted(time)

        self._previousCandle = None
        self._patternDetected = False

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the candle processor
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip first candle as we need at least one previous candle to detect the pattern
        if self._previousCandle is None:
            self._previousCandle = candle
            return

        # Check for Harami Bullish pattern:
        # 1. Previous candle is bearish (close < open)
        # 2. Current candle is bullish (close > open)
        # 3. Current candle is completely inside the previous candle (high < prev high and low > prev low)
        isPreviousBearish = self._previousCandle.OpenPrice > self._previousCandle.ClosePrice
        isCurrentBullish = candle.OpenPrice < candle.ClosePrice
        isInsidePrevious = candle.HighPrice < self._previousCandle.HighPrice and \
            candle.LowPrice > self._previousCandle.LowPrice

        # Detect Harami Bullish pattern
        if isPreviousBearish and isCurrentBullish and isInsidePrevious and not self._patternDetected:
            self._patternDetected = True

            # Calculate position size (if we already have a position, this will close it and open a new one)
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position at market price
            self.BuyMarket(volume)

            # Set stop-loss level
            stopLossLevel = float(candle.LowPrice * (1 - self.StopLossPercent / 100))

            self.LogInfo("Harami Bullish detected. Buying at {0}. Stop-loss set at {1}".format(
                candle.ClosePrice, stopLossLevel))
        elif self._patternDetected:
            # Check for exit condition: price breaks above the previous candle's high
            if candle.HighPrice > self._previousCandle.HighPrice:
                # If we have a long position and price breaks above previous high, close the position
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self._patternDetected = False

                    self.LogInfo("Exit signal: Price broke above previous high ({0}). Closing position at {1}".format(
                        self._previousCandle.HighPrice, candle.ClosePrice))

        # Store current candle as previous for the next iteration
        self._previousCandle = candle

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return harami_bullish_strategy()