import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class cci_mean_reversion_strategy(Strategy):
    """
    CCI Mean Reversion strategy.
    This strategy enters positions when CCI is significantly below or above its average value.
    """

    def __init__(self):
        super(cci_mean_reversion_strategy, self).__init__()

        # CCI Period.
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "Indicators")

        # Period for calculating mean and standard deviation of CCI.
        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating CCI average and standard deviation", "Settings")

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

        self._prev_cci = 0.0
        self._avg_cci = 0.0
        self._std_dev_cci = 0.0
        self._sum_cci = 0.0
        self._sum_squares_cci = 0.0
        self._count = 0
        self._cci_values = []

    @property
    def CciPeriod(self):
        """CCI Period."""
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def AveragePeriod(self):
        """Period for calculating mean and standard deviation of CCI."""
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

    @property
    def DeviationMultiplier(self):
        """Deviation multiplier for entry signals."""
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        # Reset variables
        self._prev_cci = 0.0
        self._avg_cci = 0.0
        self._std_dev_cci = 0.0
        self._sum_cci = 0.0
        self._sum_squares_cci = 0.0
        self._count = 0
        self._cci_values = []

        # Create CCI indicator
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(cci, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        super(cci_mean_reversion_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, cci_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract CCI value
        current_cci = to_float(cci_value)

        # Update CCI statistics
        self.UpdateCciStatistics(current_cci)

        # Save current CCI for next iteration
        self._prev_cci = current_cci

        # If we don't have enough data yet for statistics
        if self._count < self.AveragePeriod:
            return

        # Check for entry conditions
        if self.Position == 0:
            # Long entry - CCI is significantly below its average
            if current_cci < self._avg_cci - self.DeviationMultiplier * self._std_dev_cci:
                self.BuyMarket(self.Volume)
                self.LogInfo("Long entry: CCI = {0}, CCI Avg = {1}, CCI StdDev = {2}".format(
                    current_cci, self._avg_cci, self._std_dev_cci))
            # Short entry - CCI is significantly above its average
            elif current_cci > self._avg_cci + self.DeviationMultiplier * self._std_dev_cci:
                self.SellMarket(self.Volume)
                self.LogInfo("Short entry: CCI = {0}, CCI Avg = {1}, CCI StdDev = {2}".format(
                    current_cci, self._avg_cci, self._std_dev_cci))
        # Check for exit conditions
        elif self.Position > 0:  # Long position
            if current_cci > self._avg_cci:
                self.ClosePosition()
                self.LogInfo("Long exit: CCI = {0}, CCI Avg = {1}".format(current_cci, self._avg_cci))
        elif self.Position < 0:  # Short position
            if current_cci < self._avg_cci:
                self.ClosePosition()
                self.LogInfo("Short exit: CCI = {0}, CCI Avg = {1}".format(current_cci, self._avg_cci))

    def UpdateCciStatistics(self, current_cci):
        # Add current value to the queue
        self._cci_values.append(current_cci)
        self._sum_cci += current_cci
        self._sum_squares_cci += current_cci * current_cci
        self._count += 1

        # If queue is larger than period, remove oldest value
        if len(self._cci_values) > self.AveragePeriod:
            oldest_cci = self._cci_values.pop(0)
            self._sum_cci -= oldest_cci
            self._sum_squares_cci -= oldest_cci * oldest_cci
            self._count -= 1

        # Calculate average and standard deviation
        if self._count > 0:
            self._avg_cci = self._sum_cci / self._count

            if self._count > 1:
                variance = (self._sum_squares_cci - (self._sum_cci * self._sum_cci) / self._count) / (self._count - 1)
                self._std_dev_cci = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_cci = 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return cci_mean_reversion_strategy()

