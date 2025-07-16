import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_slope_mean_reversion_strategy(Strategy):
    """
    ADX Slope Mean Reversion Strategy.
    This strategy trades based on ADX slope reversions to the mean.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(adx_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._adx = None
        self._previous_adx = 0.0
        self._current_adx_slope = 0.0
        self._is_first_calculation = True

        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    @property
    def AdxPeriod(self):
        """ADX Period."""
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

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

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(adx_slope_mean_reversion_strategy, self).OnReseted()
        self._previous_adx = 0.0
        self._current_adx_slope = 0.0
        self._is_first_calculation = True
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(adx_slope_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        # Initialize statistics variables
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0
        self._is_first_calculation = True

        self._previous_adx = 0.0
        self._current_adx_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(self.StopLossPercent, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, adx_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_typed = adx_value
        if not hasattr(adx_typed, 'MovingAverage') or adx_typed.MovingAverage is None:
            return

        adx = float(adx_typed.MovingAverage)
        dx = adx_typed.Dx
        if dx is None or dx.Plus is None or dx.Minus is None:
            return

        # Calculate ADX slope
        if self._is_first_calculation:
            self._previous_adx = adx
            self._is_first_calculation = False
            return

        self._current_adx_slope = adx - self._previous_adx
        self._previous_adx = adx

        # Update statistics for slope values
        self._sample_count += 1
        self._sum_slopes += self._current_adx_slope
        self._sum_slopes_squared += self._current_adx_slope * self._current_adx_slope

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
        self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(variance)

        # Calculate thresholds for entries
        long_entry_threshold = self._average_slope - self.DeviationMultiplier * self._slope_std_dev
        short_entry_threshold = self._average_slope + self.DeviationMultiplier * self._slope_std_dev

        # Trading logic
        if self._current_adx_slope < long_entry_threshold and self.Position <= 0:
            # Long entry: slope is significantly lower than average (mean reversion expected)
            self.LogInfo("ADX slope {0} below threshold {1}, entering LONG".format(self._current_adx_slope, long_entry_threshold))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._current_adx_slope > short_entry_threshold and self.Position >= 0:
            # Short entry: slope is significantly higher than average (mean reversion expected)
            self.LogInfo("ADX slope {0} above threshold {1}, entering SHORT".format(self._current_adx_slope, short_entry_threshold))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and self._current_adx_slope > self._average_slope:
            # Exit long when slope returns to or above average
            self.LogInfo("ADX slope {0} returned to average {1}, exiting LONG".format(self._current_adx_slope, self._average_slope))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._current_adx_slope < self._average_slope:
            # Exit short when slope returns to or below average
            self.LogInfo("ADX slope {0} returned to average {1}, exiting SHORT".format(self._current_adx_slope, self._average_slope))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_slope_mean_reversion_strategy()
