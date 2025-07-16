import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class bollinger_volume_strategy(Strategy):
    """\
    Strategy that uses Bollinger Bands breakouts with volume confirmation.
    Enters positions when price breaks above/below Bollinger Bands with increased volume.
    """

    def __init__(self):
        super(bollinger_volume_strategy, self).__init__()

        # Strategy parameters
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")

        self._bollingerDeviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")

        self._volumePeriod = self.Param("VolumePeriod", 20) \
            .SetDisplay("Volume Period", "Period for volume averaging", "Indicators")

        self._volumeMultiplier = self.Param("VolumeMultiplier", 1.5) \
            .SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm breakouts", "Indicators")

        self._stopLossAtr = self.Param("StopLossAtr", 2.0) \
            .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management")

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period of the ATR for stop loss calculation", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._avgVolume = 0.0

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period."""
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands standard deviation multiplier."""
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def VolumePeriod(self):
        """Volume averaging period."""
        return self._volumePeriod.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volumePeriod.Value = value

    @property
    def VolumeMultiplier(self):
        """Volume multiplier for confirmation."""
        return self._volumeMultiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volumeMultiplier.Value = value

    @property
    def StopLossAtr(self):
        """Stop loss in ATR multiples."""
        return self._stopLossAtr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stopLossAtr.Value = value

    @property
    def AtrPeriod(self):
        """ATR period."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(bollinger_volume_strategy, self).OnStarted(time)

        # Initialize variables
        self._avgVolume = 0.0

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        volumeAvg = SimpleMovingAverage()
        volumeAvg.Length = self.VolumePeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(volumeAvg, bollinger, atr, self.ProcessIndicators).Start()

        # Setup position protection with ATR-based stop loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossAtr, UnitTypes.Absolute)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, volumeAvgValue, bollingerValue, atrValue):
        """Process Bollinger Bands and ATR indicator values."""
        if volumeAvgValue.IsFinal:
            self._avgVolume = to_float(volumeAvgValue)

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading() or self._avgVolume <= 0:
            return

        bb = bollingerValue  # BollingerBandsValue
        middleBand = bb.MovingAverage
        upperBand = bb.UpBand
        lowerBand = bb.LowBand

        atr = to_float(atrValue)

        # Check volume confirmation
        isVolumeHighEnough = candle.TotalVolume > self._avgVolume * self.VolumeMultiplier

        if isVolumeHighEnough:
            # Long entry: price breaks above upper Bollinger Band with increased volume
            if candle.ClosePrice > upperBand and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
            # Short entry: price breaks below lower Bollinger Band with increased volume
            elif candle.ClosePrice < lowerBand and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

        # Exit logic - price returns to middle band
        if self.Position > 0 and candle.ClosePrice < middleBand:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > middleBand:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_volume_strategy()
