import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_slope_mean_reversion_strategy(Strategy):
    """
    MACD Slope Mean Reversion Strategy.
    This strategy trades based on MACD histogram slope reversions to the mean.
    """

    def __init__(self):
        super(macd_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._fast_macd_period = self.Param("FastMacdPeriod", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicator Parameters") \
            .SetCanOptimize(True).SetOptimize(8, 20, 2)

        self._slow_macd_period = self.Param("SlowMacdPeriod", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicator Parameters") \
            .SetCanOptimize(True).SetOptimize(20, 40, 2)

        self._signal_macd_period = self.Param("SignalMacdPeriod", 9) \
            .SetDisplay("MACD Signal", "Signal line period for MACD", "Indicator Parameters") \
            .SetCanOptimize(True).SetOptimize(5, 15, 2)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters") \
            .SetCanOptimize(True).SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True).SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True).SetOptimize(1.0, 5.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # State variables
        self._macd = None
        self._previous_histogram = 0.0
        self._current_histogram_slope = 0.0
        self._is_first_calculation = True

        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    @property
    def fast_macd_period(self):
        """MACD Fast Period."""
        return self._fast_macd_period.Value

    @fast_macd_period.setter
    def fast_macd_period(self, value):
        self._fast_macd_period.Value = value

    @property
    def slow_macd_period(self):
        """MACD Slow Period."""
        return self._slow_macd_period.Value

    @slow_macd_period.setter
    def slow_macd_period(self, value):
        self._slow_macd_period.Value = value

    @property
    def signal_macd_period(self):
        """MACD Signal Period."""
        return self._signal_macd_period.Value

    @signal_macd_period.setter
    def signal_macd_period(self, value):
        self._signal_macd_period.Value = value

    @property
    def lookback_period(self):
        """Period for calculating slope average and standard deviation."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def deviation_multiplier(self):
        """The multiplier for standard deviation to determine entry threshold."""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(macd_slope_mean_reversion_strategy, self).OnReseted()
        self._macd = None
        self._previous_histogram = 0.0
        self._current_histogram_slope = 0.0
        self._is_first_calculation = True
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(macd_slope_mean_reversion_strategy, self).OnStarted(time)

        # Initialize statistics variables
        self._previous_histogram = 0.0
        self._current_histogram_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0
        self._is_first_calculation = True

        # Initialize indicators
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.fast_macd_period
        self._macd.Macd.LongMa.Length = self.slow_macd_period
        self._macd.SignalMa.Length = self.signal_macd_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(self.stop_loss_percent, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, macd_value):
        """Processes each finished candle and executes trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract MACD and Signal values
        try:
            macd = float(macd_value.Macd)
            signal = float(macd_value.Signal)
        except Exception:
            return

        # Calculate MACD histogram
        histogram = macd - signal

        # Calculate MACD histogram slope
        if self._is_first_calculation:
            self._previous_histogram = histogram
            self._is_first_calculation = False
            return

        self._current_histogram_slope = histogram - self._previous_histogram
        self._previous_histogram = histogram

        # Update statistics for slope values
        self._sample_count += 1
        self._sum_slopes += self._current_histogram_slope
        self._sum_slopes_squared += self._current_histogram_slope * self._current_histogram_slope

        # We need enough samples to calculate meaningful statistics
        if self._sample_count < self.lookback_period:
            return

        # If we have more samples than our lookback period, adjust the statistics
        if self._sample_count > self.lookback_period:
            # This is a simplified approach - ideally we would keep a circular buffer
            # of the last N slopes for more accurate calculations
            self._sample_count = self.lookback_period

        # Calculate statistics
        self._average_slope = self._sum_slopes / self._sample_count
        variance = (self._sum_slopes_squared / self._sample_count) - (self._average_slope * self._average_slope)
        self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(variance)

        # Calculate thresholds for entries
        long_entry_threshold = self._average_slope - self.deviation_multiplier * self._slope_std_dev
        short_entry_threshold = self._average_slope + self.deviation_multiplier * self._slope_std_dev

        # Trading logic
        if self._current_histogram_slope < long_entry_threshold and self.Position <= 0:
            # Long entry: slope is significantly lower than average (mean reversion expected)
            self.LogInfo(f"MACD histogram slope {self._current_histogram_slope} below threshold {long_entry_threshold}, entering LONG")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_histogram_slope > short_entry_threshold and self.Position >= 0:
            # Short entry: slope is significantly higher than average (mean reversion expected)
            self.LogInfo(f"MACD histogram slope {self._current_histogram_slope} above threshold {short_entry_threshold}, entering SHORT")
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and self._current_histogram_slope > self._average_slope:
            # Exit long when slope returns to or above average
            self.LogInfo(f"MACD histogram slope {self._current_histogram_slope} returned to average {self._average_slope}, exiting LONG")
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._current_histogram_slope < self._average_slope:
            # Exit short when slope returns to or below average
            self.LogInfo(f"MACD histogram slope {self._current_histogram_slope} returned to average {self._average_slope}, exiting SHORT")
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_slope_mean_reversion_strategy()
