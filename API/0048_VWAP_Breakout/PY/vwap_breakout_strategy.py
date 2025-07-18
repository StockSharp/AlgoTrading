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

class vwap_breakout_strategy(Strategy):
    """
    VWAP Breakout strategy
    Long entry: Price breaks above VWAP
    Short entry: Price breaks below VWAP
    Exit when price crosses back through VWAP
    
    """
    def __init__(self):
        super(vwap_breakout_strategy, self).__init__()
        
        # Initialize internal state
        self._previousClosePrice = 0
        self._previousVWAP = 0
        self._isFirstCandle = True

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

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
        super(vwap_breakout_strategy, self).OnReseted()
        self._previousClosePrice = 0
        self._previousVWAP = 0
        self._isFirstCandle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vwap_breakout_strategy, self).OnStarted(time)

        self._previousClosePrice = 0
        self._previousVWAP = 0
        self._isFirstCandle = True

        # Create VWAP indicator
        vwap = VolumeWeightedMovingAverage()

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(vwap, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, vwapValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param vwapValue: The VWAP indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract VWAP value from indicator result
        vwapPrice = float(vwapValue)
        
        # Skip the first candle, just initialize values
        if self._isFirstCandle:
            self._previousClosePrice = float(candle.ClosePrice)
            self._previousVWAP = vwapPrice
            self._isFirstCandle = False
            return
        
        # Check for VWAP breakouts
        breakoutUp = (self._previousClosePrice <= self._previousVWAP and 
                     candle.ClosePrice > vwapPrice)
        breakoutDown = (self._previousClosePrice >= self._previousVWAP and 
                       candle.ClosePrice < vwapPrice)
        
        # Log current values
        self.LogInfo("Candle Close: {0}, VWAP: {1}".format(candle.ClosePrice, vwapPrice))
        self.LogInfo("Previous Close: {0}, Previous VWAP: {1}".format(
            self._previousClosePrice, self._previousVWAP))
        self.LogInfo("Breakout Up: {0}, Breakout Down: {1}".format(breakoutUp, breakoutDown))

        # Trading logic:
        # Long: Price breaks above VWAP
        if breakoutUp and self.Position <= 0:
            self.LogInfo("Buy Signal: Price ({0}) breaking above VWAP ({1})".format(
                candle.ClosePrice, vwapPrice))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Price breaks below VWAP
        elif breakoutDown and self.Position >= 0:
            self.LogInfo("Sell Signal: Price ({0}) breaking below VWAP ({1})".format(
                candle.ClosePrice, vwapPrice))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Price crosses back through VWAP
        if self.Position > 0 and candle.ClosePrice < vwapPrice:
            self.LogInfo("Exit Long: Price ({0}) < VWAP ({1})".format(candle.ClosePrice, vwapPrice))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > vwapPrice:
            self.LogInfo("Exit Short: Price ({0}) > VWAP ({1})".format(candle.ClosePrice, vwapPrice))
            self.BuyMarket(Math.Abs(self.Position))

        # Store current values for next comparison
        self._previousClosePrice = float(candle.ClosePrice)
        self._previousVWAP = vwapPrice

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_breakout_strategy()