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
from StockSharp.Algo.Indicators import Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class three_bar_reversal_up_strategy(Strategy):
    """
    Strategy based on the Three-Bar Reversal Up pattern.
    This pattern consists of three consecutive bars where:
    1. First bar is bearish (close < open)
    2. Second bar is bearish with a lower low than the first
    3. Third bar is bullish and closes above the high of the second bar
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(three_bar_reversal_up_strategy, self).__init__()
        
        # Initialize internal state
        self._lastThreeCandles = Queue[object](3)
        self._lowestIndicator = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage below pattern's low for stop-loss", "Risk Management")

        self._requireDowntrend = self.Param("RequireDowntrend", True) \
            .SetDisplay("Require Downtrend", "Whether to require a prior downtrend", "Pattern Parameters")

        self._downtrendLength = self.Param("DowntrendLength", 5) \
            .SetDisplay("Downtrend Length", "Number of bars to check for downtrend", "Pattern Parameters")

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
    def RequireDowntrend(self):
        return self._requireDowntrend.Value

    @RequireDowntrend.setter
    def RequireDowntrend(self, value):
        self._requireDowntrend.Value = value

    @property
    def DowntrendLength(self):
        return self._downtrendLength.Value

    @DowntrendLength.setter
    def DowntrendLength(self, value):
        self._downtrendLength.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(three_bar_reversal_up_strategy, self).OnReseted()
        self._lastThreeCandles.Clear()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(three_bar_reversal_up_strategy, self).OnStarted(time)

        # Clear candle queue
        self._lastThreeCandles.Clear()

        # Create lowest indicator for downtrend identification
        self._lowestIndicator = Lowest()
        self._lowestIndicator.Length = self.DowntrendLength

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing with the lowest indicator
        subscription.Bind(self._lowestIndicator, self.ProcessCandle).Start()

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

    def ProcessCandle(self, candle, lowestValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param lowestValue: The lowest value from indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already in position, no need to search for new patterns
        if self.Position > 0:
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

        # Check for Three-Bar Reversal Up pattern:
        # 1. First candle is bearish
        isFirstBearish = firstCandle.ClosePrice < firstCandle.OpenPrice

        # 2. Second candle is bearish with a lower low
        isSecondBearish = secondCandle.ClosePrice < secondCandle.OpenPrice
        hasSecondLowerLow = secondCandle.LowPrice < firstCandle.LowPrice

        # 3. Third candle is bullish and closes above second candle's high
        isThirdBullish = thirdCandle.ClosePrice > thirdCandle.OpenPrice
        doesThirdCloseAboveSecondHigh = thirdCandle.ClosePrice > secondCandle.HighPrice

        # 4. Check if we're in a downtrend (if required)
        isInDowntrend = not self.RequireDowntrend or self.IsInDowntrend(lowestValue)

        # Check if the pattern is complete
        if (isFirstBearish and isSecondBearish and hasSecondLowerLow and 
            isThirdBullish and doesThirdCloseAboveSecondHigh and isInDowntrend):
            # Pattern found - take long position
            patternLow = Math.Min(secondCandle.LowPrice, thirdCandle.LowPrice)
            stopLoss = patternLow * (1 - self.StopLossPercent / 100)

            self.BuyMarket(self.Volume)
            self.LogInfo("Three-Bar Reversal Up pattern detected at {0}".format(thirdCandle.OpenTime))
            self.LogInfo("First bar: O={0}, C={1}, L={2}".format(
                firstCandle.OpenPrice, firstCandle.ClosePrice, firstCandle.LowPrice))
            self.LogInfo("Second bar: O={0}, C={1}, L={2}".format(
                secondCandle.OpenPrice, secondCandle.ClosePrice, secondCandle.LowPrice))
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

    def IsInDowntrend(self, lowestValue):
        """
        Check if we're in a downtrend
        
        :param lowestValue: The lowest value from indicator.
        :return: True if in downtrend.
        """
        # If we have the lowest indicator value, check if current price is near it
        if self._lastThreeCandles.Count > 0:
            candlesList = list(self._lastThreeCandles)
            lastCandle = candlesList[-1]  # Get the last candle
            return Math.Abs(lastCandle.LowPrice - lowestValue) / lowestValue < 0.03
        
        return False

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return three_bar_reversal_up_strategy()