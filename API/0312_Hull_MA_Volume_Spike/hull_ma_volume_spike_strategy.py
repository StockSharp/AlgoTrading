import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class hull_ma_volume_spike_strategy(Strategy):
    """Strategy based on Hull Moving Average with Volume Spike detection."""

    def __init__(self):
        super(hull_ma_volume_spike_strategy, self).__init__()

        # Hull Moving Average period parameter.
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        # Volume average period parameter.
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Volume threshold factor parameter.
        self._volume_threshold_factor = self.Param("VolumeThresholdFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Threshold Factor", "Factor for volume spike detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Store previous HMA value to detect direction changes
        self._prev_hma_value = 0.0

    @property
    def hma_period(self):
        """Hull Moving Average period parameter."""
        return self._hma_period.Value

    @hma_period.setter
    def hma_period(self, value):
        self._hma_period.Value = value

    @property
    def volume_avg_period(self):
        """Volume average period parameter."""
        return self._volume_avg_period.Value

    @volume_avg_period.setter
    def volume_avg_period(self, value):
        self._volume_avg_period.Value = value

    @property
    def volume_threshold_factor(self):
        """Volume threshold factor parameter."""
        return self._volume_threshold_factor.Value

    @volume_threshold_factor.setter
    def volume_threshold_factor(self, value):
        self._volume_threshold_factor.Value = value

    @property
    def candle_type(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(hull_ma_volume_spike_strategy, self).OnReseted()
        self._prev_hma_value = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(hull_ma_volume_spike_strategy, self).OnStarted(time)

        # Initialize previous values
        self._prev_hma_value = 0.0

        # Create indicators
        hma = HullMovingAverage()
        hma.Length = self.hma_period
        volume_sma = SimpleMovingAverage()
        volume_sma.Length = self.volume_avg_period
        volume_std_dev = StandardDeviation()
        volume_std_dev.Length = self.volume_avg_period

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)

        def process(candle, hma_value):
            # Process volume indicators
            volume_sma_value = volume_sma.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished)
            volume_std_dev_value = volume_std_dev.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished)

            # Process the strategy logic
            self.ProcessStrategy(
                candle,
                float(hma_value),
                float(candle.TotalVolume),
                float(to_float(volume_sma_value)),
                float(to_float(volume_std_dev_value))
            )

        subscription.BindEx(hma, process).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessStrategy(self, candle, hma_value, volume, volume_avg, volume_std_dev):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if it's the first valid candle
        if self._prev_hma_value == 0:
            self._prev_hma_value = hma_value
            return

        # Detect HMA direction
        is_hma_rising = hma_value > self._prev_hma_value

        # Check for volume spike
        volume_threshold = volume_avg + (self.volume_threshold_factor * volume_std_dev)
        is_volume_spiking = volume > volume_threshold

        # Trading logic - only enter on HMA direction change with volume spike
        if is_hma_rising and is_volume_spiking and self.Position <= 0:
            # Hull MA rising with volume spike - Go long
            self.CancelActiveOrders()

            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(position_size)
        elif not is_hma_rising and is_volume_spiking and self.Position >= 0:
            # Hull MA falling with volume spike - Go short
            self.CancelActiveOrders()

            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(position_size)

        # Exit logic - HMA direction reversal
        if (self.Position > 0 and not is_hma_rising) or (self.Position < 0 and is_hma_rising):
            # Close position on HMA direction change
            self.ClosePosition()

        # Update previous HMA value
        self._prev_hma_value = hma_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_volume_spike_strategy()
