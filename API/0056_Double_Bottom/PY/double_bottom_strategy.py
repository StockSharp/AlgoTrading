import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class double_bottom_strategy(Strategy):
    """
    Double Bottom reversal strategy: looks for two similar bottoms with confirmation.
    This pattern often indicates a trend reversal from bearish to bullish.
    
    """
    def __init__(self):
        super(double_bottom_strategy, self).__init__()
        
        # Initialize internal state
        self._firstBottomLow = None
        self._secondBottomLow = None
        self._barsSinceFirstBottom = 0
        self._patternConfirmed = False
        self._lowestIndicator = None

        # Initialize strategy parameters
        self._distanceParam = self.Param("Distance", 5) \
            .SetDisplay("Distance between bottoms", "Number of bars between two bottoms", "Pattern Parameters")

        self._similarityPercent = self.Param("SimilarityPercent", 2.0) \
            .SetDisplay("Similarity %", "Maximum percentage difference between two bottoms", "Pattern Parameters")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage below bottom for stop-loss", "Risk Management")

    @property
    def Distance(self):
        return self._distanceParam.Value

    @Distance.setter
    def Distance(self, value):
        self._distanceParam.Value = value

    @property
    def SimilarityPercent(self):
        return self._similarityPercent.Value

    @SimilarityPercent.setter
    def SimilarityPercent(self, value):
        self._similarityPercent.Value = value

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

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(double_bottom_strategy, self).OnReseted()
        self._firstBottomLow = None
        self._secondBottomLow = None
        self._barsSinceFirstBottom = 0
        self._patternConfirmed = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(double_bottom_strategy, self).OnStarted(time)

        self._firstBottomLow = None
        self._secondBottomLow = None
        self._barsSinceFirstBottom = 0
        self._patternConfirmed = False

        # Create indicator to find lowest values
        self._lowestIndicator = Lowest()
        self._lowestIndicator.Length = self.Distance * 2

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing 
        subscription.Bind(self.ProcessCandle).Start()

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

    def ProcessCandle(self, candle):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process the candle with the Lowest indicator
        lowestValue = float(process_candle(self._lowestIndicator, candle))

        # If strategy is not ready yet, return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already in position, no need to search for new patterns
        if self.Position > 0:
            return

        # If we have a confirmed pattern and price rises above resistance
        if self._patternConfirmed and candle.ClosePrice > candle.OpenPrice:
            # Buy signal - Double Bottom with confirmation candle
            self.BuyMarket(self.Volume)
            stopLossLevel = Math.Min(self._firstBottomLow, self._secondBottomLow) * (1 - self.StopLossPercent / 100)
            self.LogInfo("Double Bottom signal: Buy at {0}, Stop Loss at {1}".format(
                candle.ClosePrice, stopLossLevel))
            
            # Reset pattern detection
            self._patternConfirmed = False
            self._firstBottomLow = None
            self._secondBottomLow = None
            self._barsSinceFirstBottom = 0
            return

        # Pattern detection logic
        if self._firstBottomLow is None:
            # Looking for first bottom
            if candle.LowPrice == lowestValue:
                self._firstBottomLow = float(candle.LowPrice)
                self._barsSinceFirstBottom = 0
                self.LogInfo("Potential first bottom detected at price {0}".format(self._firstBottomLow))
        else:
            self._barsSinceFirstBottom += 1

            # If we're at the appropriate distance, check for second bottom
            if (self._barsSinceFirstBottom >= self.Distance and 
                self._secondBottomLow is None):
                # Check if current low is close to first bottom
                priceDifference = float(Math.Abs((candle.LowPrice - self._firstBottomLow) / self._firstBottomLow * 100))
                
                if priceDifference <= self.SimilarityPercent:
                    self._secondBottomLow = float(candle.LowPrice)
                    self._patternConfirmed = True
                    self.LogInfo("Double Bottom pattern confirmed. First: {0}, Second: {1}".format(
                        self._firstBottomLow, self._secondBottomLow))

            # If too much time has passed, reset pattern search
            if (self._barsSinceFirstBottom > self.Distance * 3 or 
                (self._secondBottomLow is not None and self._barsSinceFirstBottom > self.Distance * 4)):
                self._firstBottomLow = None
                self._secondBottomLow = None
                self._barsSinceFirstBottom = 0
                self._patternConfirmed = False

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return double_bottom_strategy()