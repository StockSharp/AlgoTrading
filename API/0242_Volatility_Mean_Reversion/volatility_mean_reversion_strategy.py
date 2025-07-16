import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("System.Collections")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volatility_mean_reversion_strategy(Strategy):
    """
    Volatility Mean Reversion strategy.
    This strategy enters positions when ATR (volatility) is significantly below or above its average value.
    """

    def __init__(self):
        super(volatility_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 5) \
            .SetDisplay("ATR Period", "Period for Average True Range indicator", "Indicators")

        self._averagePeriod = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating ATR average and standard deviation", "Settings")

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Statistics variables
        self._prevAtr = 0.0
        self._avgAtr = 0.0
        self._stdDevAtr = 0.0
        self._sumAtr = 0.0
        self._sumSquaresAtr = 0.0
        self._count = 0
        self._atrValues = Queue[float]()

    @property
    def AtrPeriod(self):
        """ATR Period."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AveragePeriod(self):
        """Period for calculating mean and standard deviation of ATR."""
        return self._averagePeriod.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._averagePeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Deviation multiplier for entry signals."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type."""
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
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(volatility_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables
        self._prevAtr = 0
        self._avgAtr = 0
        self._stdDevAtr = 0
        self._sumAtr = 0
        self._sumSquaresAtr = 0
        self._count = 0
        self._atrValues.Clear()

        # Create ATR indicator
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0),
            Unit(self.StopLossPercent, UnitTypes.Percent),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle, atrValue):
        """Process candle and ATR value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract ATR value
        currentAtr = float(atrValue)

        # Update ATR statistics
        self.UpdateAtrStatistics(currentAtr)

        # If we don't have enough data yet for statistics
        if self._count < self.AveragePeriod:
            self._prevAtr = currentAtr
            return

        # For volatility mean reversion, we need to use price action to determine direction
        # We'll use simple momentum for direction (current price vs previous price)
        priceDirectionIsBuy = candle.ClosePrice > candle.OpenPrice

        # Check for entry conditions
        if self.Position == 0:
            # Low volatility expecting increase - possibly prepare for a breakout
            if currentAtr < self._avgAtr - self.DeviationMultiplier * self._stdDevAtr:
                # In low volatility, follow the current short-term price direction
                if priceDirectionIsBuy:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(f"Long entry: ATR = {currentAtr}, Avg = {self._avgAtr}, StdDev = {self._stdDevAtr}, Price up")
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(f"Short entry: ATR = {currentAtr}, Avg = {self._avgAtr}, StdDev = {self._stdDevAtr}, Price down")
            # High volatility expecting decrease - possibly looking for market exhaustion
            elif currentAtr > self._avgAtr + self.DeviationMultiplier * self._stdDevAtr:
                # In high volatility, consider going against the short-term trend
                # as excessive volatility often leads to reversals
                if not priceDirectionIsBuy:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(f"Contrarian long entry: ATR = {currentAtr}, Avg = {self._avgAtr}, StdDev = {self._stdDevAtr}, High volatility")
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(f"Contrarian short entry: ATR = {currentAtr}, Avg = {self._avgAtr}, StdDev = {self._stdDevAtr}, High volatility")
        # Check for exit conditions
        elif self.Position > 0:  # Long position
            if currentAtr < self._avgAtr and not priceDirectionIsBuy:
                self.ClosePosition()
                self.LogInfo(f"Long exit: ATR = {currentAtr}, Avg = {self._avgAtr}, Price down")
        elif self.Position < 0:  # Short position
            if currentAtr < self._avgAtr and priceDirectionIsBuy:
                self.ClosePosition()
                self.LogInfo(f"Short exit: ATR = {currentAtr}, Avg = {self._avgAtr}, Price up")

        # Save current ATR for next iteration
        self._prevAtr = currentAtr

    def UpdateAtrStatistics(self, currentAtr):
        # Add current value to the queue
        self._atrValues.Enqueue(currentAtr)
        self._sumAtr += currentAtr
        self._sumSquaresAtr += currentAtr * currentAtr
        self._count += 1

        # If queue is larger than period, remove oldest value
        while self._atrValues.Count > self.AveragePeriod:
            oldestAtr = self._atrValues.Dequeue()
            self._sumAtr -= oldestAtr
            self._sumSquaresAtr -= oldestAtr * oldestAtr
            self._count -= 1

        # Calculate average and standard deviation
        if self._count > 0:
            self._avgAtr = self._sumAtr / self._count

            if self._count > 1:
                variance = (self._sumSquaresAtr - (self._sumAtr * self._sumAtr) / self._count) / (self._count - 1)
                self._stdDevAtr = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._stdDevAtr = 0

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volatility_mean_reversion_strategy()
