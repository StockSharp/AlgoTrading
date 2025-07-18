import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class williams_r_mean_reversion_strategy(Strategy):
    """
    Williams %R Mean Reversion strategy.
    This strategy enters positions when Williams %R is significantly below or above its average value.
    """

    def __init__(self):
        super(williams_r_mean_reversion_strategy, self).__init__()

        # Williams %R Period.
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")

        # Period for calculating mean and standard deviation of Williams %R.
        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating Williams %R average and standard deviation", "Settings")

        # Deviation multiplier for entry signals.
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")

        # Candle type.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Stop-loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # Internal statistics
        self._prev_williams_r = 0.0
        self._avg_williams_r = 0.0
        self._std_dev_williams_r = 0.0
        self._sum_williams_r = 0.0
        self._sum_squares_williams_r = 0.0
        self._count = 0
        self._williams_r_values = Queue[float]()

    @property
    def WilliamsRPeriod(self):
        return self._williams_r_period.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williams_r_period.Value = value

    @property
    def AveragePeriod(self):
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

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
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        super(williams_r_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables
        self._prev_williams_r = 0.0
        self._avg_williams_r = 0.0
        self._std_dev_williams_r = 0.0
        self._sum_williams_r = 0.0
        self._sum_squares_williams_r = 0.0
        self._count = 0
        self._williams_r_values.Clear()

        # Create Williams %R indicator
        williams_r = WilliamsR()
        williams_r.Length = self.WilliamsRPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(williams_r, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, williams_r_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Williams %R value
        current_williams_r = float(williams_r_value)

        # Update Williams %R statistics
        self.UpdateWilliamsRStatistics(current_williams_r)

        # Save current Williams %R for next iteration
        self._prev_williams_r = current_williams_r

        # If we don't have enough data yet for statistics
        if self._count < self.AveragePeriod:
            return

        # Check for entry conditions
        if self.Position == 0:
            # Long entry - Williams %R is significantly below its average
            if current_williams_r < self._avg_williams_r - self.DeviationMultiplier * self._std_dev_williams_r:
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Long entry: Williams %R = {current_williams_r}, Avg = {self._avg_williams_r}, StdDev = {self._std_dev_williams_r}")
            # Short entry - Williams %R is significantly above its average
            elif current_williams_r > self._avg_williams_r + self.DeviationMultiplier * self._std_dev_williams_r:
                self.SellMarket(self.Volume)
                self.LogInfo(f"Short entry: Williams %R = {current_williams_r}, Avg = {self._avg_williams_r}, StdDev = {self._std_dev_williams_r}")
        # Check for exit conditions
        elif self.Position > 0:  # Long position
            if current_williams_r > self._avg_williams_r:
                self.ClosePosition()
                self.LogInfo(f"Long exit: Williams %R = {current_williams_r}, Avg = {self._avg_williams_r}")
        elif self.Position < 0:  # Short position
            if current_williams_r < self._avg_williams_r:
                self.ClosePosition()
                self.LogInfo(f"Short exit: Williams %R = {current_williams_r}, Avg = {self._avg_williams_r}")

    def UpdateWilliamsRStatistics(self, current_williams_r):
        # Add current value to the queue
        self._williams_r_values.Enqueue(current_williams_r)
        self._sum_williams_r += current_williams_r
        self._sum_squares_williams_r += current_williams_r * current_williams_r
        self._count += 1

        # If queue is larger than period, remove oldest value
        if self._williams_r_values.Count > self.AveragePeriod:
            oldest = self._williams_r_values.Dequeue()
            self._sum_williams_r -= oldest
            self._sum_squares_williams_r -= oldest * oldest
            self._count -= 1

        # Calculate average and standard deviation
        if self._count > 0:
            self._avg_williams_r = self._sum_williams_r / self._count

            if self._count > 1:
                variance = (self._sum_squares_williams_r - (self._sum_williams_r * self._sum_williams_r) / self._count) / (self._count - 1)
                self._std_dev_williams_r = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_williams_r = 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_r_mean_reversion_strategy()
