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

class engulfing_bearish_strategy(Strategy):
    """
    Strategy based on Bearish Engulfing candlestick pattern.
    This pattern occurs when a bearish (black) candlestick completely engulfs
    the previous bullish (white) candlestick, signaling a potential bearish reversal.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(engulfing_bearish_strategy, self).__init__()
        
        # Initialize internal state
        self._previousCandle = None
        self._consecutiveUpBars = 0

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage above pattern's high for stop-loss", "Risk Management")

        self._requireUptrend = self.Param("RequireUptrend", True) \
            .SetDisplay("Require Uptrend", "Whether to require a prior uptrend", "Pattern Parameters")

        self._uptrendBars = self.Param("UptrendBars", 3) \
            .SetDisplay("Uptrend Bars", "Number of consecutive bullish bars for uptrend", "Pattern Parameters")

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
    def UptrendBars(self):
        return self._uptrendBars.Value

    @UptrendBars.setter
    def UptrendBars(self, value):
        self._uptrendBars.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(engulfing_bearish_strategy, self).OnReseted()
        self._previousCandle = None
        self._consecutiveUpBars = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(engulfing_bearish_strategy, self).OnStarted(time)

        self._previousCandle = None
        self._consecutiveUpBars = 0

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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already in position, no need to search for new patterns
        if self.Position < 0:
            # Store current candle for next iteration
            self._previousCandle = candle
            return

        # Track consecutive up bars for uptrend identification
        if candle.ClosePrice > candle.OpenPrice:
            self._consecutiveUpBars += 1
        else:
            self._consecutiveUpBars = 0

        # If we have a previous candle, check for engulfing pattern
        if self._previousCandle is not None:
            # Check for bearish engulfing pattern:
            # 1. Previous candle is bullish (close > open)
            # 2. Current candle is bearish (close < open)
            # 3. Current candle's body completely engulfs previous candle's body
            
            isPreviousBullish = self._previousCandle.ClosePrice > self._previousCandle.OpenPrice
            isCurrentBearish = candle.ClosePrice < candle.OpenPrice
            
            isPreviousEngulfed = (candle.OpenPrice > self._previousCandle.ClosePrice and 
                                 candle.ClosePrice < self._previousCandle.OpenPrice)
            
            isUptrendPresent = (not self.RequireUptrend or 
                               self._consecutiveUpBars >= self.UptrendBars)
            
            if (isPreviousBullish and isCurrentBearish and 
                isPreviousEngulfed and isUptrendPresent):
                # Bearish engulfing pattern detected
                patternHigh = Math.Max(candle.HighPrice, self._previousCandle.HighPrice)
                
                # Sell signal
                self.SellMarket(self.Volume)
                self.LogInfo("Bearish Engulfing pattern detected at {0}: Open={1}, Close={2}".format(
                    candle.OpenTime, candle.OpenPrice, candle.ClosePrice))
                self.LogInfo("Previous candle: Open={0}, Close={1}".format(
                    self._previousCandle.OpenPrice, self._previousCandle.ClosePrice))
                self.LogInfo("Stop Loss set at {0}".format(
                    patternHigh * (1 + self.StopLossPercent / 100)))
                
                # Reset consecutive up bars
                self._consecutiveUpBars = 0

        # Store current candle for next iteration
        self._previousCandle = candle

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return engulfing_bearish_strategy()