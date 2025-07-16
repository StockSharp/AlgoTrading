import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class shooting_star_strategy(Strategy):
    """
    Strategy based on Shooting Star candlestick pattern.
    Shooting Star is a bearish reversal pattern that forms after an advance
    and is characterized by a small body with a long upper shadow.
    
    """
    def __init__(self):
        super(shooting_star_strategy, self).__init__()
        
        # Initialize internal state
        self._shootingStarHigh = None
        self._shootingStarLow = None
        self._patternDetected = False

        # Initialize strategy parameters
        self._shadowToBodyRatio = self.Param("ShadowToBodyRatio", 2.0) \
            .SetDisplay("Shadow/Body Ratio", "Minimum ratio of upper shadow to body length", "Pattern Parameters")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage above shooting star's high for stop-loss", "Risk Management")

        self._confirmationRequired = self.Param("ConfirmationRequired", True) \
            .SetDisplay("Confirmation Required", "Whether to wait for a bearish confirmation candle", "Pattern Parameters")

    @property
    def ShadowToBodyRatio(self):
        return self._shadowToBodyRatio.Value

    @ShadowToBodyRatio.setter
    def ShadowToBodyRatio(self, value):
        self._shadowToBodyRatio.Value = value

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
    def ConfirmationRequired(self):
        return self._confirmationRequired.Value

    @ConfirmationRequired.setter
    def ConfirmationRequired(self, value):
        self._confirmationRequired.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(shooting_star_strategy, self).OnReseted()
        self._shootingStarHigh = None
        self._shootingStarLow = None
        self._patternDetected = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(shooting_star_strategy, self).OnStarted(time)

        self._shootingStarHigh = None
        self._shootingStarLow = None
        self._patternDetected = False

        # Create highest indicator for trend identification
        highest = Highest()
        highest.Length = 10

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing with the highest indicator
        subscription.Bind(highest, self.ProcessCandle).Start()

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
            return
        
        # If we have detected a shooting star and are waiting for confirmation
        if self._patternDetected:
            # If confirmation required and we get a bearish candle
            if self.ConfirmationRequired and candle.ClosePrice < candle.OpenPrice:
                # Sell signal - Shooting Star with confirmation candle
                self.SellMarket(self.Volume)
                stopLossLevel = self._shootingStarHigh * (1 + self.StopLossPercent / 100)
                self.LogInfo("Shooting Star pattern confirmed: Sell at {0}, Stop Loss at {1}".format(
                    candle.ClosePrice, stopLossLevel))
                
                # Reset pattern detection
                self._patternDetected = False
                self._shootingStarHigh = None
                self._shootingStarLow = None
            # If no confirmation required or we don't want to wait anymore
            elif not self.ConfirmationRequired:
                # Sell signal - Shooting Star without waiting for confirmation
                self.SellMarket(self.Volume)
                stopLossLevel = self._shootingStarHigh * (1 + self.StopLossPercent / 100)
                self.LogInfo("Shooting Star pattern detected: Sell at {0}, Stop Loss at {1}".format(
                    candle.ClosePrice, stopLossLevel))
                
                # Reset pattern detection
                self._patternDetected = False
                self._shootingStarHigh = None
                self._shootingStarLow = None
            # If we've seen a shooting star but today's candle doesn't confirm, reset
            elif candle.ClosePrice > candle.OpenPrice:
                self._patternDetected = False
                self._shootingStarHigh = None
                self._shootingStarLow = None
        
        # Pattern detection logic
        else:
            # Identify shooting star pattern
            # 1. Candle should appear after an advance (price near recent highs)
            # 2. Upper shadow should be at least X times longer than the body
            # 3. Candle should have small or no lower shadow
            
            # Check if we're near recent highs
            isNearHighs = Math.Abs(candle.HighPrice - highestValue) / highestValue < 0.03
            
            # Check if high is above previous high (market is advancing)
            isAdvance = candle.HighPrice > highestValue
            
            # Calculate candle body and shadows
            bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice)
            upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice)
            lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice
            
            # Check for bearish shooting star pattern
            isBearish = candle.ClosePrice < candle.OpenPrice
            hasLongUpperShadow = upperShadow > bodyLength * self.ShadowToBodyRatio
            hasSmallLowerShadow = lowerShadow < bodyLength * 0.3
            
            # Identify shooting star
            if (isNearHighs or isAdvance) and hasLongUpperShadow and hasSmallLowerShadow:
                self._shootingStarHigh = candle.HighPrice
                self._shootingStarLow = candle.LowPrice
                self._patternDetected = True
                
                self.LogInfo("Potential shooting star detected at {0}: high={1}, body ratio={2:F2}".format(
                    candle.OpenTime, candle.HighPrice, upperShadow/bodyLength if bodyLength > 0 else 0))
                
                # If confirmation not required, sell immediately
                if not self.ConfirmationRequired:
                    self.SellMarket(self.Volume)
                    stopLossLevel = self._shootingStarHigh * (1 + self.StopLossPercent / 100)
                    self.LogInfo("Shooting Star pattern detected: Sell at {0}, Stop Loss at {1}".format(
                        candle.ClosePrice, stopLossLevel))
                    
                    # Reset pattern detection
                    self._patternDetected = False
                    self._shootingStarHigh = None
                    self._shootingStarLow = None

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return shooting_star_strategy()