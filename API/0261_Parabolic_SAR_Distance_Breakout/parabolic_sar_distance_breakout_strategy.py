import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_distance_breakout_strategy(Strategy):
    """Strategy that enters positions when the distance between price and Parabolic SAR
    exceeds the average distance plus a multiple of standard deviation"""

    def __init__(self):
        super(parabolic_sar_distance_breakout_strategy, self).__init__()

        # Initial acceleration factor for Parabolic SAR
        self._acceleration = self.Param("Acceleration", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("Acceleration", "Initial acceleration factor for Parabolic SAR", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.05, 0.01)

        # Maximum acceleration factor for Parabolic SAR
        self._max_acceleration = self.Param("MaxAcceleration", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.5, 0.1)

        # Lookback period for distance statistics calculation
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for statistical calculations", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Standard deviation multiplier for breakout detection
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._parabolic_sar = None

        self._avg_distance_long = 0
        self._std_dev_distance_long = 0
        self._avg_distance_short = 0
        self._std_dev_distance_short = 0

        self._last_long_distance = 0
        self._last_short_distance = 0
        self._samples_count = 0

    # Initial acceleration factor for Parabolic SAR
    @property
    def Acceleration(self):
        return self._acceleration.Value

    @Acceleration.setter
    def Acceleration(self, value):
        self._acceleration.Value = value

    # Maximum acceleration factor for Parabolic SAR
    @property
    def MaxAcceleration(self):
        return self._max_acceleration.Value

    @MaxAcceleration.setter
    def MaxAcceleration(self, value):
        self._max_acceleration.Value = value

    # Lookback period for distance statistics calculation
    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    # Standard deviation multiplier for breakout detection
    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    # Candle type
    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = self.Acceleration
        self._parabolic_sar.AccelerationMax = self.MaxAcceleration

        self._avg_distance_long = 0
        self._std_dev_distance_long = 0
        self._avg_distance_short = 0
        self._std_dev_distance_short = 0
        self._last_long_distance = 0
        self._last_short_distance = 0
        self._samples_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._parabolic_sar, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolic_sar)
            self.DrawOwnTrades(area)

        # Set up position protection using the dynamic Parabolic SAR
        self.StartProtection(
            takeProfit=None,  # We'll handle exits via strategy logic
            stopLoss=None,    # The dynamic SAR will act as our stop
            isStopTrailing=True
        )

        super(parabolic_sar_distance_breakout_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, sar_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate distances
        long_distance = 0
        short_distance = 0

        # If SAR is below price, it's in uptrend
        if sar_value < candle.ClosePrice:
            long_distance = candle.ClosePrice - sar_value
        # If SAR is above price, it's in downtrend
        elif sar_value > candle.ClosePrice:
            short_distance = sar_value - candle.ClosePrice

        # Update statistics
        self.UpdateDistanceStatistics(long_distance, short_distance)

        # Trading logic
        if self._samples_count >= self.LookbackPeriod:
            # Long signal: distance exceeds average + k*stddev and we don't have a long position
            if long_distance > 0 and \
               long_distance > self._avg_distance_long + self.DeviationMultiplier * self._std_dev_distance_long and \
               self.Position <= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Long signal: Distance {0} > Avg {1} + {2}*StdDev {3}".format(
                    long_distance, self._avg_distance_long, self.DeviationMultiplier, self._std_dev_distance_long))
            # Short signal: distance exceeds average + k*stddev and we don't have a short position
            elif short_distance > 0 and \
                 short_distance > self._avg_distance_short + self.DeviationMultiplier * self._std_dev_distance_short and \
                 self.Position >= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Short signal: Distance {0} > Avg {1} + {2}*StdDev {3}".format(
                    short_distance, self._avg_distance_short, self.DeviationMultiplier, self._std_dev_distance_short))

            # Exit conditions - when price crosses SAR
            if self.Position > 0 and candle.ClosePrice < sar_value:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Price {0} crossed below SAR {1}".format(candle.ClosePrice, sar_value))
            elif self.Position < 0 and candle.ClosePrice > sar_value:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} crossed above SAR {1}".format(candle.ClosePrice, sar_value))

        # Store current distances for next update
        self._last_long_distance = long_distance
        self._last_short_distance = short_distance

    def UpdateDistanceStatistics(self, long_distance, short_distance):
        self._samples_count += 1

        # Simple calculation of running average and standard deviation
        if self._samples_count == 1:
            # Initialize with first values
            self._avg_distance_long = long_distance
            self._avg_distance_short = short_distance
            self._std_dev_distance_long = 0
            self._std_dev_distance_short = 0
        else:
            # Update running average
            old_avg_long = self._avg_distance_long
            old_avg_short = self._avg_distance_short

            self._avg_distance_long = old_avg_long + (long_distance - old_avg_long) / self._samples_count
            self._avg_distance_short = old_avg_short + (short_distance - old_avg_short) / self._samples_count

            # Update running standard deviation using Welford's algorithm
            if self._samples_count > 1:
                self._std_dev_distance_long = (1 - 1.0 / (self._samples_count - 1)) * self._std_dev_distance_long + \
                    self._samples_count * ((self._avg_distance_long - old_avg_long) * (self._avg_distance_long - old_avg_long))

                self._std_dev_distance_short = (1 - 1.0 / (self._samples_count - 1)) * self._std_dev_distance_short + \
                    self._samples_count * ((self._avg_distance_short - old_avg_short) * (self._avg_distance_short - old_avg_short))

            # We only need last LookbackPeriod samples
            if self._samples_count > self.LookbackPeriod:
                self._samples_count = self.LookbackPeriod

        # Calculate square root for final standard deviation
        self._std_dev_distance_long = Math.Sqrt(float(self._std_dev_distance_long) / self._samples_count)
        self._std_dev_distance_short = Math.Sqrt(float(self._std_dev_distance_short) / self._samples_count)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_distance_breakout_strategy()
