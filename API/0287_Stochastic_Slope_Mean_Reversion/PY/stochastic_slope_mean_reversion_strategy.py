import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, StochasticOscillatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class stochastic_slope_mean_reversion_strategy(Strategy):
    """
    Stochastic Slope Mean Reversion Strategy - strategy based on mean reversion of Stochastic %K slope.
    """

    def __init__(self):
        super(stochastic_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stoch Period", "Stochastic oscillator period", "Stochastic Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        self._stoch_k_period = self.Param("StochKPeriod", 3) \
            .SetDisplay("Stoch %K Period", "Stochastic %K smoothing period", "Stochastic Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetDisplay("Stoch %D Period", "Stochastic %D smoothing period", "Stochastic Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

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

        # Internal variables
        self._previous_stoch_k_value = 0
        self._current_slope = 0
        self._average_slope = 0
        self._slope_std_dev = 0
        self._slope_count = 0
        self._sum_slopes = 0
        self._sum_squared_diff = 0

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochKPeriod(self):
        """Stochastic %K period."""
        return self._stoch_k_period.Value

    @StochKPeriod.setter
    def StochKPeriod(self, value):
        self._stoch_k_period.Value = value

    @property
    def StochDPeriod(self):
        """Stochastic %D period."""
        return self._stoch_d_period.Value

    @StochDPeriod.setter
    def StochDPeriod(self, value):
        self._stoch_d_period.Value = value

    @property
    def SlopeLookback(self):
        """Period for calculating slope statistics."""
        return self._slope_lookback.Value

    @SlopeLookback.setter
    def SlopeLookback(self, value):
        self._slope_lookback.Value = value

    @property
    def ThresholdMultiplier(self):
        """Threshold multiplier for standard deviation."""
        return self._threshold_multiplier.Value

    @ThresholdMultiplier.setter
    def ThresholdMultiplier(self, value):
        self._threshold_multiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(stochastic_slope_mean_reversion_strategy, self).OnReseted()
        self._previous_stoch_k_value = 0
        self._current_slope = 0
        self._average_slope = 0
        self._slope_std_dev = 0
        self._slope_count = 0
        self._sum_slopes = 0
        self._sum_squared_diff = 0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_slope_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables

        # Create Stochastic indicator
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochKPeriod
        stochastic.D.Length = self.StochDPeriod

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if stoch_value.K is None:
            return

        stoch_k = float(stoch_value.K)

        # Calculate Stochastic %K slope only if we have previous %K value
        if self._previous_stoch_k_value != 0:
            # Calculate current slope
            self._current_slope = stoch_k - self._previous_stoch_k_value

            # Update statistics
            self._slope_count += 1
            self._sum_slopes += self._current_slope

            # Update average slope
            if self._slope_count > 0:
                self._average_slope = self._sum_slopes / self._slope_count

            # Calculate sum of squared differences for std dev
            self._sum_squared_diff += (self._current_slope - self._average_slope) * (self._current_slope - self._average_slope)

            # Calculate standard deviation after we have enough samples
            if self._slope_count >= self.SlopeLookback:
                self._slope_std_dev = Math.Sqrt(self._sum_squared_diff / self._slope_count)

                # Remove oldest slope value contribution (simple approximation)
                if self._slope_count > self.SlopeLookback:
                    self._slope_count = self.SlopeLookback
                    self._sum_slopes = self._average_slope * self.SlopeLookback
                    self._sum_squared_diff = self._slope_std_dev * self._slope_std_dev * self.SlopeLookback

                # Calculate entry thresholds
                lower_threshold = self._average_slope - self.ThresholdMultiplier * self._slope_std_dev
                upper_threshold = self._average_slope + self.ThresholdMultiplier * self._slope_std_dev

                # Trading logic
                if self._current_slope < lower_threshold and self.Position <= 0:
                    # Slope is below lower threshold (Stochastic %K falling rapidly) - mean reversion buy signal
                    self.BuyMarket(self.Volume + abs(self.Position))
                    self.LogInfo(
                        "BUY Signal: Stoch %K Slope {0:F6} < Lower Threshold {1:F6}".format(
                            self._current_slope, lower_threshold))
                elif self._current_slope > upper_threshold and self.Position >= 0:
                    # Slope is above upper threshold (Stochastic %K rising rapidly) - mean reversion sell signal
                    self.SellMarket(self.Volume + abs(self.Position))
                    self.LogInfo(
                        "SELL Signal: Stoch %K Slope {0:F6} > Upper Threshold {1:F6}".format(
                            self._current_slope, upper_threshold))
                elif self._current_slope > self._average_slope and self.Position > 0:
                    # Exit long position when slope returns to average (profit target)
                    self.SellMarket(self.Position)
                    self.LogInfo(
                        "EXIT LONG: Stoch %K Slope {0:F6} returned to average {1:F6}".format(
                            self._current_slope, self._average_slope))
                elif self._current_slope < self._average_slope and self.Position < 0:
                    # Exit short position when slope returns to average (profit target)
                    self.BuyMarket(abs(self.Position))
                    self.LogInfo(
                        "EXIT SHORT: Stoch %K Slope {0:F6} returned to average {1:F6}".format(
                            self._current_slope, self._average_slope))

        # Save current Stochastic %K value for next calculation
        self._previous_stoch_k_value = stoch_k

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_slope_mean_reversion_strategy()

