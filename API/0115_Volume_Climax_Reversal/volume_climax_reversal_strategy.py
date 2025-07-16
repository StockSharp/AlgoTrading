import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class volume_climax_reversal_strategy(Strategy):
    """
    Volume Climax Reversal strategy.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(volume_climax_reversal_strategy, self).__init__()
        # Initializes a new instance of the <see cref="VolumeClimaxReversalStrategy"/>.

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Candles")

        self._volumePeriod = self.Param("VolumePeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Period", "Period for volume average calculation", "Volume") \
            .SetCanOptimize(True)

        self._volumeMultiplier = self.Param("VolumeMultiplier", 3.0) \
            .SetRange(1.5, 5.0) \
            .SetDisplay("Volume Multiplier", "Volume threshold as multiplier of average volume", "Volume") \
            .SetCanOptimize(True)

        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Moving Average") \
            .SetCanOptimize(True)

        self._atrMultiplier = self.Param("ATRMultiplier", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to calculate stop loss", "Risk") \
            .SetCanOptimize(True)

        self._ma = None
        self._volumeAverage = None
        self._atr = None

    @property
    def CandleType(self):
        """Candle type and timeframe."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def VolumePeriod(self):
        """Period for volume average calculation."""
        return self._volumePeriod.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volumePeriod.Value = value

    @property
    def VolumeMultiplier(self):
        """Volume multiplier for signal detection."""
        return self._volumeMultiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volumeMultiplier.Value = value

    @property
    def MAPeriod(self):
        """Moving average period for trend determination."""
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def ATRMultiplier(self):
        """ATR multiplier for stop loss."""
        return self._atrMultiplier.Value

    @ATRMultiplier.setter
    def ATRMultiplier(self, value):
        self._atrMultiplier.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(volume_climax_reversal_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MAPeriod
        self._volumeAverage = SimpleMovingAverage()
        self._volumeAverage.Length = self.VolumePeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.VolumePeriod

        # Create and subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Use BindEx to process both price and volume
        subscription.Bind(self._ma, self._atr, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, atrValue):
        """
        Process candle logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process indicators
        volumeAverageValue = to_float(self._volumeAverage.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current candle information
        currentVolume = candle.TotalVolume
        isBullishCandle = candle.ClosePrice > candle.OpenPrice
        isBearishCandle = candle.ClosePrice < candle.OpenPrice

        # Check for volume climax (volume spike)
        isVolumeClimaxDetected = currentVolume > volumeAverageValue * self.VolumeMultiplier

        if isVolumeClimaxDetected:
            self.LogInfo("Volume climax detected: {0} > {1} * {2}".format(currentVolume, volumeAverageValue, self.VolumeMultiplier))

            # Bullish reversal: High volume + bearish candle + price below MA
            if isBearishCandle and candle.ClosePrice < maValue and self.Position <= 0:
                self.LogInfo("Bullish reversal signal detected")
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Bearish reversal: High volume + bullish candle + price above MA
            elif isBullishCandle and candle.ClosePrice > maValue and self.Position >= 0:
                self.LogInfo("Bearish reversal signal detected")
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit logic - Price crosses MA
        if self.Position > 0 and candle.ClosePrice < maValue:
            self.LogInfo("Exit long: Price moved below MA")
            self.ClosePosition()
        elif self.Position < 0 and candle.ClosePrice > maValue:
            self.LogInfo("Exit short: Price moved above MA")
            self.ClosePosition()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_climax_reversal_strategy()
