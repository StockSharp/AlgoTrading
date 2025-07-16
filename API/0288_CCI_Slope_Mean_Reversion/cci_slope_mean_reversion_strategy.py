import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class cci_slope_mean_reversion_strategy(Strategy):
    """CCI Slope Mean Reversion Strategy - strategy based on mean reversion of CCI slope.

    """

    def __init__(self):
        super(cci_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Commodity Channel Index period", "CCI Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 40, 5)

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

        # Internal state variables
        self._previous_cci_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

    # CCI period.
    @property
    def cci_period(self):
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    # Period for calculating slope statistics.
    @property
    def slope_lookback(self):
        return self._slope_lookback.Value

    @slope_lookback.setter
    def slope_lookback(self, value):
        self._slope_lookback.Value = value

    # Threshold multiplier for standard deviation.
    @property
    def threshold_multiplier(self):
        return self._threshold_multiplier.Value

    @threshold_multiplier.setter
    def threshold_multiplier(self, value):
        self._threshold_multiplier.Value = value

    # Stop-loss percentage.
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    # Candle type.
    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Initialize cci_slope_mean_reversion_strategy."""
        super(cci_slope_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables
        self._previous_cci_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

        # Create CCI indicator
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate CCI slope only if we have previous CCI value
        if self._previous_cci_value != 0:
            # Calculate current slope
            self._current_slope = cci_value - self._previous_cci_value

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
                self._slope_std_dev = Math.Sqrt(self._sum_squared_diff / self._slope_count)

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
                    # Slope is below lower threshold (CCI falling rapidly) - mean reversion buy signal
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("BUY Signal: CCI Slope {0:F6} < Lower Threshold {1:F6}".format(
                        self._current_slope, lower_threshold))
                elif self._current_slope > upper_threshold and self.Position >= 0:
                    # Slope is above upper threshold (CCI rising rapidly) - mean reversion sell signal
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("SELL Signal: CCI Slope {0:F6} > Upper Threshold {1:F6}".format(
                        self._current_slope, upper_threshold))
                elif self._current_slope > self._average_slope and self.Position > 0:
                    # Exit long position when slope returns to average (profit target)
                    self.SellMarket(self.Position)
                    self.LogInfo("EXIT LONG: CCI Slope {0:F6} returned to average {1:F6}".format(
                        self._current_slope, self._average_slope))
                elif self._current_slope < self._average_slope and self.Position < 0:
                    # Exit short position when slope returns to average (profit target)
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("EXIT SHORT: CCI Slope {0:F6} returned to average {1:F6}".format(
                        self._current_slope, self._average_slope))

        # Save current CCI value for next calculation
        self._previous_cci_value = cci_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return cci_slope_mean_reversion_strategy()
