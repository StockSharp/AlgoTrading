import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, LinearRegression, LinearRegressionValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class cci_slope_breakout_strategy(Strategy):
    """CCI Slope Breakout Strategy"""

    def __init__(self):
        super(cci_slope_breakout_strategy, self).__init__()

        self._cciPeriod = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicator") \
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

        self._cci = None
        self._cciSlope = None
        self._prevCciSlopeValue = 0.0
        self._slopeAvg = 0.0
        self._slopeStdDev = 0.0
        self._sumSlope = 0.0
        self._sumSlopeSquared = 0.0
        self._slopeValues = []

    @property
    def CciPeriod(self):
        return self._cciPeriod.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cciPeriod.Value = value

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

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(cci_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._cciSlope = LinearRegression()  # For calculating slope
        self._cciSlope.Length = 2

        self._prevCciSlopeValue = 0.0
        self._slopeAvg = 0.0
        self._slopeStdDev = 0.0
        self._sumSlope = 0.0
        self._sumSlopeSquared = 0.0
        self._slopeValues.clear()

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._cci, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, cciValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate CCI slope
        currentSlopeTyped = process_float(
            self._cciSlope,
            cciValue,
            candle.ServerTime,
            candle.State == CandleStates.Finished,
        )

        if currentSlopeTyped.LinearReg is None:
            return

        currentSlopeValue = float(currentSlopeTyped.LinearReg)

        # Update slope stats when we have 2 values to calculate slope
        if self._prevCciSlopeValue != 0:
            # Calculate simple slope from current and previous values
            slope = currentSlopeValue - self._prevCciSlopeValue

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
            self._slopeStdDev = 0 if variance <= 0 else Math.Sqrt(variance)

            # Generate signals if we have enough data for statistics
            if len(self._slopeValues) >= self.SlopePeriod:
                # Breakout logic
                if slope > self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev and self.Position <= 0:
                    # Long position on bullish slope breakout
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Long entry: CCI slope breakout above {0:F2}".format(self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev))
                elif slope < self._slopeAvg - self.BreakoutMultiplier * self._slopeStdDev and self.Position >= 0:
                    # Short position on bearish slope breakout
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Short entry: CCI slope breakout below {0:F2}".format(self._slopeAvg - self.BreakoutMultiplier * self._slopeStdDev))

                # Exit logic - Return to mean
                if self.Position > 0 and slope < self._slopeAvg:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Long exit: CCI slope returned to mean")
                elif self.Position < 0 and slope > self._slopeAvg:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Short exit: CCI slope returned to mean")

        # Update previous value for next iteration
        self._prevCciSlopeValue = currentSlopeValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return cci_slope_breakout_strategy()

