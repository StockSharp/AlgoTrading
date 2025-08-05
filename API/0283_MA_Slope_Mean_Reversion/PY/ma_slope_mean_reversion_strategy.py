import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ma_slope_mean_reversion_strategy(Strategy):
    """MA Slope Mean Reversion Strategy - strategy based on mean reversion of moving average slope."""

    def __init__(self):
        super(ma_slope_mean_reversion_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving Average period", "MA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

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

        self._previous_ma_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

    @property
    def MaPeriod(self):
        """MA period."""
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

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

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ma_slope_mean_reversion_strategy, self).OnReseted()
        self._previous_ma_value = 0.0
        self._current_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._slope_count = 0
        self._sum_slopes = 0.0
        self._sum_squared_diff = 0.0

    def OnStarted(self, time):
        super(ma_slope_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables

        # Create MA indicator
        ma = SimpleMovingAverage()
        ma.Length = self.MaPeriod

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma_float = float(ma_value)
        if self._previous_ma_value != 0:
            # Calculate current slope
            self._current_slope = ma_float - self._previous_ma_value

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
                self._slope_std_dev = Math.Sqrt(float(self._sum_squared_diff / self._slope_count))

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
                    # Slope is below lower threshold (falling rapidly) - mean reversion buy signal
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(f"BUY Signal: Slope {self._current_slope:F6} < Lower Threshold {lower_threshold:F6}")
                elif self._current_slope > upper_threshold and self.Position >= 0:
                    # Slope is above upper threshold (rising rapidly) - mean reversion sell signal
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(f"SELL Signal: Slope {self._current_slope:F6} > Upper Threshold {upper_threshold:F6}")
                elif self._current_slope > self._average_slope and self.Position > 0:
                    # Exit long position when slope returns to average (profit target)
                    self.SellMarket(self.Position)
                    self.LogInfo(f"EXIT LONG: Slope {self._current_slope:F6} returned to average {self._average_slope:F6}")
                elif self._current_slope < self._average_slope and self.Position < 0:
                    # Exit short position when slope returns to average (profit target)
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo(f"EXIT SHORT: Slope {self._current_slope:F6} returned to average {self._average_slope:F6}")

        # Save current MA value for next calculation
        self._previous_ma_value = ma_float

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_slope_mean_reversion_strategy()
