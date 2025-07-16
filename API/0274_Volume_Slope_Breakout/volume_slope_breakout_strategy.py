import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import VolumeIndicator, SimpleMovingAverage, ExponentialMovingAverage, LinearRegression, LinearRegressionValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_slope_breakout_strategy(Strategy):
    """Volume Slope Breakout Strategy"""

    def __init__(self):
        super(volume_slope_breakout_strategy, self).__init__()

        self._volumeSmaPeriod = self.Param("VolumeSMAPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume SMA Period", "Period for volume SMA calculation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._slopePeriod = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope average and standard deviation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._breakoutMultiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._volumeIndicator = None
        self._volumeSma = None
        self._priceEma = None
        self._volumeSlope = None
        self._prevSlopeValue = 0
        self._slopeAvg = 0
        self._slopeStdDev = 0
        self._sumSlope = 0
        self._sumSlopeSquared = 0
        self._slopeValues = []

    @property
    def VolumeSMAPeriod(self):
        return self._volumeSmaPeriod.Value

    @VolumeSMAPeriod.setter
    def VolumeSMAPeriod(self, value):
        self._volumeSmaPeriod.Value = value

    @property
    def SlopePeriod(self):
        return self._slopePeriod.Value

    @SlopePeriod.setter
    def SlopePeriod(self, value):
        self._slopePeriod.Value = value

    @property
    def BreakoutMultiplier(self):
        return self._breakoutMultiplier.Value

    @BreakoutMultiplier.setter
    def BreakoutMultiplier(self, value):
        self._breakoutMultiplier.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        super(volume_slope_breakout_strategy, self).OnReseted()
        self._prevSlopeValue = 0
        self._slopeAvg = 0
        self._slopeStdDev = 0
        self._sumSlope = 0
        self._sumSlopeSquared = 0
        self._slopeValues = []

    def OnStarted(self, time):
        super(volume_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._volumeIndicator = VolumeIndicator()
        self._volumeSma = SimpleMovingAverage()
        self._volumeSma.Length = self.VolumeSMAPeriod
        self._priceEma = ExponentialMovingAverage()
        self._priceEma.Length = 20  # For trend direction
        self._volumeSlope = LinearRegression()
        self._volumeSlope.Length = 2  # For calculating slope

        self._prevSlopeValue = 0
        self._slopeAvg = 0
        self._slopeStdDev = 0
        self._sumSlope = 0
        self._sumSlopeSquared = 0
        self._slopeValues = []

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and processing logic using event handlers
        # since we need to track multiple indicator values
        subscription.Bind(self._volumeIndicator, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._volumeIndicator)
            self.DrawIndicator(area, self._volumeSma)
            self.DrawIndicator(area, self._priceEma)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, volumeValue):
        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Process volume SMA
        volumeSma = to_float(process_float(self._volumeSma, volumeValue, candle.ServerTime, candle.State == CandleStates.Finished))

        # Process price EMA for trend direction
        priceEma = to_float(process_candle(self._priceEma, candle))
        priceAboveEma = candle.ClosePrice > priceEma

        # Calculate volume slope (current volume relative to SMA)
        volumeRatio = volumeValue / volumeSma

        # We use LinearRegression to calculate slope of this ratio
        currentSlopeTyped = process_float(self._volumeSlope, volumeRatio, candle.ServerTime, candle.State == CandleStates.Finished)

        if not isinstance(currentSlopeTyped, LinearRegressionValue) or currentSlopeTyped.LinearReg is None:
            return  # Skip if slope is not available
        currentSlopeValue = currentSlopeTyped.LinearReg

        # Update slope stats when we have 2 values to calculate slope
        if self._prevSlopeValue != 0:
            # Calculate simple slope from current and previous values
            slope = currentSlopeValue - self._prevSlopeValue

            # Update running statistics
            self._slopeValues.append(slope)
            self._sumSlope += slope
            self._sumSlopeSquared += slope * slope

            # Remove oldest value if we have enough
            if len(self._slopeValues) > self.SlopePeriod:
                oldSlope = self._slopeValues.pop(0)
                self._sumSlope -= oldSlope
                self._sumSlopeSquared -= oldSlope * oldSlope

            # Calculate average and standard deviation
            self._slopeAvg = self._sumSlope / len(self._slopeValues)
            variance = (self._sumSlopeSquared / len(self._slopeValues)) - (self._slopeAvg * self._slopeAvg)
            self._slopeStdDev = 0 if variance <= 0 else Math.Sqrt(float(variance))

            # Generate signals if we have enough data for statistics
            if len(self._slopeValues) >= self.SlopePeriod and volumeValue > volumeSma:
                # Breakout logic - Volume slope increase with price confirmation
                if slope > self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev:
                    if priceAboveEma and self.Position <= 0:
                        # Go long on volume spike with price above EMA
                        self.BuyMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo("Long entry: Volume slope breakout above {0:F3} with price above EMA".format(
                            self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev))
                    elif not priceAboveEma and self.Position >= 0:
                        # Go short on volume spike with price below EMA
                        self.SellMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo("Short entry: Volume slope breakout above {0:F3} with price below EMA".format(
                            self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev))

                # Exit logic - Volume spike down (unusual activity ending)
                if slope < self._slopeAvg - self.BreakoutMultiplier * self._slopeStdDev:
                    if self.Position > 0:
                        self.SellMarket(Math.Abs(self.Position))
                        self.LogInfo("Long exit: Volume activity declining")
                    elif self.Position < 0:
                        self.BuyMarket(Math.Abs(self.Position))
                        self.LogInfo("Short exit: Volume activity declining")

            # Additional exit rule - Return to mean with lower volume
            if volumeValue < volumeSma and slope < self._slopeAvg:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Long exit: Volume returned to normal levels")
                elif self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Short exit: Volume returned to normal levels")

        # Update previous value for next iteration
        self._prevSlopeValue = currentSlopeValue

    def CreateClone(self):
        return volume_slope_breakout_strategy()
