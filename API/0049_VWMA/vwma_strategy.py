import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwma_strategy(Strategy):
    """
    Volume Weighted Moving Average (VWMA) Strategy
    Long entry: Price crosses above VWMA
    Short entry: Price crosses below VWMA
    Exit: Price crosses back through VWMA
    
    """
    def __init__(self):
        super(vwma_strategy, self).__init__()
        
        # Initialize internal state
        self._previousClosePrice = 0
        self._previousVWMA = 0
        self._isFirstCandle = True

        # Initialize strategy parameters
        self._vwmaPeriod = self.Param("VWMAPeriod", 14) \
            .SetDisplay("VWMA Period", "Period for Volume Weighted Moving Average calculation", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def VWMAPeriod(self):
        return self._vwmaPeriod.Value

    @VWMAPeriod.setter
    def VWMAPeriod(self, value):
        self._vwmaPeriod.Value = value

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
        super(vwma_strategy, self).OnReseted()
        self._previousClosePrice = 0
        self._previousVWMA = 0
        self._isFirstCandle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vwma_strategy, self).OnStarted(time)

        self._previousClosePrice = 0
        self._previousVWMA = 0
        self._isFirstCandle = True

        # Create VWMA indicator
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self.VWMAPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(vwma, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, vwmaValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param vwmaValue: The VWMA indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract VWMA value from indicator result
        vwmaPrice = to_float(vwmaValue)
        
        # Skip the first candle, just initialize values
        if self._isFirstCandle:
            self._previousClosePrice = float(candle.ClosePrice)
            self._previousVWMA = vwmaPrice
            self._isFirstCandle = False
            return
        
        # Check for VWMA crossovers
        crossoverUp = (self._previousClosePrice <= self._previousVWMA and 
                      candle.ClosePrice > vwmaPrice)
        crossoverDown = (self._previousClosePrice >= self._previousVWMA and 
                        candle.ClosePrice < vwmaPrice)
        
        # Log current values
        self.LogInfo("Candle Close: {0}, VWMA: {1}".format(candle.ClosePrice, vwmaPrice))
        self.LogInfo("Previous Close: {0}, Previous VWMA: {1}".format(
            self._previousClosePrice, self._previousVWMA))
        self.LogInfo("Crossover Up: {0}, Crossover Down: {1}".format(crossoverUp, crossoverDown))

        # Trading logic:
        # Long: Price crosses above VWMA
        if crossoverUp and self.Position <= 0:
            self.LogInfo("Buy Signal: Price crossing above VWMA ({0} > {1})".format(
                candle.ClosePrice, vwmaPrice))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Price crosses below VWMA
        elif crossoverDown and self.Position >= 0:
            self.LogInfo("Sell Signal: Price crossing below VWMA ({0} < {1})".format(
                candle.ClosePrice, vwmaPrice))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses back through VWMA
        if self.Position > 0 and crossoverDown:
            self.LogInfo("Exit Long: Price crossing below VWMA ({0} < {1})".format(
                candle.ClosePrice, vwmaPrice))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and crossoverUp:
            self.LogInfo("Exit Short: Price crossing above VWMA ({0} > {1})".format(
                candle.ClosePrice, vwmaPrice))
            self.BuyMarket(Math.Abs(self.Position))

        # Store current values for next comparison
        self._previousClosePrice = float(candle.ClosePrice)
        self._previousVWMA = vwmaPrice

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwma_strategy()