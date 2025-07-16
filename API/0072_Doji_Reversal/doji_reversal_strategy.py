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

class doji_reversal_strategy(Strategy):
    """
    Doji Reversal strategy.
    The strategy looks for doji candlestick patterns after a trend and takes a reversal position.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(doji_reversal_strategy, self).__init__()
        
        # Initialize internal state
        self._previousCandle = None
        self._previousPreviousCandle = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._dojiThreshold = self.Param("DojiThreshold", 0.1) \
            .SetDisplay("Doji Threshold", "Maximum body size as percentage of candle range to consider it a doji", "Indicators")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def DojiThreshold(self):
        return self._dojiThreshold.Value

    @DojiThreshold.setter
    def DojiThreshold(self, value):
        self._dojiThreshold.Value = value

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
        super(doji_reversal_strategy, self).OnReseted()
        self._previousCandle = None
        self._previousPreviousCandle = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(doji_reversal_strategy, self).OnStarted(time)

        # Reset candle storage
        self._previousCandle = None
        self._previousPreviousCandle = None

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
        
        # Start protection with dynamic stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
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

        # We need 3 candles to make a decision
        if self._previousCandle is None:
            self._previousCandle = candle
            return

        if self._previousPreviousCandle is None:
            self._previousPreviousCandle = self._previousCandle
            self._previousCandle = candle
            return

        # Check if current candle is a doji
        isDoji = self.IsDojiCandle(candle)
        
        if isDoji:
            # Check for downtrend before the doji (previous candle lower than the one before it)
            isDowntrend = self._previousCandle.ClosePrice < self._previousPreviousCandle.ClosePrice
            
            # Check for uptrend before the doji (previous candle higher than the one before it)
            isUptrend = self._previousCandle.ClosePrice > self._previousPreviousCandle.ClosePrice
            
            self.LogInfo("Doji detected. Downtrend before: {0}, Uptrend before: {1}".format(isDowntrend, isUptrend))
            
            # If we have a doji after a downtrend and no long position yet
            if isDowntrend and self.Position <= 0:
                # Cancel any existing orders
                self.CancelActiveOrders()
                
                # Enter long position
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                
                self.LogInfo("Buying at {0} after doji in downtrend".format(candle.ClosePrice))
            # If we have a doji after an uptrend and no short position yet
            elif isUptrend and self.Position >= 0:
                # Cancel any existing orders
                self.CancelActiveOrders()
                
                # Enter short position
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                
                self.LogInfo("Selling at {0} after doji in uptrend".format(candle.ClosePrice))

        # Exit logic - exiting when the price moves beyond the doji's high/low
        if self.Position > 0 and candle.HighPrice > self._previousCandle.HighPrice:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position at {0} (price above doji high)".format(candle.ClosePrice))
        elif self.Position < 0 and candle.LowPrice < self._previousCandle.LowPrice:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position at {0} (price below doji low)".format(candle.ClosePrice))

        # Update candles for next iteration
        self._previousPreviousCandle = self._previousCandle
        self._previousCandle = candle

    def IsDojiCandle(self, candle):
        """
        Check if a candle is a doji
        
        :param candle: The candle to check
        :return: True if the candle is a doji
        """
        # Calculate the body size (absolute difference between open and close)
        bodySize = Math.Abs(candle.OpenPrice - candle.ClosePrice)
        
        # Calculate the total range of the candle
        totalRange = candle.HighPrice - candle.LowPrice
        
        # Avoid division by zero
        if totalRange == 0:
            return False
        
        # Calculate the body as a percentage of the total range
        bodySizePercentage = bodySize / totalRange
        
        # It's a doji if the body size is smaller than the threshold
        isDoji = bodySizePercentage < self.DojiThreshold
        
        self.LogInfo("Candle analysis: Body size: {0}, Total range: {1}, Body %: {2:P2}, Is Doji: {3}".format(
            bodySize, totalRange, bodySizePercentage, isDoji))
        
        return isDoji

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return doji_reversal_strategy()