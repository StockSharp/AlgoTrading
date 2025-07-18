import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import (

    MovingAverageConvergenceDivergenceSignal,
    SimpleMovingAverage,
    StandardDeviation,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class macd_adaptive_histogram_strategy(Strategy):
    """Strategy based on MACD with adaptive histogram threshold."""

    def __init__(self):
        super(macd_adaptive_histogram_strategy, self).__init__()

        # Initialize strategy parameters
        self._fast_period = (
            self.Param("FastPeriod", 12)
            .SetGreaterThanZero()
            .SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(8, 16, 2)
        )

        self._slow_period = (
            self.Param("SlowPeriod", 26)
            .SetGreaterThanZero()
            .SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(20, 32, 3)
        )

        self._signal_period = (
            self.Param("SignalPeriod", 9)
            .SetGreaterThanZero()
            .SetDisplay("Signal Period", "Signal line period for MACD", "MACD Settings")
            .SetCanOptimize(True)
            .SetOptimize(7, 12, 1)
        )

        self._histogram_avg_period = (
            self.Param("HistogramAvgPeriod", 20)
            .SetGreaterThanZero()
            .SetDisplay(
                "Histogram Avg Period",
                "Period for histogram average calculation",
                "Strategy Settings",
            )
            .SetCanOptimize(True)
            .SetOptimize(10, 30, 5)
        )

        self._std_dev_multiplier = (
            self.Param("StdDevMultiplier", 2.0)
            .SetGreaterThanZero()
            .SetDisplay(
                "StdDev Multiplier",
                "Standard deviation multiplier for histogram threshold",
                "Strategy Settings",
            )
            .SetCanOptimize(True)
            .SetOptimize(1.0, 3.0, 0.5)
        )

        self._stop_loss_percent = (
            self.Param("StopLossPercent", 2.0)
            .SetGreaterThanZero()
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings")
            .SetCanOptimize(True)
            .SetOptimize(1.0, 3.0, 0.5)
        )

        self._candle_type = (
            self.Param("CandleType", tf(15))
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")
        )

        # Indicators for the histogram statistics
        self._hist_avg = None
        self._hist_std_dev = None

    @property
    def FastPeriod(self):
        """Fast EMA period for MACD."""
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        """Slow EMA period for MACD."""
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def SignalPeriod(self):
        """Signal line period for MACD."""
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    @property
    def HistogramAvgPeriod(self):
        """Period for histogram average and standard deviation calculation."""
        return self._histogram_avg_period.Value

    @HistogramAvgPeriod.setter
    def HistogramAvgPeriod(self, value):
        self._histogram_avg_period.Value = value

    @property
    def StdDevMultiplier(self):
        """Standard deviation multiplier for histogram threshold."""
        return self._std_dev_multiplier.Value

    @StdDevMultiplier.setter
    def StdDevMultiplier(self, value):
        self._std_dev_multiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(macd_adaptive_histogram_strategy, self).OnStarted(time)

        # Create MACD indicator with custom settings
        macd_line = MovingAverageConvergenceDivergenceSignal()
        macd_line.Macd.ShortMa.Length = self.FastPeriod
        macd_line.Macd.LongMa.Length = self.SlowPeriod
        macd_line.SignalMa.Length = self.SignalPeriod

        # Create indicators for the histogram statistics
        self._hist_avg = SimpleMovingAverage()
        self._hist_avg.Length = self.HistogramAvgPeriod
        self._hist_std_dev = StandardDeviation()
        self._hist_std_dev.Length = self.HistogramAvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind MACD to subscription
        subscription.BindEx(macd_line, self.ProcessCandle).Start()

        # Enable position protection with percentage stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd_line)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value):
        """Process candle and execute trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        # Extract MACD values
        macd = macd_value.Macd
        signal = macd_value.Signal
        histogram = macd - signal

        # Process the histogram through the statistics indicators
        hist_avg_value = float(
            process_float(
                self._hist_avg,
                histogram,
                macd_value.Time,
                macd_value.IsFinal,
            )
        )
        hist_std_dev_value = float(
            process_float(
                self._hist_std_dev,
                histogram,
                macd_value.Time,
                macd_value.IsFinal,
            )
        )

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate adaptive thresholds for histogram
        upper_threshold = hist_avg_value + self.StdDevMultiplier * hist_std_dev_value
        lower_threshold = hist_avg_value - self.StdDevMultiplier * hist_std_dev_value

        # Define entry conditions with adaptive thresholds
        long_entry_condition = histogram > upper_threshold and self.Position <= 0
        short_entry_condition = histogram < lower_threshold and self.Position >= 0

        # Define exit conditions
        long_exit_condition = histogram < 0 and self.Position > 0
        short_exit_condition = histogram > 0 and self.Position < 0

        # Log current values
        self.LogInfo(
            "Candle: {0}, Close: {1}, MACD: {2}, Signal: {3}, Histogram: {4}".format(
                candle.OpenTime, candle.ClosePrice, macd, signal, histogram
            )
        )
        self.LogInfo(
            "Hist Avg: {0}, Hist StdDev: {1}, Upper: {2}, Lower: {3}".format(
                hist_avg_value, hist_std_dev_value, upper_threshold, lower_threshold
            )
        )

        # Execute trading logic
        if long_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)
            # Enter long position
            self.BuyMarket(position_size)
            self.LogInfo(
                "Long entry: Price={0}, Histogram={1}, Threshold={2}".format(
                    candle.ClosePrice, histogram, upper_threshold
                )
            )
        elif short_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)
            # Enter short position
            self.SellMarket(position_size)
            self.LogInfo(
                "Short entry: Price={0}, Histogram={1}, Threshold={2}".format(
                    candle.ClosePrice, histogram, lower_threshold
                )
            )
        elif long_exit_condition:
            # Exit long position
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Long exit: Price={0}, Histogram={1}".format(candle.ClosePrice, histogram)
            )
        elif short_exit_condition:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Short exit: Price={0}, Histogram={1}".format(candle.ClosePrice, histogram)
            )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_adaptive_histogram_strategy()