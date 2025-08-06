import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class morning_star_strategy(Strategy):
    """
    Morning Star candle pattern strategy.
    The strategy looks for a Morning Star pattern - first bearish candle, second small candle (doji), third bullish candle that closes above the midpoint of the first.
    
    """
    def __init__(self):
        super(morning_star_strategy, self).__init__()
        
        # Initialize internal state
        self._firstCandle = None
        self._secondCandle = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage below the second candle's low", "Risk Management")

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
        super(morning_star_strategy, self).OnReseted()
        self._firstCandle = None
        self._secondCandle = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(morning_star_strategy, self).OnStarted(time)

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Setup trailing stop
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=True
        )
    def ProcessCandle(self, candle):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # The strategy only takes long positions
        if self.Position > 0:
            return

        # If we have no previous candle stored, store the current one and return
        if self._firstCandle is None:
            self._firstCandle = candle
            return

        # If we have one previous candle stored, store the current one as the second and return
        if self._secondCandle is None:
            self._secondCandle = candle
            return

        # We now have three candles to analyze (the first two stored and the current one)
        isMorningStar = self.CheckMorningStar(self._firstCandle, self._secondCandle, candle)

        if isMorningStar:
            # Morning Star pattern detected - enter long position
            self.BuyMarket(self.Volume)
            
            self.LogInfo("Morning Star pattern detected. Entering long position at {0}".format(candle.ClosePrice))
            
            # Set stop-loss
            stopPrice = self._secondCandle.LowPrice * (1 - self.StopLossPercent / 100)
            self.LogInfo("Setting stop-loss at {0}".format(stopPrice))

        # Shift candles (drop first, move second to first, current to second)
        self._firstCandle = self._secondCandle
        self._secondCandle = candle

        # Exit logic for existing positions
        if self.Position > 0 and candle.HighPrice > self._secondCandle.HighPrice:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit signal: Price above previous high. Closing position at {0}".format(candle.ClosePrice))

    def CheckMorningStar(self, first, second, third):
        """
        Check if three candles form a Morning Star pattern
        
        :param first: First candle
        :param second: Second candle
        :param third: Third candle
        :return: True if Morning Star pattern is detected
        """
        # Check the first candle is bearish (close lower than open)
        firstIsBearish = first.ClosePrice < first.OpenPrice
        
        # Check the third candle is bullish (close higher than open)
        thirdIsBullish = third.ClosePrice > third.OpenPrice
        
        # Calculate the body size (absolute difference between open and close)
        firstBodySize = Math.Abs(first.OpenPrice - first.ClosePrice)
        secondBodySize = Math.Abs(second.OpenPrice - second.ClosePrice)
        
        # Second candle should have a small body (doji or near-doji) - typically less than 30% of the first
        secondIsSmall = secondBodySize < (firstBodySize * 0.3)
        
        # Calculate midpoint of first candle
        firstMidpoint = (first.HighPrice + first.LowPrice) / 2
        
        # Third candle close should be above the midpoint of the first candle
        thirdClosesHighEnough = third.ClosePrice > firstMidpoint
        
        # Log pattern analysis
        self.LogInfo("Pattern analysis: First bearish={0}, Second small={1}, Third bullish={2}, Third above midpoint={3}".format(
            firstIsBearish, secondIsSmall, thirdIsBullish, thirdClosesHighEnough))
        
        # Return true if all conditions are met
        return firstIsBearish and secondIsSmall and thirdIsBullish and thirdClosesHighEnough

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return morning_star_strategy()