import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("System.Collections")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from System.Collections.Generic import Queue
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
import System.Linq

class cumulative_delta_breakout_strategy(Strategy):
    """
    Cumulative Delta Breakout strategy
    Long entry: Cumulative Delta breaks above its N-period highest
    Short entry: Cumulative Delta breaks below its N-period lowest
    Exit: Cumulative Delta changes sign (crosses zero)
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(cumulative_delta_breakout_strategy, self).__init__()
        
        # Initialize internal state
        self._cumulativeDelta = 0
        self._highestDelta = None
        self._lowestDelta = None
        self._barCount = 0
        self._deltaWindow = Queue[float]()

        # Initialize strategy parameters
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating highest/lowest delta", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

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
        super(cumulative_delta_breakout_strategy, self).OnReseted()
        self._cumulativeDelta = 0
        self._highestDelta = None
        self._lowestDelta = None
        self._barCount = 0
        self._deltaWindow.Clear()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(cumulative_delta_breakout_strategy, self).OnStarted(time)

        self._cumulativeDelta = 0
        self._highestDelta = None
        self._lowestDelta = None
        self._barCount = 0
        self._deltaWindow.Clear()

        # Create subscription for both candles and ticks
        candleSubscription = self.SubscribeCandles(self.CandleType)
        
        # Bind candle processing
        candleSubscription.Bind(self.ProcessCandle).Start()
        
        # Subscribe to ticks to compute delta
        tickSubscription = self.SubscribeTicks()
        tickSubscription.Bind(self.ProcessTrade).Start()
        
        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candleSubscription)
            self.DrawOwnTrades(area)

    def ProcessTrade(self, trade):
        """
        Process trade ticks to calculate delta
        
        :param trade: The trade message.
        """
        # Calculate delta: positive for buy trades, negative for sell trades
        delta = trade.Volume if trade.OriginSide == Sides.Buy else -trade.Volume
        
        # Add to cumulative delta
        self._cumulativeDelta += delta

    def ProcessCandle(self, candle):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Increment bar counter
        self._barCount += 1

        # Update rolling window
        self._deltaWindow.Enqueue(self._cumulativeDelta)
        if self._deltaWindow.Count > self.LookbackPeriod:
            self._deltaWindow.Dequeue()

        if self._deltaWindow.Count < self.LookbackPeriod:
            # Calculate highest and lowest manually for partial window
            deltaList = list(self._deltaWindow)
            self._highestDelta = max(deltaList) if deltaList else self._cumulativeDelta
            self._lowestDelta = min(deltaList) if deltaList else self._cumulativeDelta
            return

        # Calculate highest and lowest from full window
        deltaList = list(self._deltaWindow)
        self._highestDelta = max(deltaList)
        self._lowestDelta = min(deltaList)

        # Log current values
        self.LogInfo("Candle Close: {0}, Cumulative Delta: {1}".format(
            candle.ClosePrice, self._cumulativeDelta))
        self.LogInfo("Highest Delta: {0}, Lowest Delta: {1}".format(
            self._highestDelta, self._lowestDelta))

        # Trading logic:
        # Long: Cumulative Delta breaks above highest
        if (self._highestDelta is not None and 
            self._cumulativeDelta > self._highestDelta and self.Position <= 0):
            self.LogInfo("Buy Signal: Cumulative Delta ({0}) breaking above highest ({1})".format(
                self._cumulativeDelta, self._highestDelta))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Cumulative Delta breaks below lowest
        elif (self._lowestDelta is not None and 
              self._cumulativeDelta < self._lowestDelta and self.Position >= 0):
            self.LogInfo("Sell Signal: Cumulative Delta ({0}) breaking below lowest ({1})".format(
                self._cumulativeDelta, self._lowestDelta))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Cumulative Delta crosses zero
        if self.Position > 0 and self._cumulativeDelta < 0:
            self.LogInfo("Exit Long: Cumulative Delta ({0}) < 0".format(self._cumulativeDelta))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._cumulativeDelta > 0:
            self.LogInfo("Exit Short: Cumulative Delta ({0}) > 0".format(self._cumulativeDelta))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cumulative_delta_breakout_strategy()