import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volatility_adjusted_momentum_strategy(Strategy):
    """
    Strategy based on Momentum adjusted by volatility (ATR)
    Enters positions when the volatility-adjusted momentum exceeds average plus a multiple of standard deviation
    """

    def __init__(self):
        """Constructor"""
        super(volatility_adjusted_momentum_strategy, self).__init__()

        # Momentum period
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for Momentum indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        # ATR period
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for Average True Range indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        # Lookback period for statistics calculation
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Standard deviation multiplier for breakout detection
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop loss value
        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss value in ATRs", "Risk Management")

        # Candle type
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal variables
        self._momentum = None
        self._atr = None
        self._momentum_atr_ratio = 0.0
        self._avg_ratio = 0.0
        self._std_dev_ratio = 0.0
        self._ratios = []
        self._current_index = 0

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(volatility_adjusted_momentum_strategy, self).OnReseted()
        self._momentum = None
        self._atr = None
        self._momentum_atr_ratio = 0.0
        self._avg_ratio = 0.0
        self._std_dev_ratio = 0.0
        self._ratios = [0.0] * self.LookbackPeriod
        self._current_index = 0

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(volatility_adjusted_momentum_strategy, self).OnStarted(time)

        self._momentum = Momentum()
        self._momentum.Length = self.MomentumPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        self._momentum_atr_ratio = 0.0
        self._avg_ratio = 0.0
        self._std_dev_ratio = 0.0
        self._ratios = [0.0] * self.LookbackPeriod
        self._current_index = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._momentum, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._momentum)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Set up position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=True
        )
    def ProcessCandle(self, candle, momentum_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if indicators are formed
        if not self._momentum.IsFormed or not self._atr.IsFormed:
            return

        # Avoid division by zero
        if atr_value == 0:
            return

        # Calculate the momentum/ATR ratio
        self._momentum_atr_ratio = float(momentum_value) / float(atr_value)

        # Store ratio in array and update index
        self._ratios[self._current_index] = self._momentum_atr_ratio
        self._current_index = (self._current_index + 1) % self.LookbackPeriod

        # Calculate statistics once we have enough data
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.CalculateStatistics()

        # Trading logic
        if abs(self._avg_ratio) > 0:
            # Long signal: momentum/ATR ratio exceeds average + k*stddev (we don't have a long position)
            if (self._momentum_atr_ratio > self._avg_ratio + self.DeviationMultiplier * self._std_dev_ratio and
                    self.Position <= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Long signal: Momentum/ATR {0} > Avg {1} + {2}*StdDev {3}".format(
                    self._momentum_atr_ratio, self._avg_ratio, self.DeviationMultiplier, self._std_dev_ratio))
            # Short signal: momentum/ATR ratio falls below average - k*stddev (we don't have a short position)
            elif (self._momentum_atr_ratio < self._avg_ratio - self.DeviationMultiplier * self._std_dev_ratio and
                    self.Position >= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Short signal: Momentum/ATR {0} < Avg {1} - {2}*StdDev {3}".format(
                    self._momentum_atr_ratio, self._avg_ratio, self.DeviationMultiplier, self._std_dev_ratio))

            # Exit conditions - when momentum/ATR ratio returns to average
            if self.Position > 0 and self._momentum_atr_ratio < self._avg_ratio:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Momentum/ATR {0} < Avg {1}".format(
                    self._momentum_atr_ratio, self._avg_ratio))
            elif self.Position < 0 and self._momentum_atr_ratio > self._avg_ratio:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Momentum/ATR {0} > Avg {1}".format(
                    self._momentum_atr_ratio, self._avg_ratio))

    def CalculateStatistics(self):
        # Reset statistics
        self._avg_ratio = 0.0
        sum_squared_diffs = 0.0

        # Calculate average
        for i in range(self.LookbackPeriod):
            self._avg_ratio += self._ratios[i]
        self._avg_ratio /= float(self.LookbackPeriod)

        # Calculate standard deviation
        for i in range(self.LookbackPeriod):
            diff = self._ratios[i] - self._avg_ratio
            sum_squared_diffs += diff * diff

        self._std_dev_ratio = Math.Sqrt(sum_squared_diffs / float(self.LookbackPeriod))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volatility_adjusted_momentum_strategy()
