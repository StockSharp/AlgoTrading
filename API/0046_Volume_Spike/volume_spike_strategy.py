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
from datatype_extensions import *

class volume_spike_strategy(Strategy):
    """
    Volume Spike strategy
    Long entry: Volume increases 2x above previous candle and price is above MA
    Short entry: Volume increases 2x above previous candle and price is below MA
    Exit when volume falls below average volume
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(volume_spike_strategy, self).__init__()
        
        # Initialize internal state
        self._previousVolume = 0

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._volAvgPeriod = self.Param("VolAvgPeriod", 20) \
            .SetDisplay("Volume Average Period", "Period for Average Volume calculation", "Strategy Parameters")

        self._volumeSpikeMultiplier = self.Param("VolumeSpikeMultiplier", 2.0) \
            .SetDisplay("Volume Spike Multiplier", "Minimum volume increase multiplier to generate signal", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def VolAvgPeriod(self):
        return self._volAvgPeriod.Value

    @VolAvgPeriod.setter
    def VolAvgPeriod(self, value):
        self._volAvgPeriod.Value = value

    @property
    def VolumeSpikeMultiplier(self):
        return self._volumeSpikeMultiplier.Value

    @VolumeSpikeMultiplier.setter
    def VolumeSpikeMultiplier(self, value):
        self._volumeSpikeMultiplier.Value = value

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
        super(volume_spike_strategy, self).OnReseted()
        self._previousVolume = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(volume_spike_strategy, self).OnStarted(time)

        self._previousVolume = 0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        volumeMA = SimpleMovingAverage()
        volumeMA.Length = self.VolAvgPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, volumeMA, self.ProcessCandle).Start()

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

    def ProcessCandle(self, candle, maValue, volumeMAValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param volumeMAValue: The Volume Moving Average value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip first candle, just store volume
        if self._previousVolume == 0:
            self._previousVolume = candle.TotalVolume
            return

        # Calculate volume change
        volumeChange = candle.TotalVolume / self._previousVolume if self._previousVolume != 0 else 1
        
        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, Volume: {2}".format(
            candle.ClosePrice, maValue, candle.TotalVolume))
        self.LogInfo("Previous Volume: {0}, Volume Change: {1:P2}, Average Volume: {2}".format(
            self._previousVolume, volumeChange - 1, volumeMAValue))

        # Trading logic:
        # Check for volume spike
        if volumeChange >= self.VolumeSpikeMultiplier:
            self.LogInfo("Volume Spike detected: {0:P2}".format(volumeChange - 1))

            # Long: Volume spike and price above MA
            if candle.ClosePrice > maValue and self.Position <= 0:
                self.LogInfo("Buy Signal: Volume Spike ({0:P2}) and Price ({1}) > MA ({2})".format(
                    volumeChange - 1, candle.ClosePrice, maValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short: Volume spike and price below MA
            elif candle.ClosePrice < maValue and self.Position >= 0:
                self.LogInfo("Sell Signal: Volume Spike ({0:P2}) and Price ({1}) < MA ({2})".format(
                    volumeChange - 1, candle.ClosePrice, maValue))
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

        # Store current volume for next comparison
        self._previousVolume = candle.TotalVolume

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_spike_strategy()