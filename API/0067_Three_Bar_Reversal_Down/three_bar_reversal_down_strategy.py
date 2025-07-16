import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("System.Collections")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from System.Collections.Generic import Queue
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class three_bar_reversal_down_strategy(Strategy):
    """
    Strategy based on the Three-Bar Reversal Down pattern.
    This pattern consists of three consecutive bars where:
    1. First bar is bullish (close > open)
    2. Second bar is bullish with a higher high than the first
    3. Third bar is bearish and closes below the low of the second bar
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(three_bar_reversal_down_strategy, self).__init__()
        
        # Initialize internal state
        self._lastThreeCandles = Queue[object](3)
        self._highestIndicator = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage above pattern's high for stop-loss", "Risk Management")

        self._requireUptrend = self.Param("RequireUptrend", True) \
            .SetDisplay("Require Uptrend", "Whether to require a prior uptrend", "Pattern Parameters")

        self._uptrendLength = self.Param("UptrendLength", 5) \
            .SetDisplay("Uptrend Length", "Number of bars to check for uptrend", "Pattern Parameters")

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def RequireUptrend(self):
        return self._requireUptrend.Value

    @RequireUptrend.setter
    def RequireUptrend(self, value):
        self._requireUptrend.Value = value

    @property
    def UptrendLength(self):
        return self._uptrendLength.Value

    @UptrendLength.setter
    def UptrendLength(self, value):
        self._uptrendLength.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(three_bar_reversal_down_strategy, self).OnReseted()
        self._lastThreeCandles.Clear()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(three_bar_reversal_down_strategy, self).OnStarted(time)

        # Clear candle queue
        self._lastThreeCandles.Clear()

        # Create highest indicator for uptrend identification
        self._highestIndicator = Highest()
        self._highestIndicator.Length = self.UptrendLength

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing with the highest indicator
        subscription.Bind(self._highestIndicator, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, highestValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param highestValue: The highest value from indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already in position, no need to search for new patterns
        if self.Position < 0:
            self.UpdateCandleQueue(candle)
            return

        # Add current candle to the queue and maintain its size
        self.UpdateCandleQueue(candle)

        # Check if we have enough candles for pattern detection
        if self._lastThreeCandles.Count < 3:
            return

        # Get the three candles for pattern analysis
        candles = list(self._lastThreeCandles)
        firstCandle = candles[0]
        secondCandle = candles[1]
        thirdCandle = candles[2]  # Current candle

        # Check for Three-Bar Reversal Down pattern:
        # 1. First candle is bullish
        isFirstBullish = firstCandle.ClosePrice > firstCandle.OpenPrice

        # 2. Second candle is bullish with a higher high
        isSecondBullish = secondCandle.ClosePrice > secondCandle.OpenPrice
        hasSecondHigherHigh = secondCandle.HighPrice > firstCandle.HighPrice

        # 3. Third candle is bearish and closes below second candle's low
        isThirdBearish = thirdCandle.ClosePrice < thirdCandle.OpenPrice
        doesThirdCloseBelowSecondLow = thirdCandle.ClosePrice < secondCandle.LowPrice

        # 4. Check if we're in an uptrend (if required)
        isInUptrend = not self.RequireUptrend or self.IsInUptrend(highestValue)

        # Check if the pattern is complete
        if (isFirstBullish and isSecondBullish and hasSecondHigherHigh and 
            isThirdBearish and doesThirdCloseBelowSecondLow and isInUptrend):
            # Pattern found - take short position
            patternHigh = Math.Max(secondCandle.HighPrice, thirdCandle.HighPrice)
            stopLoss = patternHigh * (1 + self.StopLossPercent / 100)

            self.SellMarket(self.Volume)
            self.LogInfo("Three-Bar Reversal Down pattern detected at {0}".format(thirdCandle.OpenTime))
            self.LogInfo("First bar: O={0}, C={1}, H={2}".format(
                firstCandle.OpenPrice, firstCandle.ClosePrice, firstCandle.HighPrice))
            self.LogInfo("Second bar: O={0}, C={1}, H={2}".format(
                secondCandle.OpenPrice, secondCandle.ClosePrice, secondCandle.HighPrice))
            self.LogInfo("Third bar: O={0}, C={1}".format(
                thirdCandle.OpenPrice, thirdCandle.ClosePrice))
            self.LogInfo("Stop Loss set at {0}".format(stopLoss))

    def UpdateCandleQueue(self, candle):
        """
        Update the candle queue with the latest candle
        
        :param candle: The candle to add.
        """
        self._lastThreeCandles.Enqueue(candle)
        while self._lastThreeCandles.Count > 3:
            self._lastThreeCandles.Dequeue()

    def IsInUptrend(self, highestValue):
        """
        Check if we're in an uptrend
        
        :param highestValue: The highest value from indicator.
        :return: True if in uptrend.
        """
        # If we have the highest indicator value, check if current price is near it
        if self._lastThreeCandles.Count > 0:
            candlesList = list(self._lastThreeCandles)
            lastCandle = candlesList[-1]  # Get the last candle
            return Math.Abs(lastCandle.HighPrice - highestValue) / highestValue < 0.03
        
        return False

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return three_bar_reversal_down_strategy()