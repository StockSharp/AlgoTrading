import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_distance_mean_reversion_strategy(Strategy):
    """
    Parabolic SAR Distance Mean Reversion Strategy.
    This strategy trades based on the mean reversion of the distance between price and Parabolic SAR.
    """

    def __init__(self):
        super(parabolic_sar_distance_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._acceleration_factor = self.Param("AccelerationFactor", 0.02) \
            .SetDisplay("Acceleration Factor", "Acceleration factor for Parabolic SAR", "Parabolic SAR") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.05, 0.01)

        self._acceleration_limit = self.Param("AccelerationLimit", 0.2) \
            .SetDisplay("Acceleration Limit", "Acceleration limit for Parabolic SAR", "Parabolic SAR") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.05)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of distance", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # Indicators
        self._parabolic_sar = None
        self._distance_average = None
        self._distance_std_dev = None

        # State variables
        self._current_distance_long = 0.0  # Price - SAR (for long positions)
        self._current_distance_short = 0.0 # SAR - Price (for short positions)
        self._prev_distance_long = 0.0
        self._prev_distance_short = 0.0
        self._prev_distance_avg_long = 0.0
        self._prev_distance_avg_short = 0.0
        self._prev_distance_std_dev_long = 0.0
        self._prev_distance_std_dev_short = 0.0
        self._sar_value = 0.0

    @property
    def acceleration_factor(self):
        """Acceleration factor for Parabolic SAR."""
        return self._acceleration_factor.Value

    @acceleration_factor.setter
    def acceleration_factor(self, value):
        self._acceleration_factor.Value = value

    @property
    def acceleration_limit(self):
        """Acceleration limit for Parabolic SAR."""
        return self._acceleration_limit.Value

    @acceleration_limit.setter
    def acceleration_limit(self, value):
        self._acceleration_limit.Value = value

    @property
    def lookback_period(self):
        """Lookback period for calculating the average and standard deviation of distance."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def deviation_multiplier(self):
        """Deviation multiplier for mean reversion detection."""
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

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(parabolic_sar_distance_mean_reversion_strategy, self).OnReseted()
        self._current_distance_long = 0.0
        self._current_distance_short = 0.0
        self._prev_distance_long = 0.0
        self._prev_distance_short = 0.0
        self._prev_distance_avg_long = 0.0
        self._prev_distance_avg_short = 0.0
        self._prev_distance_std_dev_long = 0.0
        self._prev_distance_std_dev_short = 0.0
        self._sar_value = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(parabolic_sar_distance_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = self.acceleration_factor
        self._parabolic_sar.AccelerationMax = self.acceleration_limit

        self._distance_average = SimpleMovingAverage()
        self._distance_average.Length = self.lookback_period
        self._distance_std_dev = StandardDeviation()
        self._distance_std_dev.Length = self.lookback_period

        # Reset stored values
        self._current_distance_long = 0.0
        self._current_distance_short = 0.0
        self._prev_distance_long = 0.0
        self._prev_distance_short = 0.0
        self._prev_distance_avg_long = 0.0
        self._prev_distance_avg_short = 0.0
        self._prev_distance_std_dev_long = 0.0
        self._prev_distance_std_dev_short = 0.0
        self._sar_value = 0.0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._parabolic_sar, self.ProcessParabolicSar).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolic_sar)
            self.DrawOwnTrades(area)

    def ProcessParabolicSar(self, candle, value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get the Parabolic SAR value
        self._sar_value = float(value)

        # Calculate distances
        self._current_distance_long = candle.ClosePrice - self._sar_value
        self._current_distance_short = self._sar_value - candle.ClosePrice

        # Calculate averages and standard deviations for both distances
        long_distance_avg = to_float(self._distance_average.Process(self._current_distance_long, candle.ServerTime, True))
        long_distance_std = to_float(self._distance_std_dev.Process(self._current_distance_long, candle.ServerTime, True))

        short_distance_avg = to_float(self._distance_average.Process(self._current_distance_short, candle.ServerTime, True))
        short_distance_std = to_float(self._distance_std_dev.Process(self._current_distance_short, candle.ServerTime, True))

        # Skip the first value
        if self._prev_distance_long == 0 or self._prev_distance_short == 0:
            self._prev_distance_long = self._current_distance_long
            self._prev_distance_short = self._current_distance_short
            self._prev_distance_avg_long = long_distance_avg
            self._prev_distance_avg_short = short_distance_avg
            self._prev_distance_std_dev_long = long_distance_std
            self._prev_distance_std_dev_short = short_distance_std
            return

        # Calculate thresholds for long position
        long_distance_extended_threshold = self._prev_distance_avg_long + self._prev_distance_std_dev_long * self.deviation_multiplier

        # Calculate thresholds for short position
        short_distance_extended_threshold = self._prev_distance_avg_short + self._prev_distance_std_dev_short * self.deviation_multiplier

        # Trading logic:
        # For long positions - when price is far above SAR (mean reversion to downside)
        if (self._current_distance_long > long_distance_extended_threshold and
                self._prev_distance_long <= long_distance_extended_threshold and
                self.Position >= 0 and candle.ClosePrice > self._sar_value):
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long distance extended: {0} > {1}. Selling at {2}".format(
                self._current_distance_long, long_distance_extended_threshold, candle.ClosePrice))
        # For short positions - when price is far below SAR (mean reversion to upside)
        elif (self._current_distance_short > short_distance_extended_threshold and
                self._prev_distance_short <= short_distance_extended_threshold and
                self.Position <= 0 and candle.ClosePrice < self._sar_value):
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short distance extended: {0} > {1}. Buying at {2}".format(
                self._current_distance_short, short_distance_extended_threshold, candle.ClosePrice))

        # Exit positions when distance returns to average
        elif (self.Position < 0 and self._current_distance_short < self._prev_distance_avg_short and
              self._prev_distance_short >= self._prev_distance_avg_short):
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short distance returned to average: {0} < {1}. Closing short position at {2}".format(
                self._current_distance_short, self._prev_distance_avg_short, candle.ClosePrice))
        elif (self.Position < 0 and self._current_distance_short < self._prev_distance_avg_short and
              self._prev_distance_short >= self._prev_distance_avg_short):
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short distance returned to average: {0} < {1}. Closing short position at {2}".format(
                self._current_distance_short, self._prev_distance_avg_short, candle.ClosePrice))
        elif (self.Position > 0 and self._current_distance_long < self._prev_distance_avg_long and
              self._prev_distance_long >= self._prev_distance_avg_long):
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long distance returned to average: {0} < {1}. Closing long position at {2}".format(
                self._current_distance_long, self._prev_distance_avg_long, candle.ClosePrice))

        # Use Parabolic SAR as dynamic stop
        elif ((self.Position > 0 and candle.ClosePrice < self._sar_value) or
              (self.Position < 0 and candle.ClosePrice > self._sar_value)):
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Price crossed below Parabolic SAR: {0} < {1}. Closing long position at {0}".format(
                    candle.ClosePrice, self._sar_value))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Price crossed above Parabolic SAR: {0} > {1}. Closing short position at {0}".format(
                    candle.ClosePrice, self._sar_value))

        # Store current values for next comparison
        self._prev_distance_long = self._current_distance_long
        self._prev_distance_short = self._current_distance_short
        self._prev_distance_avg_long = long_distance_avg
        self._prev_distance_avg_short = short_distance_avg
        self._prev_distance_std_dev_long = long_distance_std
        self._prev_distance_std_dev_short = short_distance_std

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_distance_mean_reversion_strategy()
