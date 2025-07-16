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
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class volume_surge_strategy(Strategy):
    """
    Volume Surge strategy
    Long entry: Volume exceeds average volume by k times and price is above MA
    Short entry: Volume exceeds average volume by k times and price is below MA
    Exit when volume falls below average
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(volume_surge_strategy, self).__init__()
        
        # Initialize internal state
        self._volumeMA = None

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._volumeAvgPeriod = self.Param("VolumeAvgPeriod", 20) \
            .SetDisplay("Volume Average Period", "Period for Average Volume calculation", "Strategy Parameters")

        self._volumeSurgeMultiplier = self.Param("VolumeSurgeMultiplier", 2.0) \
            .SetDisplay("Volume Surge Multiplier", "Minimum volume increase multiplier to generate signal", "Strategy Parameters")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def VolumeAvgPeriod(self):
        return self._volumeAvgPeriod.Value

    @VolumeAvgPeriod.setter
    def VolumeAvgPeriod(self, value):
        self._volumeAvgPeriod.Value = value

    @property
    def VolumeSurgeMultiplier(self):
        return self._volumeSurgeMultiplier.Value

    @VolumeSurgeMultiplier.setter
    def VolumeSurgeMultiplier(self, value):
        self._volumeSurgeMultiplier.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(volume_surge_strategy, self).OnStarted(time)

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        self._volumeMA = SimpleMovingAverage()
        self._volumeMA.Length = self.VolumeAvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        
        # Regular price MA binding for signals and visualization
        subscription.Bind(ma, self.ProcessCandle).Start()

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

    def ProcessCandle(self, candle, maValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        """
        volumeMAValue = to_float(self._volumeMA.Process(candle.TotalVolume, candle.ServerTime, 
                                              candle.State == CandleStates.Finished))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate volume surge ratio
        volumeSurgeRatio = candle.TotalVolume / volumeMAValue if volumeMAValue != 0 else 0
        isVolumeSurge = volumeSurgeRatio >= self.VolumeSurgeMultiplier
        
        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, Volume: {2}".format(
            candle.ClosePrice, maValue, candle.TotalVolume))
        self.LogInfo("Volume MA: {0}, Volume Surge Ratio: {1:P2}".format(
            volumeMAValue, volumeSurgeRatio - 1))
        self.LogInfo("Is Volume Surge: {0}, Threshold: {1}".format(
            isVolumeSurge, self.VolumeSurgeMultiplier))

        # Trading logic:
        # Check for volume surge
        if isVolumeSurge:
            # Long: Volume surge and price above MA
            if candle.ClosePrice > maValue and self.Position <= 0:
                self.LogInfo("Buy Signal: Volume Surge ({0:P2}) and Price ({1}) > MA ({2})".format(
                    volumeSurgeRatio - 1, candle.ClosePrice, maValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short: Volume surge and price below MA
            elif candle.ClosePrice < maValue and self.Position >= 0:
                self.LogInfo("Sell Signal: Volume Surge ({0:P2}) and Price ({1}) < MA ({2})".format(
                    volumeSurgeRatio - 1, candle.ClosePrice, maValue))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Volume falls below average
        if candle.TotalVolume < volumeMAValue:
            if self.Position > 0:
                self.LogInfo("Exit Long: Volume ({0}) < Average Volume ({1})".format(
                    candle.TotalVolume, volumeMAValue))
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0:
                self.LogInfo("Exit Short: Volume ({0}) < Average Volume ({1})".format(
                    candle.TotalVolume, volumeMAValue))
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_surge_strategy()