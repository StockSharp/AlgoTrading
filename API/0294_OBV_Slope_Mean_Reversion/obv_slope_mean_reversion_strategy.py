import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import OnBalanceVolume
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class obv_slope_mean_reversion_strategy(Strategy):
    """
    OBV Slope Mean Reversion Strategy.
    This strategy trades based on On-Balance Volume (OBV) slope reversions to the mean.
    """

    def __init__(self):
        super(obv_slope_mean_reversion_strategy, self).__init__()

        # OBV SMA Period.
        self._obv_sma_period = self.Param("ObvSmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("OBV SMA Period", "Period for OBV SMA", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Period for calculating slope average and standard deviation.
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # The multiplier for standard deviation to determine entry threshold.
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._obv = None
        self._obv_sma = None
        self._previous_obv = 0.0
        self._current_obv_value = 0.0
        self._current_obv_slope = 0.0
        self._is_first_calculation = True

        self._average_slope = 0.0
        self._slope_std_dev = 0.0
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0
        self._slope_buffer = []  # buffer for last N slopes

    @property
    def ObvSmaPeriod(self):
        return self._obv_sma_period.Value

    @ObvSmaPeriod.setter
    def ObvSmaPeriod(self, value):
        self._obv_sma_period.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        # Initialize indicators
        self._obv = OnBalanceVolume()
        self._obv_sma = SimpleMovingAverage()
        self._obv_sma.Length = self.ObvSmaPeriod

        # Initialize statistics variables
        self._sample_count = 0
        self._sum_slopes = 0.0
        self._sum_slopes_squared = 0.0
        self._is_first_calculation = True
        self._slope_buffer = []
        self._previous_obv = 0.0
        self._current_obv_value = 0.0
        self._current_obv_slope = 0.0
        self._average_slope = 0.0
        self._slope_std_dev = 0.0

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Set up chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._obv)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(self.StopLossPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent, UnitTypes.Percent)
        )

        super(obv_slope_mean_reversion_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Process the candle with OBV indicator
        obv_value = to_float(self._obv.Process(candle))

        # Process OBV through SMA
        obv_sma_value = to_float(self._obv_sma.Process(obv_value, candle.ServerTime, candle.State == CandleStates.Finished))

        # Skip if OBV SMA is not formed yet
        if not self._obv_sma.IsFormed:
            return

        # Save current OBV value
        self._current_obv_value = obv_value

        # Calculate OBV slope
        if self._is_first_calculation:
            self._previous_obv = self._current_obv_value
            self._is_first_calculation = False
            return

        self._current_obv_slope = self._current_obv_value - self._previous_obv
        self._previous_obv = self._current_obv_value

        # Update statistics for slope values using a circular buffer
        if len(self._slope_buffer) == self.LookbackPeriod:
            removed = self._slope_buffer.pop(0)
            self._sum_slopes -= removed
            self._sum_slopes_squared -= removed * removed
        self._slope_buffer.append(self._current_obv_slope)
        self._sum_slopes += self._current_obv_slope
        self._sum_slopes_squared += self._current_obv_slope * self._current_obv_slope
        self._sample_count = len(self._slope_buffer)

        # We need enough samples to calculate meaningful statistics
        if self._sample_count < self.LookbackPeriod:
            return

        # Calculate statistics
        self._average_slope = self._sum_slopes / self._sample_count
        variance = (self._sum_slopes_squared / self._sample_count) - (self._average_slope * self._average_slope)
        self._slope_std_dev = 0.0 if variance <= 0 else Math.Sqrt(variance)

        # Calculate thresholds for entries
        long_entry_threshold = self._average_slope - self.DeviationMultiplier * self._slope_std_dev
        short_entry_threshold = self._average_slope + self.DeviationMultiplier * self._slope_std_dev

        # OBV divergence check (price vs OBV)
        price_change = candle.ClosePrice - candle.OpenPrice
        obv_change_relative_to_price = 0 if price_change == 0 else (self._current_obv_slope / abs(price_change))

        # Trading logic
        if self._current_obv_slope < long_entry_threshold and self.Position <= 0:
            # Long entry: OBV slope is significantly lower than average (mean reversion expected)
            # Additional filter: Check for positive price movement to confirm potential reversal
            if candle.ClosePrice > candle.OpenPrice:
                self.LogInfo("OBV slope {0} below threshold {1}, entering LONG".format(self._current_obv_slope, long_entry_threshold))
                self.BuyMarket(self.Volume + abs(self.Position))
        elif self._current_obv_slope > short_entry_threshold and self.Position >= 0:
            # Short entry: OBV slope is significantly higher than average (mean reversion expected)
            # Additional filter: Check for negative price movement to confirm potential reversal
            if candle.ClosePrice < candle.OpenPrice:
                self.LogInfo("OBV slope {0} above threshold {1}, entering SHORT".format(self._current_obv_slope, short_entry_threshold))
                self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and self._current_obv_slope > self._average_slope:
            # Exit long when OBV slope returns to or above average
            self.LogInfo("OBV slope {0} returned to average {1}, exiting LONG".format(self._current_obv_slope, self._average_slope))
            self.SellMarket(abs(self.Position))
        elif self.Position < 0 and self._current_obv_slope < self._average_slope:
            # Exit short when OBV slope returns to or below average
            self.LogInfo("OBV slope {0} returned to average {1}, exiting SHORT".format(self._current_obv_slope, self._average_slope))
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return obv_slope_mean_reversion_strategy()
