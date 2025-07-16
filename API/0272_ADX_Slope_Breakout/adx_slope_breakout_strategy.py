import clr

clr.AddReference("System.Collections")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, LinearRegression
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_slope_breakout_strategy(Strategy):
    """
    ADX Slope Breakout Strategy

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(adx_slope_breakout_strategy, self).__init__()

        # Initialize internal state
        self._adx = None
        self._adxSlope = None
        self._prevSlopeValue = 0.0
        self._slopeAvg = 0.0
        self._slopeStdDev = 0.0
        self._sumSlope = 0.0
        self._sumSlopeSquared = 0.0
        self._slopeValues = Queue[float]()

        # Initialize strategy parameters
        self._adxPeriod = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

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

    @property
    def AdxPeriod(self):
        return self._adxPeriod.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adxPeriod.Value = value

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
        """!! REQUIRED!! Returns securities this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(adx_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod
        self._adxSlope = LinearRegression()
        self._adxSlope.Length = 2  # For calculating slope

        self._prevSlopeValue = 0.0
        self._slopeAvg = 0.0
        self._slopeStdDev = 0.0
        self._sumSlope = 0.0
        self._sumSlopeSquared = 0.0
        self._slopeValues = Queue[float]()

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, adxValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get ADX value
        typedAdx = adxValue
        adx = typedAdx.MovingAverage
        if adx is None:
            return

        dx = typedAdx.Dx
        diMinus = dx.Minus
        diPlus = dx.Plus
        if diMinus is None or diPlus is None:
            return

        # Calculate ADX slope
        currentSlopeTyped = process_float(self._adxSlope, adx, candle.ServerTime, candle.State == CandleStates.Finished)
        currentSlopeValue = currentSlopeTyped.LinearReg
        if currentSlopeValue is None:
            return

        # Update slope stats when we have 2 values to calculate slope
        if self._prevSlopeValue != 0:
            # Calculate simple slope from current and previous values
            slope = currentSlopeValue - self._prevSlopeValue

            # Update running statistics
            self._slopeValues.Enqueue(slope)
            self._sumSlope += slope
            self._sumSlopeSquared += slope * slope

            # Remove oldest value if we have enough
            if self._slopeValues.Count > self.SlopePeriod:
                oldSlope = self._slopeValues.Dequeue()
                self._sumSlope -= oldSlope
                self._sumSlopeSquared -= oldSlope * oldSlope

            # Calculate average and standard deviation
            self._slopeAvg = self._sumSlope / self._slopeValues.Count
            variance = (self._sumSlopeSquared / self._slopeValues.Count) - (self._slopeAvg * self._slopeAvg)
            self._slopeStdDev = 0 if variance <= 0 else Math.Sqrt(variance)

            # Generate signals if we have enough data for statistics
            if self._slopeValues.Count >= self.SlopePeriod:
                # Get DI+ and DI- from the ADX indicator for trend direction
                isBullish = diPlus > diMinus

                # Breakout logic
                if slope > self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev and self.Position <= 0:
                    # ADX slope breakout indicates stronger trend
                    # Only go long if DI+ > DI- (bullish)
                    if isBullish:
                        self.BuyMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo("Long entry: ADX slope breakout above {0:F2} with DI+ > DI-".format(
                            self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev))
                elif slope > self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev and self.Position >= 0:
                    # ADX slope breakout indicates stronger trend
                    # Only go short if DI+ < DI- (bearish)
                    if not isBullish:
                        self.SellMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo("Short entry: ADX slope breakout above {0:F2} with DI+ < DI-".format(
                            self._slopeAvg + self.BreakoutMultiplier * self._slopeStdDev))

                # Exit logic - Return to mean or ADX weakening
                if self.Position > 0 and (slope < self._slopeAvg or not isBullish):
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Long exit: ADX slope returned to mean or trend changed to bearish")
                elif self.Position < 0 and (slope < self._slopeAvg or isBullish):
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Short exit: ADX slope returned to mean or trend changed to bullish")

        # Update previous value for next iteration
        self._prevSlopeValue = currentSlopeValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_slope_breakout_strategy()

