import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import OnBalanceVolume, LinearRegression, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class obv_slope_breakout_strategy(Strategy):
    """
    OBV Slope Breakout Strategy.
    Strategy enters when OBV slope breaks out of its average range.
    """

    def __init__(self):
        super(obv_slope_breakout_strategy, self).__init__()

        # Period for calculating average and standard deviation of OBV slope.
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of OBV slope", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Period for calculating slope using linear regression.
        self._slopeLength = self.Param("SlopeLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Length", "Period for calculating slope using linear regression", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(3, 10, 1)

        # Multiplier for standard deviation to determine breakout threshold.
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Std Dev Multiplier", "Multiplier for standard deviation to determine breakout threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop-loss as a percentage of entry price.
        self._stopLoss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop-loss as a percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Type of candles to use in the strategy.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use in the strategy", "General")

        self._obv = None
        self._obvSlope = None
        self._obvSlopeAvg = None
        self._obvSlopeStdDev = None
        self._lastObvValue = 0
        self._lastObvSlope = 0
        self._lastSlopeAvg = 0
        self._lastSlopeStdDev = 0

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def SlopeLength(self):
        return self._slopeLength.Value

    @SlopeLength.setter
    def SlopeLength(self, value):
        self._slopeLength.Value = value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def StopLoss(self):
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(obv_slope_breakout_strategy, self).OnStarted(time)

        self._lastObvSlope = 0
        self._lastObvValue = 0
        self._lastSlopeAvg = 0
        self._lastSlopeStdDev = 0

        # Initialize indicators
        self._obv = OnBalanceVolume()
        self._obvSlope = LinearRegression()
        self._obvSlope.Length = self.SlopeLength
        self._obvSlopeAvg = SimpleMovingAverage()
        self._obvSlopeAvg.Length = self.LookbackPeriod
        self._obvSlopeStdDev = StandardDeviation()
        self._obvSlopeStdDev.Length = self.LookbackPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._obv, self.ProcessObv).Start()

        # Set up position protection
        self.StartProtection(
            takeProfit=Unit(),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessObv(self, candle, obvValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Calculate OBV slope
        slope = process_float(self._obvSlope, obvValue, candle.ServerTime, candle.State == CandleStates.Finished)
        if not slope.IsFinal:
            return

        if slope.LinearReg is None:
            return

        slopeValue = float(slope.LinearReg)

        self._lastObvSlope = slopeValue

        # Calculate slope average and standard deviation
        avgValue = process_float(self._obvSlopeAvg, slopeValue, candle.ServerTime, candle.State == CandleStates.Finished)
        stdDevValue = process_float(self._obvSlopeStdDev, slopeValue, candle.ServerTime, candle.State == CandleStates.Finished)

        # Store values for decision making
        self._lastObvValue = obvValue

        if avgValue.IsFinal and stdDevValue.IsFinal:
            self._lastSlopeAvg = float(avgValue)
            self._lastSlopeStdDev = float(stdDevValue)

            # Check if strategy is ready to trade
            if not self.IsFormedAndOnlineAndAllowTrading():
                return

            # Calculate breakout thresholds
            upperThreshold = self._lastSlopeAvg + self.Multiplier * self._lastSlopeStdDev
            lowerThreshold = self._lastSlopeAvg - self.Multiplier * self._lastSlopeStdDev

            # Trading logic
            if self._lastObvSlope > upperThreshold and self.Position <= 0:
                # OBV slope breaks out upward - Go Long
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif self._lastObvSlope < lowerThreshold and self.Position >= 0:
                # OBV slope breaks out downward - Go Short
                self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return obv_slope_breakout_strategy()
