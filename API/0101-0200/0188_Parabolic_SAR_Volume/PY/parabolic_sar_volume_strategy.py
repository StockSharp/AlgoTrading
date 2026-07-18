import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, VolumeIndicator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_volume_strategy(Strategy):
    """
    Strategy that combines Parabolic SAR with volume confirmation.
    """
    def __init__(self):
        super(parabolic_sar_volume_strategy, self).__init__()

        self._acceleration = self.Param("Acceleration", 0.02) \
            .SetRange(0.01, 0.1) \
            .SetDisplay("SAR Acceleration", "Starting acceleration factor", "Indicators")

        self._maxAcceleration = self.Param("MaxAcceleration", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("SAR Max Acceleration", "Maximum acceleration factor", "Indicators")

        self._volumePeriod = self.Param("VolumePeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Volume Period", "Period for volume moving average", "Indicators")

        self._cooldownBars = self.Param("CooldownBars", 30) \
            .SetRange(1, 100) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._parabolicSar = None
        self._volumeIndicator = None
        self._volumeAverage = None

        self._prevSar = 0.0
        self._currentAvgVolume = 0.0
        self._prevPriceAboveSar = False
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candleType.Value

    def OnStarted2(self, time):
        super(parabolic_sar_volume_strategy, self).OnStarted2(time)
        self._prevSar = 0.0
        self._currentAvgVolume = 0.0
        self._prevPriceAboveSar = False
        self._cooldown = 0

        self._parabolicSar = ParabolicSar()
        self._parabolicSar.Acceleration = self._acceleration.Value
        self._parabolicSar.AccelerationMax = self._maxAcceleration.Value

        self._volumeIndicator = VolumeIndicator()

        self._volumeAverage = SimpleMovingAverage()
        self._volumeAverage.Length = self._volumePeriod.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._parabolicSar, self._volumeIndicator, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolicSar)

            volumeArea = self.CreateChartArea()
            if volumeArea is not None:
                self.DrawIndicator(volumeArea, self._volumeIndicator)
                self.DrawIndicator(volumeArea, self._volumeAverage)

            self.DrawOwnTrades(area)

    def OnReseted(self):
        super(parabolic_sar_volume_strategy, self).OnReseted()
        self._prevSar = 0.0
        self._currentAvgVolume = 0.0
        self._prevPriceAboveSar = False
        self._cooldown = 0
        self._parabolicSar = None
        self._volumeIndicator = None
        self._volumeAverage = None

    def ProcessIndicators(self, candle, sarValue, volumeValue):
        # Process volume average
        avgResult = process_float(self._volumeAverage, volumeValue, candle.ServerTime, True)
        if avgResult is None:
            return

        self._currentAvgVolume = float(avgResult)
        if self._currentAvgVolume <= 0:
            return

        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        currentPrice = float(candle.ClosePrice)
        currentVolume = float(candle.TotalVolume)
        isPriceAboveSar = currentPrice > float(sarValue)

        # Volume must be 1.5x above average
        isHighVolume = currentVolume > self._currentAvgVolume * 1.5

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prevSar = float(sarValue)
            self._prevPriceAboveSar = isPriceAboveSar
            return

        cooldown = int(self._cooldownBars.Value)

        # Bullish crossover
        if isPriceAboveSar and not self._prevPriceAboveSar and isHighVolume and self.Position <= 0:
            self.CancelActiveOrders()
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown

        # Bearish crossover
        elif not isPriceAboveSar and self._prevPriceAboveSar and isHighVolume and self.Position >= 0:
            self.CancelActiveOrders()
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown

        # Exit signals
        elif (self.Position > 0 and not isPriceAboveSar) or (self.Position < 0 and isPriceAboveSar):
            self.ClosePosition()
            self._cooldown = cooldown

        self._prevSar = float(sarValue)
        self._prevPriceAboveSar = isPriceAboveSar

    def CreateClone(self):
        return parabolic_sar_volume_strategy()
