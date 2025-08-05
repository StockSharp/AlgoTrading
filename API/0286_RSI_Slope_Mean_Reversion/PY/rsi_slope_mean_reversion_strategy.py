import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_slope_mean_reversion_strategy(Strategy):
    """
    The RSI Slope Mean Reversion strategy focuses on extreme readings of the RSI to exploit reversion. Wide departures from the average level rarely last.

    Trades trigger when the indicator swings far from its mean and then begins to reverse. Both long and short setups include a protective stop.

    Suited for swing traders expecting oscillations, the strategy closes out once the RSI returns toward balance. Starting parameter `RsiPeriod` = 14.

    """

    def __init__(self):
        super(rsi_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Relative Strength Index period", "RSI Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Slope Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._threshold_multiplier = self.Param("ThresholdMultiplier", 2.0) \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entry threshold", "Slope Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._previous_rsi_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def slope_lookback(self):
        """Period for calculating slope statistics."""
        return self._slope_lookback.Value

    @slope_lookback.setter
    def slope_lookback(self, value):
        self._slope_lookback.Value = value

    @property
    def threshold_multiplier(self):
        """Threshold multiplier for standard deviation."""
        return self._threshold_multiplier.Value

    @threshold_multiplier.setter
    def threshold_multiplier(self, value):
        self._threshold_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(rsi_slope_mean_reversion_strategy, self).OnReseted()
        self._previous_rsi_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(rsi_slope_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate RSI slope only if we have previous RSI value
        if self._previous_rsi_value != 0:
            # Calculate current slope
            self._current_slope = rsi_value - self._previous_rsi_value

            # Update statistics
            self._slope_count += 1
            self._sum_slopes += self._current_slope

            # Update average slope
            if self._slope_count > 0:
                self._average_slope = self._sum_slopes / self._slope_count

            # Calculate sum of squared differences for std dev
            self._sum_squared_diff += (self._current_slope - self._average_slope) * (self._current_slope - self._average_slope)

            # Calculate standard deviation after we have enough samples
            if self._slope_count >= self.slope_lookback:
                self._slope_std_dev = Math.Sqrt(float(self._sum_squared_diff / self._slope_count))

                # Remove oldest slope value contribution (simple approximation)
                if self._slope_count > self.slope_lookback:
                    self._slope_count = self.slope_lookback
                    self._sum_slopes = self._average_slope * self.slope_lookback
                    self._sum_squared_diff = self._slope_std_dev * self._slope_std_dev * self.slope_lookback

                # Calculate entry thresholds
                lower_threshold = self._average_slope - self.threshold_multiplier * self._slope_std_dev
                upper_threshold = self._average_slope + self.threshold_multiplier * self._slope_std_dev

                # Trading logic
                if self._current_slope < lower_threshold and self.Position <= 0:
                    # Slope is below lower threshold (RSI falling rapidly) - mean reversion buy signal
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(
                        "BUY Signal: RSI Slope {0:F6} < Lower Threshold {1:F6}".format(
                            self._current_slope, lower_threshold)
                    )
                elif self._current_slope > upper_threshold and self.Position >= 0:
                    # Slope is above upper threshold (RSI rising rapidly) - mean reversion sell signal
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(
                        "SELL Signal: RSI Slope {0:F6} > Upper Threshold {1:F6}".format(
                            self._current_slope, upper_threshold)
                    )
                elif self._current_slope > self._average_slope and self.Position > 0:
                    # Exit long position when slope returns to average (profit target)
                    self.SellMarket(self.Position)
                    self.LogInfo(
                        "EXIT LONG: RSI Slope {0:F6} returned to average {1:F6}".format(
                            self._current_slope, self._average_slope)
                    )
                elif self._current_slope < self._average_slope and self.Position < 0:
                    # Exit short position when slope returns to average (profit target)
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo(
                        "EXIT SHORT: RSI Slope {0:F6} returned to average {1:F6}".format(
                            self._current_slope, self._average_slope)
                    )

        # Save current RSI value for next calculation
        self._previous_rsi_value = rsi_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_slope_mean_reversion_strategy()
