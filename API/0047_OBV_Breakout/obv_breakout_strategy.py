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
from StockSharp.Algo.Indicators import OnBalanceVolume
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Indicators import Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class obv_breakout_strategy(Strategy):
    """
    On-Balance Volume (OBV) Breakout strategy
    Long entry: OBV breaks above its highest level over N periods
    Short entry: OBV breaks below its lowest level over N periods
    Exit when OBV crosses below/above its moving average
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(obv_breakout_strategy, self).__init__()
        
        # Initialize internal state
        self._highestOBV = None
        self._lowestOBV = None
        self._isFirstCandle = True

        # Initialize strategy parameters
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating OBV highest/lowest levels", "Strategy Parameters")

        self._obvMAPeriod = self.Param("OBVMAPeriod", 20) \
            .SetDisplay("OBV MA Period", "Period for OBV Moving Average calculation", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def OBVMAPeriod(self):
        return self._obvMAPeriod.Value

    @OBVMAPeriod.setter
    def OBVMAPeriod(self, value):
        self._obvMAPeriod.Value = value

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
        super(obv_breakout_strategy, self).OnReseted()
        self._highestOBV = None
        self._lowestOBV = None
        self._isFirstCandle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(obv_breakout_strategy, self).OnStarted(time)

        self._highestOBV = None
        self._lowestOBV = None
        self._isFirstCandle = True

        # Create indicators
        obv = OnBalanceVolume()
        
        # Create a custom moving average for OBV
        obvMA = SimpleMovingAverage()
        obvMA.Length = self.OBVMAPeriod
        
        # Create highest and lowest indicators for OBV values
        highest = Highest()
        highest.Length = self.LookbackPeriod
        
        lowest = Lowest()
        lowest.Length = self.LookbackPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        
        # We need to process OBV first, then calculate MA and highest/lowest from it
        subscription.BindEx(obv, self.ProcessOBVCandle).Start()

        # Store indicators for later use
        self._obvMA = obvMA
        self._highest = highest
        self._lowest = lowest

        # Configure protection
        self.StartProtection(
            Unit(3, UnitTypes.Percent),  # Take profit
            Unit(2, UnitTypes.Percent)   # Stop loss
        )

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, obv)
            self.DrawOwnTrades(area)

    def ProcessOBVCandle(self, candle, obvValue):
        """
        Process OBV values and execute trading logic
        
        :param candle: The candle message.
        :param obvValue: The OBV indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return
        
        # Process the OBV value through other indicators
        obvMAValue = to_float(self._obvMA.Process(obvValue))
        obvVal = to_float(obvValue)
        
        if not self._isFirstCandle:
            # Calculate highest and lowest OBV values
            if self._highest.IsFormed and self._highestOBV is not None:
                highestValue = to_float(self._highest.Process(obvValue))
            else:
                highestValue = Math.Max(self._highestOBV or obvVal, obvVal) if self._highestOBV is not None else obvVal
            
            if self._lowest.IsFormed and self._lowestOBV is not None:
                lowestValue = to_float(self._lowest.Process(obvValue))
            else:
                lowestValue = Math.Min(self._lowestOBV or obvVal, obvVal) if self._lowestOBV is not None else obvVal
            
            self.ProcessCandle(candle, obvVal, obvMAValue, highestValue, lowestValue)
            
            if not self._highest.IsFormed:
                self._highestOBV = highestValue
            if not self._lowest.IsFormed:
                self._lowestOBV = lowestValue
        else:
            self._highestOBV = obvVal
            self._lowestOBV = obvVal
            self._isFirstCandle = False

    def ProcessCandle(self, candle, obvValue, obvMAValue, highestValue, lowestValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param obvValue: The OBV value.
        :param obvMAValue: The OBV Moving Average value.
        :param highestValue: The highest OBV value.
        :param lowestValue: The lowest OBV value.
        """
        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Log current values
        self.LogInfo("Candle Close: {0}, OBV: {1}, OBV MA: {2}".format(
            candle.ClosePrice, obvValue, obvMAValue))
        self.LogInfo("Highest OBV: {0}, Lowest OBV: {1}".format(highestValue, lowestValue))

        # Trading logic:
        # Long: OBV breaks above highest level
        if (self._highestOBV is not None and obvValue > highestValue and 
            obvValue > self._highestOBV and self.Position <= 0):
            self.LogInfo("Buy Signal: OBV ({0}) breaking above highest level ({1})".format(
                obvValue, highestValue))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: OBV breaks below lowest level
        elif (self._lowestOBV is not None and obvValue < lowestValue and 
              obvValue < self._lowestOBV and self.Position >= 0):
            self.LogInfo("Sell Signal: OBV ({0}) breaking below lowest level ({1})".format(
                obvValue, lowestValue))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: OBV crosses below/above its moving average
        if self.Position > 0 and obvValue < obvMAValue:
            self.LogInfo("Exit Long: OBV ({0}) < OBV MA ({1})".format(obvValue, obvMAValue))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and obvValue > obvMAValue:
            self.LogInfo("Exit Short: OBV ({0}) > OBV MA ({1})".format(obvValue, obvMAValue))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return obv_breakout_strategy()