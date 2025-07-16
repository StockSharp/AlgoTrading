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
from StockSharp.Algo.Indicators import ParabolicSar, VolumeIndicator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class parabolic_sar_volume_strategy(Strategy):
    """
    Strategy that combines Parabolic SAR with volume confirmation.
    Enters trades when price crosses the Parabolic SAR with above-average volume.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(parabolic_sar_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._acceleration = self.Param("Acceleration", 0.02) \
            .SetRange(0.01, 0.1) \
            .SetCanOptimize(True) \
            .SetDisplay("SAR Acceleration", "Starting acceleration factor", "Indicators")

        self._maxAcceleration = self.Param("MaxAcceleration", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetCanOptimize(True) \
            .SetDisplay("SAR Max Acceleration", "Maximum acceleration factor", "Indicators")

        self._volumePeriod = self.Param("VolumePeriod", 20) \
            .SetRange(10, 50) \
            .SetCanOptimize(True) \
            .SetDisplay("Volume Period", "Period for volume moving average", "Indicators")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators will be initialized in OnStarted
        self._parabolicSar = None
        self._volumeIndicator = None
        self._volumeAverage = None

        # State variables
        self._prevSar = 0
        self._currentAvgVolume = 0
        self._prevPriceAboveSar = False

    @property
    def Acceleration(self):
        """Parabolic SAR acceleration factor."""
        return self._acceleration.Value

    @Acceleration.setter
    def Acceleration(self, value):
        self._acceleration.Value = value

    @property
    def MaxAcceleration(self):
        """Parabolic SAR maximum acceleration factor."""
        return self._maxAcceleration.Value

    @MaxAcceleration.setter
    def MaxAcceleration(self, value):
        self._maxAcceleration.Value = value

    @property
    def VolumePeriod(self):
        """Period for volume moving average."""
        return self._volumePeriod.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volumePeriod.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(parabolic_sar_volume_strategy, self).OnStarted(time)

        # Initialize indicators
        self._parabolicSar = ParabolicSar()
        self._parabolicSar.Acceleration = self.Acceleration
        self._parabolicSar.AccelerationMax = self.MaxAcceleration

        self._volumeIndicator = VolumeIndicator()

        self._volumeAverage = SimpleMovingAverage()
        self._volumeAverage.Length = self.VolumePeriod

        # Reset state variables
        self._prevSar = 0
        self._currentAvgVolume = 0
        self._prevPriceAboveSar = False

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Binding for Parabolic SAR indicator
        subscription.Bind(self._parabolicSar, self._volumeIndicator, self.ProcessIndicators).Start()

        # Setup position protection with trailing stop
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(0, UnitTypes.Absolute),
            isStopTrailing=True
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolicSar)

            volumeArea = self.CreateChartArea()
            self.DrawIndicator(volumeArea, self._volumeIndicator)
            self.DrawIndicator(volumeArea, self._volumeAverage)

            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, sarValue, volumeValue):
        self._currentAvgVolume = to_float(self._volumeAverage.Process(volumeValue, candle.ServerTime, candle.State == CandleStates.Finished))

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Wait until strategy and indicators are ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current price and volume
        currentPrice = candle.ClosePrice
        currentVolume = candle.TotalVolume
        isPriceAboveSar = currentPrice > sarValue

        # Determine if volume is above average
        isHighVolume = currentVolume > self._currentAvgVolume

        # Check for SAR crossover with volume confirmation
        # Bullish crossover: Price crosses above SAR with high volume
        if isPriceAboveSar and not self._prevPriceAboveSar and isHighVolume and self.Position <= 0:
            # Cancel existing orders before entering new position
            self.CancelActiveOrders()

            # Enter long position
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo("Long entry signal: Price {0} crossed above SAR {1} with high volume {2} > avg {3}".format(
                currentPrice, sarValue, currentVolume, self._currentAvgVolume))
        # Bearish crossover: Price crosses below SAR with high volume
        elif not isPriceAboveSar and self._prevPriceAboveSar and isHighVolume and self.Position >= 0:
            # Cancel existing orders before entering new position
            self.CancelActiveOrders()

            # Enter short position
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo("Short entry signal: Price {0} crossed below SAR {1} with high volume {2} > avg {3}".format(
                currentPrice, sarValue, currentVolume, self._currentAvgVolume))
        # Exit signals based on SAR crossover (without volume confirmation)
        elif (self.Position > 0 and not isPriceAboveSar) or (self.Position < 0 and isPriceAboveSar):
            # Close position on SAR reversal
            self.ClosePosition()

            self.LogInfo("Exit signal: SAR reversal. Price: {0}, SAR: {1}".format(currentPrice, sarValue))

        # Update previous values for next candle
        self._prevSar = sarValue
        self._prevPriceAboveSar = isPriceAboveSar

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return parabolic_sar_volume_strategy()
