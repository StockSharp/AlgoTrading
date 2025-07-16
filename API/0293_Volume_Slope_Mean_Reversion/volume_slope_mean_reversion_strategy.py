import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class volume_slope_mean_reversion_strategy(Strategy):
    """
    Volume Slope Mean Reversion Strategy.
    This strategy trades based on volume slope reversions to the mean.
    """

    def __init__(self):
        super(volume_slope_mean_reversion_strategy, self).__init__()

        # Volume Moving Average Period.
        self._volume_ma_period = self.Param("VolumeMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume MA Period", "Period for Volume Moving Average", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

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

        self._volume_ma = None
        self._previous_volume_ratio = 0
        self._current_volume_slope = 0
        self._is_first_calculation = True

        self._average_slope = 0
        self._slope_std_dev = 0
        self._sample_count = 0
        self._sum_slopes = 0
        self._sum_slopes_squared = 0

    @property
    def VolumeMaPeriod(self):
        """Volume Moving Average Period."""
        return self._volume_ma_period.Value

    @VolumeMaPeriod.setter
    def VolumeMaPeriod(self, value):
        self._volume_ma_period.Value = value

    @property
    def LookbackPeriod(self):
        """Period for calculating slope average and standard deviation."""
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def DeviationMultiplier(self):
        """The multiplier for standard deviation to determine entry threshold."""
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        self._previous_volume_ratio = 0
        self._current_volume_slope = 0
        self._average_slope = 0
        self._slope_std_dev = 0
        self._sample_count = 0
        self._sum_slopes = 0
        self._sum_slopes_squared = 0
        self._is_first_calculation = True

        # Initialize indicators
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self.VolumeMaPeriod

        # Initialize statistics variables
        self._sample_count = 0
        self._sum_slopes = 0
        self._sum_slopes_squared = 0
        self._is_first_calculation = True

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._volume_ma, self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(self.StopLossPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent, UnitTypes.Percent))

        super(volume_slope_mean_reversion_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, volume_ma_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Process volume through SMA
        volume_indicator_value = volume_ma_value

        # Skip if indicator is not formed yet
        if not self._volume_ma.IsFormed:
            return

        # Calculate volume ratio (current volume / average volume)
        volume_ratio = candle.TotalVolume / to_float(volume_indicator_value)

        # Calculate volume ratio slope
        if self._is_first_calculation:
            self._previous_volume_ratio = volume_ratio
            self._is_first_calculation = False
            return

        self._current_volume_slope = volume_ratio - self._previous_volume_ratio
        self._previous_volume_ratio = volume_ratio

        # Update statistics for slope values
        self._sample_count += 1
        self._sum_slopes += self._current_volume_slope
        self._sum_slopes_squared += self._current_volume_slope * self._current_volume_slope

        # We need enough samples to calculate meaningful statistics
        if self._sample_count < self.LookbackPeriod:
            return

        # If we have more samples than our lookback period, adjust the statistics
        if self._sample_count > self.LookbackPeriod:
            # This is a simplified approach - ideally we would keep a circular buffer
            # of the last N slopes for more accurate calculations
            self._sample_count = self.LookbackPeriod

        # Calculate statistics
        self._average_slope = self._sum_slopes / self._sample_count
        variance = (self._sum_slopes_squared / self._sample_count) - (self._average_slope * self._average_slope)
        self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(float(variance))

        # Calculate thresholds for entries
        long_entry_threshold = self._average_slope - self.DeviationMultiplier * self._slope_std_dev
        short_entry_threshold = self._average_slope + self.DeviationMultiplier * self._slope_std_dev

        # Determine price direction based on candle
        is_bullish_candle = candle.ClosePrice > candle.OpenPrice

        # Trading logic - we take into account both volume slope and price direction
        if self._current_volume_slope < long_entry_threshold and self.Position <= 0:
            if is_bullish_candle:
                # Long entry: volume slope is significantly lower than average on a bullish candle
                # This indicates potential for bullish continuation with volume mean reversion
                self.LogInfo("Volume slope {0} below threshold {1} with bullish price, entering LONG".format(
                    self._current_volume_slope, long_entry_threshold))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_volume_slope > short_entry_threshold and self.Position >= 0:
            if not is_bullish_candle:
                # Short entry: volume slope is significantly higher than average on a bearish candle
                # This indicates potential for bearish continuation with volume mean reversion
                self.LogInfo("Volume slope {0} above threshold {1} with bearish price, entering SHORT".format(
                    self._current_volume_slope, short_entry_threshold))
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and self._current_volume_slope > self._average_slope:
            # Exit long when volume slope returns to or above average
            self.LogInfo("Volume slope {0} returned to average {1}, exiting LONG".format(
                self._current_volume_slope, self._average_slope))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._current_volume_slope < self._average_slope:
            # Exit short when volume slope returns to or below average
            self.LogInfo("Volume slope {0} returned to average {1}, exiting SHORT".format(
                self._current_volume_slope, self._average_slope))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volume_slope_mean_reversion_strategy()
