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
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Indicators import Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vcp_strategy(Strategy):
    """
    Volume Contraction Pattern (VCP) strategy.
    The strategy looks for narrowing price ranges and breakouts after contraction.
    Long entry: Range contraction followed by a break above previous high
    Short entry: Range contraction followed by a break below previous low
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(vcp_strategy, self).__init__()
        
        # Initialize internal state
        self._prevCandleRange = 0

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating breakout levels", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

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
        super(vcp_strategy, self).OnReseted()
        self._prevCandleRange = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vcp_strategy, self).OnStarted(time)

        self._prevCandleRange = 0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        highest = Highest()
        highest.Length = self.LookbackPeriod
        
        lowest = Lowest()
        lowest.Length = self.LookbackPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, ma, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, highestValue, lowestValue, maValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param highestValue: The highest price value.
        :param lowestValue: The lowest price value.
        :param maValue: The Moving Average value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate current candle range
        currentCandleRange = candle.HighPrice - candle.LowPrice
        
        # If first candle, just store the range and return
        if self._prevCandleRange == 0:
            self._prevCandleRange = currentCandleRange
            return

        # Check for range contraction (current range smaller than previous range)
        isContraction = currentCandleRange < self._prevCandleRange
        
        # Log current values
        self.LogInfo("Candle Range: {0}, Previous Range: {1}, Contraction: {2}".format(
            currentCandleRange, self._prevCandleRange, isContraction))
        self.LogInfo("Highest: {0}, Lowest: {1}, MA: {2}".format(highestValue, lowestValue, maValue))

        # Trading logic:
        if isContraction:
            # Long: Contraction and breakout above highest high
            if candle.ClosePrice > highestValue and self.Position <= 0:
                self.LogInfo("Buy Signal: Contraction and Price ({0}) > Highest ({1})".format(
                    candle.ClosePrice, highestValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short: Contraction and breakout below lowest low
            elif candle.ClosePrice < lowestValue and self.Position >= 0:
                self.LogInfo("Sell Signal: Contraction and Price ({0}) < Lowest ({1})".format(
                    candle.ClosePrice, lowestValue))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses MA
        if self.Position > 0 and candle.ClosePrice < maValue:
            self.LogInfo("Exit Long: Price ({0}) < MA ({1})".format(candle.ClosePrice, maValue))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > maValue:
            self.LogInfo("Exit Short: Price ({0}) > MA ({1})".format(candle.ClosePrice, maValue))
            self.BuyMarket(Math.Abs(self.Position))

        # Store current range for next comparison
        self._prevCandleRange = currentCandleRange

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vcp_strategy()