import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_volume_strategy(Strategy):
    """
    Strategy combining MACD (Moving Average Convergence Divergence) with volume confirmation.
    Enters positions when MACD line crosses the Signal line and confirms with increased volume.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(macd_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period of MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 4)

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period of MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 32, 4)

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal line period of MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 13, 4)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetDisplay("Volume Period", "Period for volume averaging", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5) \
            .SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm entry", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 2.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # State variables
        self._prev_macd = None
        self._prev_signal = None
        self._avg_volume = 0

    @property
    def macd_fast(self):
        """MACD fast EMA period."""
        return self._macd_fast.Value

    @macd_fast.setter
    def macd_fast(self, value):
        self._macd_fast.Value = value

    @property
    def macd_slow(self):
        """MACD slow EMA period."""
        return self._macd_slow.Value

    @macd_slow.setter
    def macd_slow(self, value):
        self._macd_slow.Value = value

    @property
    def macd_signal(self):
        """MACD signal line period."""
        return self._macd_signal.Value

    @macd_signal.setter
    def macd_signal(self, value):
        self._macd_signal.Value = value

    @property
    def volume_period(self):
        """Volume averaging period."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_multiplier(self):
        """Volume multiplier for confirmation."""
        return self._volume_multiplier.Value

    @volume_multiplier.setter
    def volume_multiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(macd_volume_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_signal = None
        self._avg_volume = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(macd_volume_strategy, self).OnStarted(time)

        # Initialize variables
        self._prev_macd = None
        self._prev_signal = None
        self._avg_volume = 0

        # Create indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal

        volume_avg = SimpleMovingAverage()
        volume_avg.Length = self.volume_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind volume average indicator separately to update volume average
        subscription.BindEx(volume_avg, macd, self.ProcessIndicators).Start()

        # Setup position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.stop_loss_percent, UnitTypes.Percent)  # Percentage-based stop loss
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, volume_avg_value, macd_value):
        """
        Process MACD indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if volume_avg_value.IsFinal:
            self._avg_volume = float(volume_avg_value)

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading() or self._avg_volume <= 0:
            return

        macd_typed = macd_value
        macd_line = macd_typed.Macd
        signal_line = macd_typed.Signal

        # Check if we have previous values to compare
        if self._prev_macd is not None and self._prev_signal is not None:
            # Detect MACD crossover signals
            macd_crossed_above_signal = self._prev_macd < self._prev_signal and macd_line > signal_line
            macd_crossed_below_signal = self._prev_macd > self._prev_signal and macd_line < signal_line

            # Check volume confirmation
            is_volume_high_enough = candle.TotalVolume > self._avg_volume * self.volume_multiplier

            if is_volume_high_enough:
                # Long entry: MACD crosses above Signal with increased volume
                if macd_crossed_above_signal and self.Position <= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.BuyMarket(volume)
                # Short entry: MACD crosses below Signal with increased volume
                elif macd_crossed_below_signal and self.Position >= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.SellMarket(volume)

        # Exit logic - when MACD crosses back
        if self.Position > 0 and macd_line < signal_line:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and macd_line > signal_line:
            self.BuyMarket(Math.Abs(self.Position))

        # Update previous values
        self._prev_macd = macd_line
        self._prev_signal = signal_line

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_volume_strategy()
