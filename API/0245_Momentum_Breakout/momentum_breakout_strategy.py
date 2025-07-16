import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class momentum_breakout_strategy(Strategy):
    """
    Momentum Breakout Strategy (245).
    Enter when momentum breaks out above/below its average by a certain multiple of standard deviation.
    Exit when momentum returns to its average.
    """

    def __init__(self):
        super(momentum_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for momentum calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for momentum average calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        # Indicators
        self._momentum = None
        self._momentum_average = None
        self._momentum_stddev = None

        self._current_momentum = None
        self._momentum_avg_value = None
        self._momentum_stddev_value = None

    @property
    def momentum_period(self):
        """Momentum period."""
        return self._momentum_period.Value

    @momentum_period.setter
    def momentum_period(self, value):
        self._momentum_period.Value = value

    @property
    def average_period(self):
        """Period for momentum average calculation."""
        return self._average_period.Value

    @average_period.setter
    def average_period(self, value):
        self._average_period.Value = value

    @property
    def multiplier(self):
        """Standard deviation multiplier for entry."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(momentum_breakout_strategy, self).OnStarted(time)

        self._current_momentum = None
        self._momentum_avg_value = None
        self._momentum_stddev_value = None

        # Create indicators
        self._momentum = Momentum()
        self._momentum.Length = self.momentum_period
        self._momentum_average = SimpleMovingAverage()
        self._momentum_average.Length = self.average_period
        self._momentum_stddev = StandardDeviation()
        self._momentum_stddev.Length = self.average_period

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Create processing chain
        subscription.Bind(self._momentum, self.ProcessMomentum).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._momentum)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            Unit(5, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessMomentum(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        # Store the current momentum value
        self._current_momentum = momentum_value

        # Process momentum through average and standard deviation indicators
        avg_val = self._momentum_average.Process(momentum_value, candle.ServerTime, candle.State == CandleStates.Finished)
        std_val = self._momentum_stddev.Process(momentum_value, candle.ServerTime, candle.State == CandleStates.Finished)

        self._momentum_avg_value = avg_val.ToDecimal()
        self._momentum_stddev_value = std_val.ToDecimal()

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._momentum_average.IsFormed or not self._momentum_stddev.IsFormed:
            return

        # Ensure we have all needed values
        if self._current_momentum is None or self._momentum_avg_value is None or self._momentum_stddev_value is None:
            return

        # Calculate bands
        upper_band = self._momentum_avg_value + self.multiplier * self._momentum_stddev_value
        lower_band = self._momentum_avg_value - self.multiplier * self._momentum_stddev_value

        self.LogInfo("Momentum: {0}, Avg: {1}, Upper: {2}, Lower: {3}".format(
            self._current_momentum, self._momentum_avg_value, upper_band, lower_band))

        # Entry logic - BREAKOUT (not mean reversion)
        if self.Position == 0:
            # Long Entry: Momentum breaks above upper band (strong upward momentum)
            if self._current_momentum > upper_band:
                self.LogInfo("Buy Signal - Momentum ({0}) > Upper Band ({1})".format(self._current_momentum, upper_band))
                self.BuyMarket(self.Volume)
            # Short Entry: Momentum breaks below lower band (strong downward momentum)
            elif self._current_momentum < lower_band:
                self.LogInfo("Sell Signal - Momentum ({0}) < Lower Band ({1})".format(self._current_momentum, lower_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and self._current_momentum < self._momentum_avg_value:
            # Exit Long: Momentum returned to average
            self.LogInfo("Exit Long - Momentum ({0}) < Avg ({1})".format(self._current_momentum, self._momentum_avg_value))
            self.SellMarket(abs(self.Position))
        elif self.Position < 0 and self._current_momentum > self._momentum_avg_value:
            # Exit Short: Momentum returned to average
            self.LogInfo("Exit Short - Momentum ({0}) > Avg ({1})".format(self._current_momentum, self._momentum_avg_value))
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return momentum_breakout_strategy()
