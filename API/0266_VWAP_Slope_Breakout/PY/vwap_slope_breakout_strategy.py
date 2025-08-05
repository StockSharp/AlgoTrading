import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class vwap_slope_breakout_strategy(Strategy):
    """
    Strategy based on VWAP (Volume Weighted Average Price) Slope breakout
    Enters positions when the slope of VWAP exceeds average slope plus a multiple of standard deviation
    """

    def __init__(self):
        super(vwap_slope_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._vwap = None
        self._prev_vwap_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = [0.0 for _ in range(self.lookback_period)]
        self._current_index = 0
        self._is_initialized = False

    @property
    def lookback_period(self):
        """Lookback period for slope statistics calculation"""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def deviation_multiplier(self):
        """Standard deviation multiplier for breakout detection"""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(vwap_slope_breakout_strategy, self).OnReseted()
        self._prev_vwap_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = [0.0 for _ in range(self.lookback_period)]
        self._current_index = 0
        self._is_initialized = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        # VolumeWeightedMovingAverage is used as VWAP indicator
        self._vwap = VolumeWeightedMovingAverage()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._vwap, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawOwnTrades(area)

        # Set up position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        super(vwap_slope_breakout_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, vwap_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Initialize on first valid value
        if not self._is_initialized:
            self._prev_vwap_value = vwap_value
            self._is_initialized = True
            return

        # Calculate current slope (simple difference for now)
        self._current_slope = vwap_value - self._prev_vwap_value

        # Store slope in array and update index
        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % self.lookback_period

        # Calculate statistics once we have enough data
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.CalculateStatistics()

        # Trading logic
        if Math.Abs(self._avg_slope) > 0:  # Avoid division by zero
            # Long signal: slope exceeds average + k*stddev (slope is positive and we don't have a long position)
            if (self._current_slope > 0 and
                    self._current_slope > self._avg_slope + self.deviation_multiplier * self._std_dev_slope and
                    self.Position <= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo(
                    f"Long signal: VWAP Slope {self._current_slope} > Avg {self._avg_slope} + {self.deviation_multiplier}*StdDev {self._std_dev_slope}")
            # Short signal: slope exceeds average + k*stddev in negative direction (slope is negative and we don't have a short position)
            elif (self._current_slope < 0 and
                  self._current_slope < self._avg_slope - self.deviation_multiplier * self._std_dev_slope and
                  self.Position >= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo(
                    f"Short signal: VWAP Slope {self._current_slope} < Avg {self._avg_slope} - {self.deviation_multiplier}*StdDev {self._std_dev_slope}")

            # Exit conditions - when slope returns to average
            if self.Position > 0 and self._current_slope < self._avg_slope:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(
                    f"Exit long: VWAP Slope {self._current_slope} < Avg {self._avg_slope}")
            elif self.Position < 0 and self._current_slope > self._avg_slope:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(
                    f"Exit short: VWAP Slope {self._current_slope} > Avg {self._avg_slope}")

        # Store current VWAP value for next slope calculation
        self._prev_vwap_value = vwap_value

    def CalculateStatistics(self):
        # Reset statistics
        self._avg_slope = 0.0
        sum_squared_diffs = 0.0

        # Calculate average
        for i in range(self.lookback_period):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= self.lookback_period

        # Calculate standard deviation
        for i in range(self.lookback_period):
            diff = self._slopes[i] - self._avg_slope
            sum_squared_diffs += diff * diff

        self._std_dev_slope = Math.Sqrt(sum_squared_diffs / self.lookback_period)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_slope_breakout_strategy()
