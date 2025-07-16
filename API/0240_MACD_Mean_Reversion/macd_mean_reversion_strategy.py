import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceHistogram, MovingAverageConvergenceDivergenceHistogramValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_mean_reversion_strategy(Strategy):
    """
    MACD Histogram Mean Reversion strategy.
    This strategy enters positions when MACD Histogram is significantly below or above its average value.
    """

    def __init__(self):
        super(macd_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._fast_macd_period = self.Param("FastMacdPeriod", 12) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 4) \
            .SetDisplay("Fast EMA Period", "Fast EMA period for MACD", "Indicators")

        self._slow_macd_period = self.Param("SlowMacdPeriod", 26) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 5) \
            .SetDisplay("Slow EMA Period", "Slow EMA period for MACD", "Indicators")

        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(5, 13, 4) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "Indicators")

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating MACD Histogram average", "Settings")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # State variables
        self._prev_macd_hist = 0.0
        self._avg_macd_hist = 0.0
        self._std_dev_macd_hist = 0.0
        self._sum_macd_hist = 0.0
        self._sum_squares_macd_hist = 0.0
        self._count = 0
        self._macd_hist_values = []

    @property
    def fast_macd_period(self):
        """Fast EMA period for MACD."""
        return self._fast_macd_period.Value

    @fast_macd_period.setter
    def fast_macd_period(self, value):
        self._fast_macd_period.Value = value

    @property
    def slow_macd_period(self):
        """Slow EMA period for MACD."""
        return self._slow_macd_period.Value

    @slow_macd_period.setter
    def slow_macd_period(self, value):
        self._slow_macd_period.Value = value

    @property
    def signal_period(self):
        """Signal line period for MACD."""
        return self._signal_period.Value

    @signal_period.setter
    def signal_period(self, value):
        self._signal_period.Value = value

    @property
    def average_period(self):
        """Period for calculating mean and standard deviation of MACD Histogram."""
        return self._average_period.Value

    @average_period.setter
    def average_period(self, value):
        self._average_period.Value = value

    @property
    def deviation_multiplier(self):
        """Deviation multiplier for entry signals."""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        # Reset variables
        self._prev_macd_hist = 0.0
        self._avg_macd_hist = 0.0
        self._std_dev_macd_hist = 0.0
        self._sum_macd_hist = 0.0
        self._sum_squares_macd_hist = 0.0
        self._count = 0
        self._macd_hist_values = []

        # Create MACD indicator
        macd = MovingAverageConvergenceDivergenceHistogram()
        macd.Macd.ShortMa.Length = self.fast_macd_period
        macd.Macd.LongMa.Length = self.slow_macd_period
        macd.SignalMa.Length = self.signal_period

        macd_histogram = MovingAverageConvergenceDivergenceHistogram(macd.Macd, None)

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd_histogram, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawIndicator(area, macd_histogram)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            Unit(0),  # We'll manage exits ourselves based on MACD Histogram
            Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

        super(macd_mean_reversion_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract MACD Histogram value
        if not isinstance(macd_value, MovingAverageConvergenceDivergenceHistogramValue):
            return
        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        # Update MACD Histogram statistics
        self.UpdateMacdHistStatistics(macd)

        # Save current MACD Histogram for next iteration
        self._prev_macd_hist = macd

        # If we don't have enough data yet for statistics
        if self._count < self.average_period:
            return

        # Check for entry conditions
        if self.Position == 0:
            # Long entry - MACD Histogram is significantly below its average
            if macd < self._avg_macd_hist - self.deviation_multiplier * self._std_dev_macd_hist:
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Long entry: MACD Hist = {macd}, Avg = {self._avg_macd_hist}, StdDev = {self._std_dev_macd_hist}")
            # Short entry - MACD Histogram is significantly above its average
            elif macd > self._avg_macd_hist + self.deviation_multiplier * self._std_dev_macd_hist:
                self.SellMarket(self.Volume)
                self.LogInfo(f"Short entry: MACD Hist = {macd}, Avg = {self._avg_macd_hist}, StdDev = {self._std_dev_macd_hist}")
        # Check for exit conditions
        elif self.Position > 0:  # Long position
            if macd > self._avg_macd_hist:
                self.ClosePosition()
                self.LogInfo(f"Long exit: MACD Hist = {macd}, Avg = {self._avg_macd_hist}")
        elif self.Position < 0:  # Short position
            if macd < self._avg_macd_hist:
                self.ClosePosition()
                self.LogInfo(f"Short exit: MACD Hist = {macd}, Avg = {self._avg_macd_hist}")

    def UpdateMacdHistStatistics(self, current_macd_hist):
        # Add current value to the queue
        self._macd_hist_values.append(current_macd_hist)
        self._sum_macd_hist += current_macd_hist
        self._sum_squares_macd_hist += current_macd_hist * current_macd_hist
        self._count += 1

        # If queue is larger than period, remove oldest value
        if len(self._macd_hist_values) > self.average_period:
            oldest_macd_hist = self._macd_hist_values.pop(0)
            self._sum_macd_hist -= oldest_macd_hist
            self._sum_squares_macd_hist -= oldest_macd_hist * oldest_macd_hist
            self._count -= 1

        # Calculate average and standard deviation
        if self._count > 0:
            self._avg_macd_hist = self._sum_macd_hist / self._count

            if self._count > 1:
                variance = (self._sum_squares_macd_hist - (self._sum_macd_hist * self._sum_macd_hist) / self._count) / (self._count - 1)
                self._std_dev_macd_hist = 0.0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_macd_hist = 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_mean_reversion_strategy()
