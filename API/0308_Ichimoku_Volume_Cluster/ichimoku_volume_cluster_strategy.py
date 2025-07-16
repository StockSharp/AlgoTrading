import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, Unit
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ichimoku_volume_cluster_strategy(Strategy):
    """
    Strategy based on Ichimoku Cloud with volume cluster confirmation.
    """

    def __init__(self):
        super(ichimoku_volume_cluster_strategy, self).__init__()

        # Initialize strategy parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 4)

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Average Period", "Period for volume average and standard deviation", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_std_dev_multiplier = self.Param("VolumeStdDevMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume StdDev Multiplier", "Standard deviation multiplier for volume threshold", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(1*60)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        # Internal indicators
        self._volume_avg = None
        self._volume_std_dev = None

    @property
    def TenkanPeriod(self):
        """Tenkan-sen (Conversion Line) period."""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-sen (Base Line) period."""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Senkou Span B (Leading Span B) period."""
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def VolumeAvgPeriod(self):
        """Volume average period."""
        return self._volume_avg_period.Value

    @VolumeAvgPeriod.setter
    def VolumeAvgPeriod(self, value):
        self._volume_avg_period.Value = value

    @property
    def VolumeStdDevMultiplier(self):
        """Volume standard deviation multiplier."""
        return self._volume_std_dev_multiplier.Value

    @VolumeStdDevMultiplier.setter
    def VolumeStdDevMultiplier(self, value):
        self._volume_std_dev_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(ichimoku_volume_cluster_strategy, self).OnStarted(time)

        # Create Ichimoku indicator
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        # Create volume indicators
        self._volume_avg = SimpleMovingAverage()
        self._volume_avg.Length = self.VolumeAvgPeriod
        self._volume_std_dev = StandardDeviation()
        self._volume_std_dev.Length = self.VolumeAvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind Ichimoku to subscription
        subscription.BindEx(ichimoku, self.ProcessCandle).Start()

        # Setup stop-loss at Kijun-sen level
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(0),
            isStopTrailing=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

            # Create second area for volume
            volume_area = self.CreateChartArea()
            if volume_area is not None:
                self.DrawIndicator(volume_area, self._volume_avg)

    def ProcessCandle(self, candle, ichimoku_value):
        """Process candle and execute strategy logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume

        volume_avg_value = to_float(process_float(self._volume_avg, volume, candle.ServerTime, candle.State == CandleStates.Finished))
        volume_std_dev_value = to_float(process_float(self._volume_std_dev, volume, candle.ServerTime, candle.State == CandleStates.Finished))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ichimoku_typed = ichimoku_value

        # Extract Ichimoku values
        if ichimoku_typed.Tenkan is None:
            return
        if ichimoku_typed.Kijun is None:
            return
        if ichimoku_typed.SenkouA is None:
            return
        if ichimoku_typed.SenkouB is None:
            return

        tenkan = ichimoku_typed.Tenkan
        kijun = ichimoku_typed.Kijun
        senkou_a = ichimoku_typed.SenkouA
        senkou_b = ichimoku_typed.SenkouB

        # Determine cloud position
        price_above_cloud = candle.ClosePrice > Math.Max(senkou_a, senkou_b)
        price_below_cloud = candle.ClosePrice < Math.Min(senkou_a, senkou_b)

        # Check for volume spike
        volume_threshold = volume_avg_value + self.VolumeStdDevMultiplier * volume_std_dev_value
        has_volume_spike = candle.TotalVolume > volume_threshold

        # Define entry conditions
        long_entry_condition = price_above_cloud and tenkan > kijun and has_volume_spike and self.Position <= 0
        short_entry_condition = price_below_cloud and tenkan < kijun and has_volume_spike and self.Position >= 0

        # Define exit conditions
        long_exit_condition = price_below_cloud and self.Position > 0
        short_exit_condition = price_above_cloud and self.Position < 0

        # Log current values
        self.LogInfo("Candle: {0}, Close: {1}, Volume: {2}, Threshold: {3}".format(
            candle.OpenTime, candle.ClosePrice, candle.TotalVolume, volume_threshold))
        self.LogInfo("Tenkan: {0}, Kijun: {1}, Senkou A: {2}, Senkou B: {3}".format(
            tenkan, kijun, senkou_a, senkou_b))

        # Execute trading logic
        if long_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(position_size)

            self.LogInfo("Long entry: Price={0}, Volume={1}, Threshold={2}".format(
                candle.ClosePrice, candle.TotalVolume, volume_threshold))
        elif short_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(position_size)

            self.LogInfo("Short entry: Price={0}, Volume={1}, Threshold={2}".format(
                candle.ClosePrice, candle.TotalVolume, volume_threshold))
        elif long_exit_condition:
            # Exit long position
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long exit: Price={0}, Reason=Below Cloud".format(
                candle.ClosePrice))
        elif short_exit_condition:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short exit: Price={0}, Reason=Above Cloud".format(
                candle.ClosePrice))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_volume_cluster_strategy()
