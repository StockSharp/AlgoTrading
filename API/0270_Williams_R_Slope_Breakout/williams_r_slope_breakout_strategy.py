import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, LinearRegression
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *
from collections import deque


class williams_r_slope_breakout_strategy(Strategy):
    """Williams %R Slope Breakout Strategy"""

    def __init__(self):
        super(williams_r_slope_breakout_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope average and standard deviation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._williams_r = None
        self._williams_r_slope = None
        self._prev_slope_value = 0
        self._slope_avg = 0
        self._slope_std_dev = 0
        self._sum_slope = 0
        self._sum_slope_squared = 0
        self._slope_values = deque()

    # Properties
    @property
    def williams_r_period(self):
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    @property
    def slope_period(self):
        return self._slope_period.Value

    @slope_period.setter
    def slope_period(self, value):
        self._slope_period.Value = value

    @property
    def breakout_multiplier(self):
        return self._breakout_multiplier.Value

    @breakout_multiplier.setter
    def breakout_multiplier(self, value):
        self._breakout_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(williams_r_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._williams_r = WilliamsR()
        self._williams_r.Length = self.williams_r_period
        self._williams_r_slope = LinearRegression()
        self._williams_r_slope.Length = 2  # For calculating slope

        self._prev_slope_value = 0
        self._slope_avg = 0
        self._slope_std_dev = 0
        self._sum_slope = 0
        self._sum_slope_squared = 0
        self._slope_values.clear()

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._williams_r, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williams_r)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

    def ProcessCandle(self, candle, williams_r_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Williams %R slope
        current_slope_typed = process_float(self._williams_r_slope, williams_r_value, candle.ServerTime, candle.State == CandleStates.Finished)
        if not hasattr(current_slope_typed, 'LinearReg'):
            return  # Skip if slope is not available
        current_slope_value = current_slope_typed.LinearReg

        # Update slope stats when we have 2 values to calculate slope
        if self._prev_slope_value != 0:
            # Calculate simple slope from current and previous values
            slope = current_slope_value - self._prev_slope_value

            # Update running statistics
            self._slope_values.append(slope)
            self._sum_slope += slope
            self._sum_slope_squared += slope * slope

            # Remove oldest value if we have enough
            if len(self._slope_values) > self.slope_period:
                old_slope = self._slope_values.popleft()
                self._sum_slope -= old_slope
                self._sum_slope_squared -= old_slope * old_slope

            # Calculate average and standard deviation
            self._slope_avg = self._sum_slope / len(self._slope_values)
            variance = (self._sum_slope_squared / len(self._slope_values)) - (self._slope_avg * self._slope_avg)
            self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(float(variance))

            # Generate signals if we have enough data for statistics
            if len(self._slope_values) >= self.slope_period:
                # Breakout logic (Note: Williams %R is inverted, positive slope = bullish)
                if slope > self._slope_avg + self.breakout_multiplier * self._slope_std_dev and self.Position <= 0:
                    # Long position on bullish slope breakout
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(
                        f"Long entry: Williams %R slope breakout above {self._slope_avg + self.breakout_multiplier * self._slope_std_dev:F2}")
                elif slope < self._slope_avg - self.breakout_multiplier * self._slope_std_dev and self.Position >= 0:
                    # Short position on bearish slope breakout
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(
                        f"Short entry: Williams %R slope breakout below {self._slope_avg - self.breakout_multiplier * self._slope_std_dev:F2}")

                # Exit logic - Return to mean
                if self.Position > 0 and slope < self._slope_avg:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Long exit: Williams %R slope returned to mean")
                elif self.Position < 0 and slope > self._slope_avg:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Short exit: Williams %R slope returned to mean")

        # Update previous value for next iteration
        self._prev_slope_value = current_slope_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_r_slope_breakout_strategy()
