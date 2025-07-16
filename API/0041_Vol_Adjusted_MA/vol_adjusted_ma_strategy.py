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
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vol_adjusted_ma_strategy(Strategy):
    """
    Vol Adjusted MA strategy
    Strategy enters long when price is above MA + k*ATR, and short when price is below MA - k*ATR
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(vol_adjusted_ma_strategy, self).__init__()
        
        # Initialize internal state
        self._prevAdjustedUpperBand = 0
        self._prevAdjustedLowerBand = 0

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._atrPeriod = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")

        self._atrMultiplier = self.Param("ATRMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to adjust MA bands", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def ATRPeriod(self):
        return self._atrPeriod.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def ATRMultiplier(self):
        return self._atrMultiplier.Value

    @ATRMultiplier.setter
    def ATRMultiplier(self, value):
        self._atrMultiplier.Value = value

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
        super(vol_adjusted_ma_strategy, self).OnReseted()
        self._prevAdjustedUpperBand = 0
        self._prevAdjustedLowerBand = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(vol_adjusted_ma_strategy, self).OnStarted(time)

        self._prevAdjustedUpperBand = 0
        self._prevAdjustedLowerBand = 0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        atr = AverageTrueRange()
        atr.Length = self.ATRPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, atr, self.ProcessCandle).Start()

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
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, atrValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param atrValue: The ATR value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate adjusted bands
        adjustedUpperBand = maValue + self.ATRMultiplier * atrValue
        adjustedLowerBand = maValue - self.ATRMultiplier * atrValue

        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, ATR: {2}".format(candle.ClosePrice, maValue, atrValue))
        self.LogInfo("Upper Band: {0}, Lower Band: {1}".format(adjustedUpperBand, adjustedLowerBand))

        # Store for next comparison if needed
        self._prevAdjustedUpperBand = adjustedUpperBand
        self._prevAdjustedLowerBand = adjustedLowerBand

        # Trading logic:
        # Long: Price > MA + k*ATR
        if candle.ClosePrice > adjustedUpperBand and self.Position <= 0:
            self.LogInfo("Buy Signal: Price ({0}) > Upper Band ({1})".format(candle.ClosePrice, adjustedUpperBand))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Price < MA - k*ATR
        elif candle.ClosePrice < adjustedLowerBand and self.Position >= 0:
            self.LogInfo("Sell Signal: Price ({0}) < Lower Band ({1})".format(candle.ClosePrice, adjustedLowerBand))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit Long: Price < MA
        elif candle.ClosePrice < maValue and self.Position > 0:
            self.LogInfo("Exit Long: Price ({0}) < MA ({1})".format(candle.ClosePrice, maValue))
            self.SellMarket(Math.Abs(self.Position))
        # Exit Short: Price > MA
        elif candle.ClosePrice > maValue and self.Position < 0:
            self.LogInfo("Exit Short: Price ({0}) > MA ({1})".format(candle.ClosePrice, maValue))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vol_adjusted_ma_strategy()