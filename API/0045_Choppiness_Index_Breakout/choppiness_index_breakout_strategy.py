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
from StockSharp.Algo.Indicators import ChoppinessIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class choppiness_index_breakout_strategy(Strategy):
    """
    Choppiness Index Breakout strategy
    Enters trades when market transitions from choppy to trending state
    Long entry: Choppiness Index falls below 38.2 and price is above MA
    Short entry: Choppiness Index falls below 38.2 and price is below MA
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(choppiness_index_breakout_strategy, self).__init__()
        
        # Initialize internal state
        self._prevChoppiness = 100  # Initialize to high value

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._choppinessPeriod = self.Param("ChoppinessPeriod", 14) \
            .SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Strategy Parameters")

        self._choppinessThreshold = self.Param("ChoppinessThreshold", 38.2) \
            .SetDisplay("Choppiness Threshold", "Threshold below which market is considered trending", "Strategy Parameters")

        self._highChoppinessThreshold = self.Param("HighChoppinessThreshold", 61.8) \
            .SetDisplay("High Choppiness Threshold", "Threshold above which to exit positions", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def ChoppinessPeriod(self):
        return self._choppinessPeriod.Value

    @ChoppinessPeriod.setter
    def ChoppinessPeriod(self, value):
        self._choppinessPeriod.Value = value

    @property
    def ChoppinessThreshold(self):
        return self._choppinessThreshold.Value

    @ChoppinessThreshold.setter
    def ChoppinessThreshold(self, value):
        self._choppinessThreshold.Value = value

    @property
    def HighChoppinessThreshold(self):
        return self._highChoppinessThreshold.Value

    @HighChoppinessThreshold.setter
    def HighChoppinessThreshold(self, value):
        self._highChoppinessThreshold.Value = value

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
        super(choppiness_index_breakout_strategy, self).OnReseted()
        self._prevChoppiness = 100

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(choppiness_index_breakout_strategy, self).OnStarted(time)

        self._prevChoppiness = 100  # Initialize to high value

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        choppinessIndex = ChoppinessIndex()
        choppinessIndex.Length = self.ChoppinessPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, choppinessIndex, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            Unit(3, UnitTypes.Percent),  # Take profit
            Unit(2, UnitTypes.Percent)   # Stop loss
        )

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, choppinessIndex)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, choppinessValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param choppinessValue: The Choppiness Index value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, Choppiness: {2}".format(
            candle.ClosePrice, maValue, choppinessValue))
        self.LogInfo("Previous Choppiness: {0}, Threshold: {1}".format(
            self._prevChoppiness, self.ChoppinessThreshold))

        # Check for transition from choppy to trending (falling below threshold)
        transitionToTrending = (self._prevChoppiness >= self.ChoppinessThreshold and 
                               choppinessValue < self.ChoppinessThreshold)
        
        # Trading logic:
        if transitionToTrending:
            self.LogInfo("Market transitioning to trending state: {0} < {1}".format(
                choppinessValue, self.ChoppinessThreshold))
            
            # Long: Low choppiness and price above MA
            if candle.ClosePrice > maValue and self.Position <= 0:
                self.LogInfo("Buy Signal: Low choppiness ({0}) and Price ({1}) > MA ({2})".format(
                    choppinessValue, candle.ClosePrice, maValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short: Low choppiness and price below MA
            elif candle.ClosePrice < maValue and self.Position >= 0:
                self.LogInfo("Sell Signal: Low choppiness ({0}) and Price ({1}) < MA ({2})".format(
                    choppinessValue, candle.ClosePrice, maValue))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Choppiness rises above high threshold (market becoming choppy again)
        if choppinessValue > self.HighChoppinessThreshold:
            self.LogInfo("Market becoming choppy: {0} > {1}".format(
                choppinessValue, self.HighChoppinessThreshold))
            
            if self.Position > 0:
                self.LogInfo("Exit Long: High choppiness ({0})".format(choppinessValue))
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0:
                self.LogInfo("Exit Short: High choppiness ({0})".format(choppinessValue))
                self.BuyMarket(Math.Abs(self.Position))

        # Store current choppiness for next comparison
        self._prevChoppiness = choppinessValue

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return choppiness_index_breakout_strategy()