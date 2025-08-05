import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import (

    MovingAverageConvergenceDivergenceSignal,
    SimpleMovingAverage,
    StandardDeviation,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_volume_cluster_strategy(Strategy):
    """
    MACD with Volume Cluster strategy.
    Enters positions when MACD signal coincides with abnormal volume spike.

    """

    def __init__(self):
        super(macd_volume_cluster_strategy, self).__init__()

        # Initialize strategy.
        self._fast_macd_period = self.Param("FastMacdPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MACD Period", "Period for fast EMA in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 2)

        self._slow_macd_period = self.Param("SlowMacdPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MACD Period", "Period for slow EMA in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal Period", "Period for signal line in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Period", "Period for volume moving average calculation", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_deviation_factor = self.Param("VolumeDeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Deviation Factor", "Factor multiplied by standard deviation to detect volume spikes", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state variables
        self._avg_volume = 0
        self._volume_std_dev = 0
        self._processed_candles = 0

    @property
    def fast_macd_period(self):
        """Fast MACD EMA period."""
        return self._fast_macd_period.Value

    @fast_macd_period.setter
    def fast_macd_period(self, value):
        self._fast_macd_period.Value = value

    @property
    def slow_macd_period(self):
        """Slow MACD EMA period."""
        return self._slow_macd_period.Value

    @slow_macd_period.setter
    def slow_macd_period(self, value):
        self._slow_macd_period.Value = value

    @property
    def macd_signal_period(self):
        """MACD signal line period."""
        return self._macd_signal_period.Value

    @macd_signal_period.setter
    def macd_signal_period(self, value):
        self._macd_signal_period.Value = value

    @property
    def volume_period(self):
        """Period for volume average calculation."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_deviation_factor(self):
        """Volume deviation factor for volume spike detection."""
        return self._volume_deviation_factor.Value

    @volume_deviation_factor.setter
    def volume_deviation_factor(self, value):
        self._volume_deviation_factor.Value = value

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
        super(macd_volume_cluster_strategy, self).OnReseted()
        self._avg_volume = 0
        self._volume_std_dev = 0
        self._processed_candles = 0

    def OnReseted(self):
        super(macd_volume_cluster_strategy, self).OnReseted()
        self._avg_volume = 0
        self._volume_std_dev = 0
        self._processed_candles = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(macd_volume_cluster_strategy, self).OnStarted(time)

        # Create MACD indicator
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_macd_period
        macd.Macd.LongMa.Length = self.slow_macd_period
        macd.SignalMa.Length = self.macd_signal_period

        # Create volume-based indicators
        sma_volume = SimpleMovingAverage()
        sma_volume.Length = self.volume_period

        std_dev_volume = StandardDeviation()
        std_dev_volume.Length = self.volume_period

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind MACD and process volume separately
        subscription.BindEx(macd, self.ProcessMacdAndVolume).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessMacdAndVolume(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Calculate volume statistics
        self._processed_candles += 1

        # Using exponential moving average approach for volume statistics
        # to avoid keeping large arrays of historical volumes
        if self._processed_candles == 1:
            self._avg_volume = float(candle.TotalVolume)
            self._volume_std_dev = 0
        else:
            # Update average volume with smoothing factor
            alpha = 2.0 / (self.volume_period + 1)
            old_avg = self._avg_volume
            self._avg_volume = float(alpha * candle.TotalVolume + (1 - alpha) * self._avg_volume)

            # Update standard deviation (simplified approach)
            volume_dev = float(Math.Abs(candle.TotalVolume - old_avg))
            self._volume_std_dev = alpha * volume_dev + (1 - alpha) * self._volume_std_dev

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal

        # Determine if we have a volume spike
        is_volume_spike = candle.TotalVolume > (self._avg_volume + self.volume_deviation_factor * self._volume_std_dev)

        # Log the values
        self.LogInfo(
            f"MACD: {macd_line}, Signal: {signal_line}, Volume: {candle.TotalVolume}, " +
            f"Avg Volume: {self._avg_volume}, StdDev: {self._volume_std_dev}, Volume Spike: {is_volume_spike}"
        )

        # Trading logic
        if is_volume_spike:
            # Buy signal: MACD line crosses above signal line with volume spike
            if macd_line > signal_line and self.Position <= 0:
                # Close any existing short position
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))

                # Open long position
                self.BuyMarket(self.Volume)
                self.LogInfo(
                    f"Buy signal: MACD ({macd_line}) > Signal ({signal_line}) with volume spike ({candle.TotalVolume})"
                )
            # Sell signal: MACD line crosses below signal line with volume spike
            elif macd_line < signal_line and self.Position >= 0:
                # Close any existing long position
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))

                # Open short position
                self.SellMarket(self.Volume)
                self.LogInfo(
                    f"Sell signal: MACD ({macd_line}) < Signal ({signal_line}) with volume spike ({candle.TotalVolume})"
                )

        # Exit logic: MACD crosses back
        if (self.Position > 0 and macd_line < signal_line) or (
            self.Position < 0 and macd_line > signal_line
        ):
            self.ClosePosition()
            self.LogInfo(
                f"Exit signal: MACD and Signal crossed. Position closed at {candle.ClosePrice}"
            )

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_volume_cluster_strategy()