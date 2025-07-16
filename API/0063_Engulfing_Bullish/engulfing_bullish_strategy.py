import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class engulfing_bullish_strategy(Strategy):
    """
    Strategy based on Bullish Engulfing candlestick pattern.
    This pattern occurs when a bullish (white) candlestick completely engulfs
    the previous bearish (black) candlestick, signaling a potential bullish reversal.
    
    """
    def __init__(self):
        super(engulfing_bullish_strategy, self).__init__()
        
        # Initialize internal state
        self._previousCandle = None
        self._consecutiveDownBars = 0

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage below pattern's low for stop-loss", "Risk Management")

        self._requireDowntrend = self.Param("RequireDowntrend", True) \
            .SetDisplay("Require Downtrend", "Whether to require a prior downtrend", "Pattern Parameters")

        self._downtrendBars = self.Param("DowntrendBars", 3) \
            .SetDisplay("Downtrend Bars", "Number of consecutive bearish bars for downtrend", "Pattern Parameters")

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
    def DowntrendBars(self):
        return self._downtrendBars.Value

    @DowntrendBars.setter
    def DowntrendBars(self, value):
        self._downtrendBars.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(engulfing_bullish_strategy, self).OnReseted()
        self._previousCandle = None
        self._consecutiveDownBars = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(engulfing_bullish_strategy, self).OnStarted(time)

        self._previousCandle = None
        self._consecutiveDownBars = 0

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
        if self.Position > 0:
            # Store current candle for next iteration
            self._previousCandle = candle
            return

        # Track consecutive down bars for downtrend identification
        if candle.ClosePrice < candle.OpenPrice:
            self._consecutiveDownBars += 1
        else:
            self._consecutiveDownBars = 0

        # If we have a previous candle, check for engulfing pattern
        if self._previousCandle is not None:
            # Check for bullish engulfing pattern:
            # 1. Previous candle is bearish (close < open)
            # 2. Current candle is bullish (close > open)
            # 3. Current candle's body completely engulfs previous candle's body
            
            isPreviousBearish = self._previousCandle.ClosePrice < self._previousCandle.OpenPrice
            isCurrentBullish = candle.ClosePrice > candle.OpenPrice
            
            isPreviousEngulfed = (candle.ClosePrice > self._previousCandle.OpenPrice and 
                                 candle.OpenPrice < self._previousCandle.ClosePrice)
            
            isDowntrendPresent = (not self.RequireDowntrend or 
                                 self._consecutiveDownBars >= self.DowntrendBars)
            
            if (isPreviousBearish and isCurrentBullish and 
                isPreviousEngulfed and isDowntrendPresent):
                # Bullish engulfing pattern detected
                patternLow = Math.Min(candle.LowPrice, self._previousCandle.LowPrice)
                
                # Buy signal
                self.BuyMarket(self.Volume)
                self.LogInfo("Bullish Engulfing pattern detected at {0}: Open={1}, Close={2}".format(
                    candle.OpenTime, candle.OpenPrice, candle.ClosePrice))
                self.LogInfo("Previous candle: Open={0}, Close={1}".format(
                    self._previousCandle.OpenPrice, self._previousCandle.ClosePrice))
                self.LogInfo("Stop Loss set at {0}".format(
                    patternLow * (1 - self.StopLossPercent / 100)))
                
                # Reset consecutive down bars
                self._consecutiveDownBars = 0

        # Store current candle for next iteration
        self._previousCandle = candle

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return engulfing_bullish_strategy()