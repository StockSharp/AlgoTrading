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
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_trailing_strategy(Strategy):
    """
    Strategy that uses ATR (Average True Range) for trailing stop management.
    It enters positions using a simple moving average and manages exits with a dynamic
    trailing stop calculated as a multiple of ATR.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(atr_trailing_strategy, self).__init__()
        
        # Initialize internal state
        self._entryPrice = 0
        self._trailingStopLevel = 0

        # Initialize strategy parameters
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")

        self._atrMultiplier = self.Param("AtrMultiplier", 3.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop calculation", "Risk Management")

        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation for entry", "Entry Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

    @property
    def AtrPeriod(self):
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(atr_trailing_strategy, self).OnReseted()
        self._entryPrice = 0
        self._trailingStopLevel = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(atr_trailing_strategy, self).OnStarted(time)

        # Reset state variables
        self._entryPrice = 0
        self._trailingStopLevel = 0

        # Create indicators
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        
        sma = SimpleMovingAverage()
        sma.Length = self.MAPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, sma, self.ProcessCandle).Start()

        # Configure chart if GUI is available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atrValue, smaValue):
        """
        Process candle and manage positions with ATR-based trailing stops
        
        :param candle: The candle message.
        :param atrValue: The ATR value.
        :param smaValue: The SMA value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate trailing stop distance based on ATR
        trailingStopDistance = atrValue * self.AtrMultiplier

        if self.Position == 0:
            # No position - check for entry signals
            if candle.ClosePrice > smaValue:
                # Price above MA - buy (long)
                self.BuyMarket(self.Volume)
                
                # Record entry price
                self._entryPrice = candle.ClosePrice
                
                # Set initial trailing stop
                self._trailingStopLevel = self._entryPrice - trailingStopDistance
            elif candle.ClosePrice < smaValue:
                # Price below MA - sell (short)
                self.SellMarket(self.Volume)
                
                # Record entry price
                self._entryPrice = candle.ClosePrice
                
                # Set initial trailing stop
                self._trailingStopLevel = self._entryPrice + trailingStopDistance
        elif self.Position > 0:
            # Long position - update and check trailing stop
            
            # Calculate potential new trailing stop level
            newTrailingStopLevel = candle.ClosePrice - trailingStopDistance
            
            # Only move the trailing stop up, never down (for long positions)
            if newTrailingStopLevel > self._trailingStopLevel:
                self._trailingStopLevel = newTrailingStopLevel
            
            # Check if price hit the trailing stop
            if candle.LowPrice <= self._trailingStopLevel:
                # Trailing stop hit - exit long
                self.SellMarket(self.Position)
        elif self.Position < 0:
            # Short position - update and check trailing stop
            
            # Calculate potential new trailing stop level
            newTrailingStopLevel = candle.ClosePrice + trailingStopDistance
            
            # Only move the trailing stop down, never up (for short positions)
            if newTrailingStopLevel < self._trailingStopLevel or self._trailingStopLevel == 0:
                self._trailingStopLevel = newTrailingStopLevel
            
            # Check if price hit the trailing stop
            if candle.HighPrice >= self._trailingStopLevel:
                # Trailing stop hit - exit short
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return atr_trailing_strategy()