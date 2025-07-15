import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Strategies import Strategy

class double_top_strategy(Strategy):
    """
    Double Top reversal strategy: looks for two similar tops with confirmation.
    This pattern often indicates a trend reversal from bullish to bearish.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(double_top_strategy, self).__init__()
        
        # Initialize internal state
        self._firstTopHigh = None
        self._secondTopHigh = None
        self._barsSinceFirstTop = 0
        self._patternConfirmed = False
        self._highestIndicator = None

        # Initialize strategy parameters
        self._distanceParam = self.Param("Distance", 5) \
            .SetDisplay("Distance between tops", "Number of bars between two tops", "Pattern Parameters")

        self._similarityPercent = self.Param("SimilarityPercent", 2.0) \
            .SetDisplay("Similarity %", "Maximum percentage difference between two tops", "Pattern Parameters")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage above top for stop-loss", "Risk Management")

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
        super(double_top_strategy, self).OnReseted()
        self._firstTopHigh = None
        self._secondTopHigh = None
        self._barsSinceFirstTop = 0
        self._patternConfirmed = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(double_top_strategy, self).OnStarted(time)

        self._firstTopHigh = None
        self._secondTopHigh = None
        self._barsSinceFirstTop = 0
        self._patternConfirmed = False

        # Create indicator to find highest values
        self._highestIndicator = Highest()
        self._highestIndicator.Length = self.Distance * 2

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing 
        subscription.Bind(self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit (manual exit)
            Unit(self.StopLossPercent, UnitTypes.Percent),  # Stop loss at defined percentage
            False  # No trailing
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

        # Process the candle with the Highest indicator
        highestValue = self._highestIndicator.Process(candle).ToDecimal()

        # If strategy is not ready yet, return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already in position, no need to search for new patterns
        if self.Position < 0:
            return

        # If we have a confirmed pattern and price falls below support
        if self._patternConfirmed and candle.ClosePrice < candle.OpenPrice:
            # Sell signal - Double Top with confirmation candle
            self.SellMarket(self.Volume)
            stopLossLevel = Math.Max(self._firstTopHigh, self._secondTopHigh) * (1 + self.StopLossPercent / 100)
            self.LogInfo("Double Top signal: Sell at {0}, Stop Loss at {1}".format(
                candle.ClosePrice, stopLossLevel))
            
            # Reset pattern detection
            self._patternConfirmed = False
            self._firstTopHigh = None
            self._secondTopHigh = None
            self._barsSinceFirstTop = 0
            return

        # Pattern detection logic
        if self._firstTopHigh is None:
            # Looking for first top
            if candle.HighPrice == highestValue:
                self._firstTopHigh = candle.HighPrice
                self._barsSinceFirstTop = 0
                self.LogInfo("Potential first top detected at price {0}".format(self._firstTopHigh))
        else:
            self._barsSinceFirstTop += 1

            # If we're at the appropriate distance, check for second top
            if (self._barsSinceFirstTop >= self.Distance and 
                self._secondTopHigh is None):
                # Check if current high is close to first top
                priceDifference = Math.Abs((candle.HighPrice - self._firstTopHigh) / self._firstTopHigh * 100)
                
                if priceDifference <= self.SimilarityPercent:
                    self._secondTopHigh = candle.HighPrice
                    self._patternConfirmed = True
                    self.LogInfo("Double Top pattern confirmed. First: {0}, Second: {1}".format(
                        self._firstTopHigh, self._secondTopHigh))

            # If too much time has passed, reset pattern search
            if (self._barsSinceFirstTop > self.Distance * 3 or 
                (self._secondTopHigh is not None and self._barsSinceFirstTop > self.Distance * 4)):
                self._firstTopHigh = None
                self._secondTopHigh = None
                self._barsSinceFirstTop = 0
                self._patternConfirmed = False

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return double_top_strategy()