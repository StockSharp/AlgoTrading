import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class donchian_width_mean_reversion_strategy(Strategy):
    """
    Donchian Width Mean Reversion Strategy.
    This strategy trades based on the mean reversion of the Donchian Channel width.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(donchian_width_mean_reversion_strategy, self).__init__()

        # Constructor.
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Donchian Channel period", "Donchian") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of width", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # Indicators
        self._donchian = None
        self._width_average = None
        self._width_std_dev = None

        # Internal state
        self._current_width = 0.0
        self._prev_width = 0.0
        self._prev_width_average = 0.0
        self._prev_width_std_dev = 0.0

    @property
    def donchian_period(self):
        """Donchian Channel period."""
        return self._donchian_period.Value

    @donchian_period.setter
    def donchian_period(self, value):
        self._donchian_period.Value = value

    @property
    def lookback_period(self):
        """Lookback period for calculating the average and standard deviation of width."""
        return self._lookback_period.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period.Value = value

    @property
    def deviation_multiplier(self):
        """Deviation multiplier for mean reversion detection."""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
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

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(donchian_width_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._donchian = DonchianChannels()
        self._donchian.Length = self.donchian_period
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.lookback_period
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self.lookback_period

        # Reset stored values
        self._current_width = 0.0
        self._prev_width = 0.0
        self._prev_width_average = 0.0
        self._prev_width_std_dev = 0.0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._donchian, self.ProcessDonchian).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

    def ProcessDonchian(self, candle, donchian_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract upper and lower bands from the indicator value
        try:
            upper_band = float(donchian_value.UpperBand)
            lower_band = float(donchian_value.LowerBand)
        except Exception:
            return  # Not enough data to calculate bands

        # Calculate the Donchian channel width
        self._current_width = upper_band - lower_band

        # Calculate the average and standard deviation of the width
        width_average = to_float(self._width_average.Process(self._current_width, candle.ServerTime, True))
        width_std_dev = to_float(self._width_std_dev.Process(self._current_width, candle.ServerTime, True))

        # Skip the first value
        if self._prev_width == 0:
            self._prev_width = self._current_width
            self._prev_width_average = width_average
            self._prev_width_std_dev = width_std_dev
            return

        # Calculate thresholds
        narrow_threshold = self._prev_width_average - self._prev_width_std_dev * self.deviation_multiplier
        wide_threshold = self._prev_width_average + self._prev_width_std_dev * self.deviation_multiplier

        # Trading logic:
        # When channel is narrowing (compression), enter long position
        if self._current_width < narrow_threshold and self._prev_width >= narrow_threshold and self.Position == 0:
            self.BuyMarket(self.Volume)
            self.LogInfo("Donchian channel width compression: {0} < {1}. Buying at {2}".format(
                self._current_width, narrow_threshold, candle.ClosePrice))
        # When channel is widening (expansion), enter short position
        elif self._current_width > wide_threshold and self._prev_width <= wide_threshold and self.Position == 0:
            self.SellMarket(self.Volume)
            self.LogInfo("Donchian channel width expansion: {0} > {1}. Selling at {2}".format(
                self._current_width, wide_threshold, candle.ClosePrice))
        # Exit positions when width returns to average
        elif ((self.Position > 0 or self.Position < 0) and
              abs(self._current_width - self._prev_width_average) < 0.1 * self._prev_width_std_dev and
              abs(self._prev_width - self._prev_width_average) >= 0.1 * self._prev_width_std_dev):
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
                self.LogInfo("Donchian width returned to average: {0} \u2248 {1}. Closing long position at {2}".format(
                    self._current_width, self._prev_width_average, candle.ClosePrice))
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
                self.LogInfo("Donchian width returned to average: {0} \u2248 {1}. Closing short position at {2}".format(
                    self._current_width, self._prev_width_average, candle.ClosePrice))

        # Store current values for next comparison
        self._prev_width = self._current_width
        self._prev_width_average = width_average
        self._prev_width_std_dev = width_std_dev

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return donchian_width_mean_reversion_strategy()
