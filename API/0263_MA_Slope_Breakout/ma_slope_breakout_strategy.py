import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ma_slope_breakout_strategy(Strategy):
    """
    Strategy based on Moving Average Slope breakout
    Enters positions when the slope of MA exceeds average slope plus a multiple of standard deviation
    """

    def __init__(self):
        super(ma_slope_breakout_strategy, self).__init__()

        # Moving Average length
        self._ma_length = self.Param("MaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Period for Moving Average", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Lookback period for slope statistics calculation
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Standard deviation multiplier for breakout detection
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop loss percentage
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        # Candle type
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._sma = None
        self._prev_ma_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = []
        self._current_index = 0
        self._is_initialized = False

    @property
    def ma_length(self):
        """Moving Average length"""
        return self._ma_length.Value

    @ma_length.setter
    def ma_length(self, value):
        self._ma_length.Value = value

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

    def OnStarted(self, time):
        """Initialize indicators, chart, and position protection."""
        super(ma_slope_breakout_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.ma_length

        self._prev_ma_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = [0.0] * self.lookback_period
        self._current_index = 0
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

        # Set up position protection
        self.StartProtection(
            None,  # we'll handle take-profit separately
            Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ma_value):
        """Process new candle values and generate trading signals."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if indicator is formed
        if not self._sma.IsFormed:
            return

        # Calculate the slope
        if not self._is_initialized:
            self._prev_ma_value = ma_value
            self._is_initialized = True
            return

        # Calculate current slope (simple difference for now)
        self._current_slope = ma_value - self._prev_ma_value

        # Store slope in array and update index
        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % self.lookback_period

        # Calculate statistics once we have enough data
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.calculate_statistics()

        # Trading logic
        if Math.Abs(self._avg_slope) > 0:
            # Long signal: slope exceeds average + k*stddev (slope is positive and we don't have a long position)
            if (self._current_slope > 0 and
                self._current_slope > self._avg_slope + self.deviation_multiplier * self._std_dev_slope and
                self.Position <= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Long signal: Slope {0} > Avg {1} + {2}*StdDev {3}".format(
                    self._current_slope, self._avg_slope, self.deviation_multiplier, self._std_dev_slope))
            # Short signal: slope exceeds average + k*stddev in negative direction (slope is negative and we don't have a short position)
            elif (self._current_slope < 0 and
                  self._current_slope < self._avg_slope - self.deviation_multiplier * self._std_dev_slope and
                  self.Position >= 0):
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Short signal: Slope {0} < Avg {1} - {2}*StdDev {3}".format(
                    self._current_slope, self._avg_slope, self.deviation_multiplier, self._std_dev_slope))

            # Exit conditions - when slope returns to average
            if self.Position > 0 and self._current_slope < self._avg_slope:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Slope {0} < Avg {1}".format(self._current_slope, self._avg_slope))
            elif self.Position < 0 and self._current_slope > self._avg_slope:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Slope {0} > Avg {1}".format(self._current_slope, self._avg_slope))

        # Store current MA value for next slope calculation
        self._prev_ma_value = ma_value

    def calculate_statistics(self):
        """Calculate average slope and standard deviation."""
        self._avg_slope = 0.0
        sum_squared_diffs = 0.0

        for i in range(self.lookback_period):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= self.lookback_period

        for i in range(self.lookback_period):
            diff = self._slopes[i] - self._avg_slope
            sum_squared_diffs += diff * diff

        self._std_dev_slope = Math.Sqrt(sum_squared_diffs / self.lookback_period)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_slope_breakout_strategy()
