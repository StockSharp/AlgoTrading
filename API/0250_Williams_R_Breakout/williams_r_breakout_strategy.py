import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class williams_r_breakout_strategy(Strategy):
    """
    Strategy that trades on Williams %R breakouts.
    When Williams %R rises significantly above its average or falls significantly below its average,
    it enters position in the corresponding direction.
    """

    def __init__(self):
        """Initialize williams_r_breakout_strategy."""
        super(williams_r_breakout_strategy, self).__init__()

        self._williamsRPeriod = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._avgPeriod = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for Williams %R average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLoss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._prevWilliamsRValue = 0
        self._prevWilliamsRAvgValue = 0
        self._williamsR = None
        self._williamsRAverage = None

    @property
    def WilliamsRPeriod(self):
        """Williams %R period."""
        return self._williamsRPeriod.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williamsRPeriod.Value = value

    @property
    def AvgPeriod(self):
        """Period for Williams %R average calculation."""
        return self._avgPeriod.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avgPeriod.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for breakout detection."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLoss(self):
        """Stop-loss percentage."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(williams_r_breakout_strategy, self).OnStarted(time)

        self._prevWilliamsRValue = 0
        self._prevWilliamsRAvgValue = 0

        # Create indicators
        self._williamsR = WilliamsR()
        self._williamsR.Length = self.WilliamsRPeriod
        self._williamsRAverage = SimpleMovingAverage()
        self._williamsRAverage.Length = self.AvgPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind Williams %R to the candle subscription
        subscription.BindEx(self._williamsR, self.ProcessWilliamsR).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williamsR)
            self.DrawOwnTrades(area)

    def ProcessWilliamsR(self, candle, williamsRValue):
        if candle.State != CandleStates.Finished:
            return

        if not williamsRValue.IsFinal:
            return

        # Get current Williams %R value
        currentWilliamsR = to_float(williamsRValue)

        # Process Williams %R through average indicator
        williamsRAvgValue = process_float(self._williamsRAverage, currentWilliamsR, candle.ServerTime, candle.State == CandleStates.Finished)
        currentWilliamsRAvg = to_float(williamsRAvgValue)

        # For first values, just save and skip
        if self._prevWilliamsRValue == 0:
            self._prevWilliamsRValue = currentWilliamsR
            self._prevWilliamsRAvgValue = currentWilliamsRAvg
            return

        # Calculate standard deviation of Williams %R (simplified approach)
        stdDev = Math.Abs(currentWilliamsR - currentWilliamsRAvg) * 1.5  # Simplified approximation

        # Skip if indicators are not formed yet
        if not self._williamsRAverage.IsFormed:
            self._prevWilliamsRValue = currentWilliamsR
            self._prevWilliamsRAvgValue = currentWilliamsRAvg
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prevWilliamsRValue = currentWilliamsR
            self._prevWilliamsRAvgValue = currentWilliamsRAvg
            return

        # Williams %R breakout detection
        if currentWilliamsR > currentWilliamsRAvg + self.Multiplier * stdDev and self.Position <= 0:
            # Williams %R breaking out upward (but remember Williams %R is negative, less negative = bullish)
            self.CancelActiveOrders()
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif currentWilliamsR < currentWilliamsRAvg - self.Multiplier * stdDev and self.Position >= 0:
            # Williams %R breaking out downward (more negative = bearish)
            self.CancelActiveOrders()
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit condition - Williams %R returns to average
        elif (self.Position > 0 and currentWilliamsR < currentWilliamsRAvg) or \
             (self.Position < 0 and currentWilliamsR > currentWilliamsRAvg):
            # Exit position
            self.ClosePosition()

        # Update previous values
        self._prevWilliamsRValue = currentWilliamsR
        self._prevWilliamsRAvgValue = currentWilliamsRAvg

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_r_breakout_strategy()
