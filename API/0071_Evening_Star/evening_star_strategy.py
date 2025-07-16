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
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class evening_star_strategy(Strategy):
    """
    Evening Star candle pattern strategy.
    The strategy looks for an Evening Star pattern - first bullish candle, second small candle (doji), third bearish candle that closes below the midpoint of the first.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(evening_star_strategy, self).__init__()
        
        # Initialize internal state
        self._firstCandle = None
        self._secondCandle = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage above the second candle's high", "Risk Management")

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
        super(evening_star_strategy, self).OnReseted()
        self._firstCandle = None
        self._secondCandle = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(evening_star_strategy, self).OnStarted(time)

        # Reset candle storage
        self._firstCandle = None
        self._secondCandle = None

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
            Unit(0, UnitTypes.Absolute),  # no take profit, rely on exit signal
            Unit(self.StopLossPercent, UnitTypes.Percent),  # stop loss
            True  # trailing stop
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

        # The strategy only takes short positions
        if self.Position < 0:
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
        isEveningStar = self.CheckEveningStar(self._firstCandle, self._secondCandle, candle)

        if isEveningStar:
            # Evening Star pattern detected - enter short position
            self.SellMarket(self.Volume)
            
            self.LogInfo("Evening Star pattern detected. Entering short position at {0}".format(candle.ClosePrice))
            
            # Set stop-loss
            stopPrice = self._secondCandle.HighPrice * (1 + self.StopLossPercent / 100)
            self.LogInfo("Setting stop-loss at {0}".format(stopPrice))

        # Shift candles (drop first, move second to first, current to second)
        self._firstCandle = self._secondCandle
        self._secondCandle = candle

        # Exit logic for existing positions
        if self.Position < 0 and candle.LowPrice < self._secondCandle.LowPrice:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit signal: Price below previous low. Closing position at {0}".format(candle.ClosePrice))

    def CheckEveningStar(self, first, second, third):
        """
        Check if three candles form an Evening Star pattern
        
        :param first: First candle
        :param second: Second candle
        :param third: Third candle
        :return: True if Evening Star pattern is detected
        """
        # Check the first candle is bullish (close higher than open)
        firstIsBullish = first.ClosePrice > first.OpenPrice
        
        # Check the third candle is bearish (close lower than open)
        thirdIsBearish = third.ClosePrice < third.OpenPrice
        
        # Calculate the body size (absolute difference between open and close)
        firstBodySize = Math.Abs(first.OpenPrice - first.ClosePrice)
        secondBodySize = Math.Abs(second.OpenPrice - second.ClosePrice)
        
        # Second candle should have a small body (doji or near-doji) - typically less than 30% of the first
        secondIsSmall = secondBodySize < (firstBodySize * 0.3)
        
        # Calculate midpoint of first candle
        firstMidpoint = (first.HighPrice + first.LowPrice) / 2
        
        # Third candle close should be below the midpoint of the first candle
        thirdClosesLowEnough = third.ClosePrice < firstMidpoint
        
        # Log pattern analysis
        self.LogInfo("Pattern analysis: First bullish={0}, Second small={1}, Third bearish={2}, Third below midpoint={3}".format(
            firstIsBullish, secondIsSmall, thirdIsBearish, thirdClosesLowEnough))
        
        # Return true if all conditions are met
        return firstIsBullish and secondIsSmall and thirdIsBearish and thirdClosesLowEnough

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return evening_star_strategy()