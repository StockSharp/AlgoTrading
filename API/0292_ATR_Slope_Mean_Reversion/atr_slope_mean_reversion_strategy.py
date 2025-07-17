import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, ICandleMessage
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_slope_mean_reversion_strategy(Strategy):
    """ATR Slope Mean Reversion Strategy.
    This strategy trades based on ATR slope reversions to the mean.
    """

    def __init__(self):
        super(atr_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossMultiplier = self.Param("StopLossMultiplier", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss ATR Multiplier", "Multiplier for ATR to set stop loss", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Indicator and state variables
        self._atr = None
        self._previousAtr = 0.0
        self._currentAtrSlope = 0.0
        self._isFirstCalculation = True

        self._averageSlope = 0.0
        self._slopeStdDev = 0.0
        self._sampleCount = 0
        self._sumSlopes = 0.0
        self._sumSlopesSquared = 0.0

    @property
    def AtrPeriod(self):
        """ATR Period."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def LookbackPeriod(self):
        """Period for calculating slope average and standard deviation."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """The multiplier for standard deviation to determine entry threshold."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def StopLossMultiplier(self):
        """Stop loss multiplier (in ATR units)."""
        return self._stopLossMultiplier.Value

    @StopLossMultiplier.setter
    def StopLossMultiplier(self, value):
        self._stopLossMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Return securities used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """
        Initialize indicators, statistics and charting when the strategy starts.
        """
        super(atr_slope_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Initialize statistics variables
        self._sampleCount = 0
        self._sumSlopes = 0.0
        self._sumSlopesSquared = 0.0
        self._isFirstCalculation = True
        self._previousAtr = 0.0
        self._currentAtrSlope = 0.0
        self._averageSlope = 0.0
        self._slopeStdDev = 0.0

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle: ICandleMessage, atrValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate ATR slope
        if self._isFirstCalculation:
            self._previousAtr = atrValue
            self._isFirstCalculation = False
            return

        self._currentAtrSlope = atrValue - self._previousAtr
        self._previousAtr = atrValue

        # Update statistics for slope values
        self._sampleCount += 1
        self._sumSlopes += self._currentAtrSlope
        self._sumSlopesSquared += self._currentAtrSlope * self._currentAtrSlope

        # We need enough samples to calculate meaningful statistics
        if self._sampleCount < self.LookbackPeriod:
            return

        # If we have more samples than our lookback period, adjust the statistics
        if self._sampleCount > self.LookbackPeriod:
            # This is a simplified approach - ideally we would keep a circular buffer
            # of the last N slopes for more accurate calculations
            self._sampleCount = self.LookbackPeriod

        # Calculate statistics
        self._averageSlope = self._sumSlopes / self._sampleCount
        variance = (self._sumSlopesSquared / self._sampleCount) - (self._averageSlope * self._averageSlope)
        self._slopeStdDev = 0 if variance <= 0 else Math.Sqrt(float(variance))

        # Calculate thresholds for entries
        longEntryThreshold = self._averageSlope - self.DeviationMultiplier * self._slopeStdDev
        shortEntryThreshold = self._averageSlope + self.DeviationMultiplier * self._slopeStdDev

        # Trading logic
        if self._currentAtrSlope < longEntryThreshold and self.Position <= 0:
            # Long entry: slope is significantly lower than average (mean reversion expected)
            self.LogInfo(f"ATR slope {self._currentAtrSlope} below threshold {longEntryThreshold}, entering LONG")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

            # Calculate and set stop loss based on ATR
            stopPrice = float(candle.ClosePrice - atrValue * self.StopLossMultiplier)
            self.LogInfo(f"Setting stop loss at {stopPrice} (ATR: {atrValue}, Multiplier: {self.StopLossMultiplier})")
        elif self._currentAtrSlope > shortEntryThreshold and self.Position >= 0:
            # Short entry: slope is significantly higher than average (mean reversion expected)
            self.LogInfo(f"ATR slope {self._currentAtrSlope} above threshold {shortEntryThreshold}, entering SHORT")
            self.SellMarket(self.Volume + Math.Abs(self.Position))

            # Calculate and set stop loss based on ATR
            stopPrice = float(candle.ClosePrice + atrValue * self.StopLossMultiplier)
            self.LogInfo(f"Setting stop loss at {stopPrice} (ATR: {atrValue}, Multiplier: {self.StopLossMultiplier})")
        elif self.Position > 0 and self._currentAtrSlope > self._averageSlope:
            # Exit long when slope returns to or above average
            self.LogInfo(f"ATR slope {self._currentAtrSlope} returned to average {self._averageSlope}, exiting LONG")
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._currentAtrSlope < self._averageSlope:
            # Exit short when slope returns to or below average
            self.LogInfo(f"ATR slope {self._currentAtrSlope} returned to average {self._averageSlope}, exiting SHORT")
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_slope_mean_reversion_strategy()