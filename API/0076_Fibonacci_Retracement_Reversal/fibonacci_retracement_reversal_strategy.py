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

class fibonacci_retracement_reversal_strategy(Strategy):
    """
    Fibonacci Retracement Reversal strategy.
    The strategy identifies significant swings in price and looks for reversals at key Fibonacci retracement levels.
    
    """
    def __init__(self):
        super(fibonacci_retracement_reversal_strategy, self).__init__()
        
        # Fibonacci retracement levels
        self._fibLevels = [0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0]
        
        # Initialize strategy parameters
        self._swingLookbackPeriodParam = self.Param("SwingLookbackPeriod", 20) \
            .SetDisplay("Swing Lookback Period", "Number of candles to look back for swing detection", "Indicators")
        
        self._fibLevelBufferParam = self.Param("FibLevelBuffer", 0.5) \
            .SetDisplay("Fib Level Buffer %", "Buffer percentage around Fibonacci levels", "Indicators")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Variables to store swing high and low
        self._swingHigh = None
        self._swingLow = None
        self._trendIsUp = False
        
        # Store recent candles for swing detection
        self._recentCandles = []

    @property
    def SwingLookbackPeriod(self):
        return self._swingLookbackPeriodParam.Value

    @SwingLookbackPeriod.setter
    def SwingLookbackPeriod(self, value):
        self._swingLookbackPeriodParam.Value = value

    @property
    def FibLevelBuffer(self):
        return self._fibLevelBufferParam.Value

    @FibLevelBuffer.setter
    def FibLevelBuffer(self, value):
        self._fibLevelBufferParam.Value = value

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercentParam.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercentParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(fibonacci_retracement_reversal_strategy, self).OnStarted(time)

        # Reset values
        self._swingHigh = None
        self._swingLow = None
        self._trendIsUp = False
        self._recentCandles = []

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Add current candle to the list and maintain list size
        self._recentCandles.append(candle)
        while len(self._recentCandles) > self.SwingLookbackPeriod:
            self._recentCandles.pop(0)

        # We need a sufficient number of candles to identify swings
        if len(self._recentCandles) < 3:
            return

        # Update swing points if necessary
        self.UpdateSwingPoints()

        # Check for potential entry signals
        self.CheckForEntrySignals(candle)

    def UpdateSwingPoints(self):
        """
        Update swing high and low points based on recent candles.
        """
        # Get high and low from recent candles
        if len(self._recentCandles) < 3:
            return

        candles = self._recentCandles
        middleIndex = len(candles) // 2

        # Check if we have a new swing high or low
        swingHighFound = False
        swingLowFound = False

        # Check for swing high - middle candle has the highest high
        if len(candles) >= 3:
            middleHigh = 0.0
            middleLow = float('inf')

            # Find the highest high and lowest low in the middle third of candles
            start_idx = max(0, middleIndex - 1)
            end_idx = min(len(candles) - 1, middleIndex + 1)
            
            for i in range(start_idx, end_idx + 1):
                if candles[i].HighPrice > middleHigh:
                    middleHigh = candles[i].HighPrice
                
                if candles[i].LowPrice < middleLow:
                    middleLow = candles[i].LowPrice

            # Check if this middle section forms a swing high/low
            isHigher = True
            isLower = True

            # Check candles before middle
            for i in range(0, max(0, middleIndex - 1)):
                if candles[i].HighPrice >= middleHigh:
                    isHigher = False
                
                if candles[i].LowPrice <= middleLow:
                    isLower = False

            # Check candles after middle
            for i in range(min(len(candles) - 1, middleIndex + 2), len(candles)):
                if candles[i].HighPrice >= middleHigh:
                    isHigher = False
                
                if candles[i].LowPrice <= middleLow:
                    isLower = False

            # If we found a swing high or low
            if isHigher and (self._swingHigh is None or middleHigh > self._swingHigh):
                self._swingHigh = middleHigh
                swingHighFound = True
                self._trendIsUp = False  # After a swing high, the trend is down
                self.LogInfo("New swing high found: {0}", self._swingHigh)

            if isLower and (self._swingLow is None or middleLow < self._swingLow):
                self._swingLow = middleLow
                swingLowFound = True
                self._trendIsUp = True  # After a swing low, the trend is up
                self.LogInfo("New swing low found: {0}", self._swingLow)

        # If we found both a new swing high and low, use the most recent one
        if swingHighFound and swingLowFound and self._swingHigh is not None and self._swingLow is not None:
            lastCandle = candles[-1]
            self._trendIsUp = lastCandle.ClosePrice > ((self._swingHigh + self._swingLow) / 2)

    def CheckForEntrySignals(self, candle):
        """
        Check for entry signals based on Fibonacci retracement levels.
        """
        # Need valid swing points to calculate Fibonacci levels
        if (self._swingHigh is None or self._swingLow is None or 
            self._swingHigh <= self._swingLow):
            return

        swingHigh = self._swingHigh
        swingLow = self._swingLow
        currentPrice = candle.ClosePrice
        isBullish = candle.ClosePrice > candle.OpenPrice
        isBearish = candle.ClosePrice < candle.OpenPrice

        # Calculate Fibonacci retracement levels
        range_val = swingHigh - swingLow
        
        # Check if price is near a Fibonacci retracement level
        for fibLevel in self._fibLevels:
            # Calculate price at this Fibonacci level
            if self._trendIsUp:
                # For uptrend, calculate retracement levels from swing low
                levelPrice = swingLow + (range_val * fibLevel)
            else:
                # For downtrend, calculate retracement levels from swing high
                levelPrice = swingHigh - (range_val * fibLevel)

            # Calculate buffer around Fibonacci level
            buffer = range_val * (self.FibLevelBuffer / 100)
            
            # Check if price is within buffer of the Fibonacci level
            if abs(currentPrice - levelPrice) <= buffer:
                # We're at a Fibonacci level - check if we should enter a position
                self.LogInfo("Price {0} is near Fibonacci {1}% level {2} (buffer: {3})", 
                           currentPrice, fibLevel*100, levelPrice, buffer)

                # Look for long signal at 61.8% or 78.6% retracement in uptrend with bullish candle
                if (self._trendIsUp and (abs(fibLevel - 0.618) < 0.001 or abs(fibLevel - 0.786) < 0.001) and 
                    isBullish and self.Position <= 0):
                    # Enter long position
                    self.CancelActiveOrders()
                    self.BuyMarket(self.Volume + abs(self.Position))
                    self.LogInfo("Long entry at {0} near {1}% retracement level", 
                               currentPrice, fibLevel*100)
                    break

                # Look for short signal at 61.8% or 78.6% retracement in downtrend with bearish candle
                elif (not self._trendIsUp and (abs(fibLevel - 0.618) < 0.001 or abs(fibLevel - 0.786) < 0.001) and 
                      isBearish and self.Position >= 0):
                    # Enter short position
                    self.CancelActiveOrders()
                    self.SellMarket(self.Volume + abs(self.Position))
                    self.LogInfo("Short entry at {0} near {1}% retracement level", 
                               currentPrice, fibLevel*100)
                    break

        # Exit logic - exit when price reaches the central Fibonacci level (50%)
        if self._trendIsUp:
            centralLevel = swingLow + (range_val * 0.5)
        else:
            centralLevel = swingHigh - (range_val * 0.5)
        
        if self.Position > 0 and currentPrice >= centralLevel:
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit at {0}, reached 50% level {1}", currentPrice, centralLevel)

        elif self.Position < 0 and currentPrice <= centralLevel:
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit at {0}, reached 50% level {1}", currentPrice, centralLevel)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return fibonacci_retracement_reversal_strategy()