import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_volume_strategy(Strategy):
    """
    Implementation of strategy #157 - Keltner Channels + Volume.
    Buy when price breaks above upper Keltner Channel with above average volume.
    Sell when price breaks below lower Keltner Channel with above average volume.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(keltner_volume_strategy, self).__init__()

        # For volume tracking
        self._averageVolume = 0
        self._volumeCounter = 0

        # Last price flags for detecting crossovers
        self._lastPrice = 0

        # Initialize strategy parameters
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "EMA period for center line", "Keltner Parameters")

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for channel width", "Keltner Parameters")

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to define channel width", "Keltner Parameters")

        self._volumeAvgPeriod = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters")

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def EmaPeriod(self):
        """EMA period for Keltner Channels."""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for Keltner Channels."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def Multiplier(self):
        """Multiplier for Keltner Channels (how many ATRs from EMA)."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def VolumeAvgPeriod(self):
        """Volume average period."""
        return self._volumeAvgPeriod.Value

    @VolumeAvgPeriod.setter
    def VolumeAvgPeriod(self, value):
        self._volumeAvgPeriod.Value = value

    @property
    def StopLoss(self):
        """Stop-loss value."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Candle type used for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(keltner_volume_strategy, self).OnReseted()
        self._averageVolume = 0
        self._volumeCounter = 0
        self._lastPrice = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(keltner_volume_strategy, self).OnStarted(time)

        # Create indicators
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Custom Keltner Channels calculation will be done in the processing method
        # as we need both EMA and ATR values together

        # Reset volume tracking
        self._averageVolume = 0
        self._volumeCounter = 0
        self._lastPrice = 0

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            # EMA and bands will be drawn in the indicator handler
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(None, self.StopLoss)

    def ProcessCandle(self, candle, emaValue, atrValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Keltner Channels
        upperBand = emaValue + (self.Multiplier * atrValue)
        lowerBand = emaValue - (self.Multiplier * atrValue)

        # Update average volume calculation
        currentVolume = candle.TotalVolume

        if self._volumeCounter < self.VolumeAvgPeriod:
            self._volumeCounter += 1
            self._averageVolume = ((self._averageVolume * (self._volumeCounter - 1)) + currentVolume) / self._volumeCounter
        else:
            self._averageVolume = (self._averageVolume * (self.VolumeAvgPeriod - 1) + currentVolume) / self.VolumeAvgPeriod

        # Check if volume is above average
        isVolumeAboveAverage = currentVolume > self._averageVolume

        self.LogInfo("Candle: {0}, Close: {1}, EMA: {2}, " +
                     "Upper Band: {3}, Lower Band: {4}, " +
                     "Volume: {5}, Avg Volume: {6}".format(
            candle.OpenTime, candle.ClosePrice, emaValue,
            upperBand, lowerBand, currentVolume, self._averageVolume))

        # Check crossovers - only valid after we have a last price
        currentPrice = candle.ClosePrice

        # Skip if this is the first processed candle
        if self._lastPrice != 0:
            # Trading rules
            # Check Upper Band breakout with volume confirmation
            if (currentPrice > upperBand and self._lastPrice <= upperBand and
                    isVolumeAboveAverage and self.Position <= 0):
                # Buy signal - price breaks above upper band with high volume
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Buy signal: Price breaks above upper band with high volume. Volume: {0}".format(volume))
            # Check Lower Band breakdown with volume confirmation
            elif (currentPrice < lowerBand and self._lastPrice >= lowerBand and
                  isVolumeAboveAverage and self.Position >= 0):
                # Sell signal - price breaks below lower band with high volume
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Sell signal: Price breaks below lower band with high volume. Volume: {0}".format(volume))
            # Exit conditions
            elif currentPrice < emaValue and self.Position > 0:
                # Exit long when price moves below EMA (middle line)
                self.SellMarket(self.Position)

                self.LogInfo("Exit long position: Price moved below EMA. Position: {0}".format(self.Position))
            elif currentPrice > emaValue and self.Position < 0:
                # Exit short when price moves above EMA (middle line)
                self.BuyMarket(Math.Abs(self.Position))

                self.LogInfo("Exit short position: Price moved above EMA. Position: {0}".format(self.Position))

        # Update last price
        self._lastPrice = currentPrice

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_volume_strategy()
