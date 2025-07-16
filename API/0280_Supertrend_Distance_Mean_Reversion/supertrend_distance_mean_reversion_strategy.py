import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SuperTrend, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class supertrend_distance_mean_reversion_strategy(Strategy):
    """Supertrend Distance Mean Reversion Strategy.
    This strategy trades based on the mean reversion of the distance between price and Supertrend indicator.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(supertrend_distance_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR period for Supertrend calculation", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

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
        self._atr = None
        self._supertrend = None
        self._distance_average = None
        self._distance_std_dev = None

        # Internal state variables
        self._current_distance_long = 0.0
        self._current_distance_short = 0.0
        self._prev_distance_long = 0.0
        self._prev_distance_short = 0.0
        self._prev_distance_avg_long = 0.0
        self._prev_distance_avg_short = 0.0
        self._prev_distance_std_dev_long = 0.0
        self._prev_distance_std_dev_short = 0.0
        self._supertrend_value = 0.0

    # ATR period for Supertrend calculation.
    @property
    def atr_period(self):
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    # Multiplier for Supertrend calculation.
    @property
    def multiplier(self):
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    # Lookback period for calculating the average and standard deviation of distance.
    @property
    def lookback_period(self):
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    # Deviation multiplier for mean reversion detection.
    @property
    def deviation_multiplier(self):
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    # Candle type.
    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(supertrend_distance_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        self._supertrend = SuperTrend()
        self._supertrend.Length = self.atr_period
        self._supertrend.Multiplier = self.multiplier

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
        self._supertrend_value = 0.0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._supertrend, self.ProcessSupertrend).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

    def ProcessSupertrend(self, candle, supertrend_value):
        """Process Supertrend indicator value with corresponding candle."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get the Supertrend value
        self._supertrend_value = float(supertrend_value)

        # Calculate distances
        self._current_distance_long = candle.ClosePrice - self._supertrend_value
        self._current_distance_short = self._supertrend_value - candle.ClosePrice

        # Calculate averages and standard deviations for both distances
        long_distance_avg = float(self._distance_average.Process(self._current_distance_long, candle.ServerTime, candle.State == CandleStates.Finished))
        long_distance_std_dev = float(self._distance_std_dev.Process(self._current_distance_long, candle.ServerTime, candle.State == CandleStates.Finished))

        short_distance_avg = float(self._distance_average.Process(self._current_distance_short, candle.ServerTime, candle.State == CandleStates.Finished))
        short_distance_std_dev = float(self._distance_std_dev.Process(self._current_distance_short, candle.ServerTime, candle.State == CandleStates.Finished))

        # Skip the first value
        if self._prev_distance_long == 0 or self._prev_distance_short == 0:
            self._prev_distance_long = self._current_distance_long
            self._prev_distance_short = self._current_distance_short
            self._prev_distance_avg_long = long_distance_avg
            self._prev_distance_avg_short = short_distance_avg
            self._prev_distance_std_dev_long = long_distance_std_dev
            self._prev_distance_std_dev_short = short_distance_std_dev
            return

        # Calculate thresholds for long position
        long_distance_extended_threshold = self._prev_distance_avg_long + self._prev_distance_std_dev_long * self.deviation_multiplier

        # Calculate thresholds for short position
        short_distance_extended_threshold = self._prev_distance_avg_short + self._prev_distance_std_dev_short * self.deviation_multiplier

        # Trading logic:
        # For long positions - when price is far above Supertrend (mean reversion to downside)
        if (self._current_distance_long > long_distance_extended_threshold and
                self._prev_distance_long <= long_distance_extended_threshold and
                self.Position >= 0 and candle.ClosePrice > self._supertrend_value):
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                f"Long distance extended: {self._current_distance_long} > {long_distance_extended_threshold}. Selling at {candle.ClosePrice}")
        # For short positions - when price is far below Supertrend (mean reversion to upside)
        elif (self._current_distance_short > short_distance_extended_threshold and
              self._prev_distance_short <= short_distance_extended_threshold and
              self.Position <= 0 and candle.ClosePrice < self._supertrend_value):
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                f"Short distance extended: {self._current_distance_short} > {short_distance_extended_threshold}. Buying at {candle.ClosePrice}")
        # Exit positions when distance returns to average
        elif self.Position < 0 and self._current_distance_short < self._prev_distance_avg_short and self._prev_distance_short >= self._prev_distance_avg_short:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                f"Short distance returned to average: {self._current_distance_short} < {self._prev_distance_avg_short}. Closing short position at {candle.ClosePrice}")
        elif self.Position > 0 and self._current_distance_long < self._prev_distance_avg_long and self._prev_distance_long >= self._prev_distance_avg_long:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                f"Long distance returned to average: {self._current_distance_long} < {self._prev_distance_avg_long}. Closing long position at {candle.ClosePrice}")
        # Use Supertrend as dynamic stop
        elif ((self.Position > 0 and candle.ClosePrice < self._supertrend_value) or
              (self.Position < 0 and candle.ClosePrice > self._supertrend_value)):
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(
                    f"Price crossed below Supertrend: {candle.ClosePrice} < {self._supertrend_value}. Closing long position at {candle.ClosePrice}")
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(
                    f"Price crossed above Supertrend: {candle.ClosePrice} > {self._supertrend_value}. Closing short position at {candle.ClosePrice}")

        # Store current values for next comparison
        self._prev_distance_long = self._current_distance_long
        self._prev_distance_short = self._current_distance_short
        self._prev_distance_avg_long = long_distance_avg
        self._prev_distance_avg_short = short_distance_avg
        self._prev_distance_std_dev_long = long_distance_std_dev
        self._prev_distance_std_dev_short = short_distance_std_dev

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_distance_mean_reversion_strategy()
