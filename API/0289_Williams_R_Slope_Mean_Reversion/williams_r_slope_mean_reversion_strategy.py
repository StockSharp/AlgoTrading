import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class williams_r_slope_mean_reversion_strategy(Strategy):
    """
    Williams %R Slope Mean Reversion Strategy.
    This strategy trades based on Williams %R slope reversions to the mean.
    """

    def __init__(self):
        super(williams_r_slope_mean_reversion_strategy, self).__init__()

        # Williams %R Period.
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        # Period for calculating slope average and standard deviation.
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # The multiplier for standard deviation to determine entry threshold.
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state variables
        self._williams_r = None
        self._previous_slope_value = 0.0
        self._current_slope_value = 0.0
        self._is_first_calculation = True
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    # Williams %R Period.
    @property
    def williams_r_period(self):
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    # Period for calculating slope average and standard deviation.
    @property
    def lookback_period(self):
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    # The multiplier for standard deviation to determine entry threshold.
    @property
    def deviation_multiplier(self):
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    # Stop loss percentage.
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    # Candle type for strategy.
    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(williams_r_slope_mean_reversion_strategy, self).OnReseted()
        self._williams_r = None
        self._previous_slope_value = 0.0
        self._current_slope_value = 0.0
        self._is_first_calculation = True
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(williams_r_slope_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._williams_r = WilliamsR()
        self._williams_r.Length = self.williams_r_period

        # Initialize statistics variables
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0
        self._is_first_calculation = True
        self._previous_slope_value = 0.0
        self._current_slope_value = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._williams_r, self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williams_r)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(self.stop_loss_percent, UnitTypes.Percent),
            Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, williams_r_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Williams %R slope
        if self._is_first_calculation:
            self._previous_slope_value = williams_r_value
            self._is_first_calculation = False
            return

        self._current_slope_value = williams_r_value - self._previous_slope_value
        self._previous_slope_value = williams_r_value

        # Update statistics for slope values
        self._sample_count += 1
        self._sum_slopes += self._current_slope_value
        self._sum_slopes_squared += self._current_slope_value * self._current_slope_value

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
        self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(float(variance))

        # Calculate thresholds for entries
        long_entry_threshold = self._average_slope - self.deviation_multiplier * self._slope_std_dev
        short_entry_threshold = self._average_slope + self.deviation_multiplier * self._slope_std_dev

        # Trading logic
        if self._current_slope_value < long_entry_threshold and self.Position <= 0:
            # Long entry: slope is significantly lower than average (mean reversion expected)
            self.LogInfo(f"Williams %R slope {self._current_slope_value} below threshold {long_entry_threshold}, entering LONG")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_slope_value > short_entry_threshold and self.Position >= 0:
            # Short entry: slope is significantly higher than average (mean reversion expected)
            self.LogInfo(f"Williams %R slope {self._current_slope_value} above threshold {short_entry_threshold}, entering SHORT")
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and self._current_slope_value > self._average_slope:
            # Exit long when slope returns to or above average
            self.LogInfo(f"Williams %R slope {self._current_slope_value} returned to average {self._average_slope}, exiting LONG")
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._current_slope_value < self._average_slope:
            # Exit short when slope returns to or below average
            self.LogInfo(f"Williams %R slope {self._current_slope_value} returned to average {self._average_slope}, exiting SHORT")
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_r_slope_mean_reversion_strategy()
