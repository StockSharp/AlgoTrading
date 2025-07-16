import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import VolumeIndicator
from StockSharp.Algo.Strategies import Strategy

class volume_mean_reversion_strategy(Strategy):
    """
    Volume Mean Reversion strategy.
    This strategy enters positions when trading volume is significantly below or above its average value.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(volume_mean_reversion_strategy, self).__init__()

        # Period for calculating mean and standard deviation of Volume.
        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating Volume average and standard deviation", "Settings")

        # Deviation multiplier for entry signals.
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")

        # Candle type.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Stop-loss percentage.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # Internal state variables
        self._avg_volume = 0.0
        self._std_dev_volume = 0.0
        self._sum_volume = 0.0
        self._sum_squares_volume = 0.0
        self._count = 0
        self._volume_values = []

    @property
    def AveragePeriod(self):
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def GetWorkingSecurities(self):
        """Return security and timeframe used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """
        Reset variables and initialize indicators, subscriptions and charting.
        """
        super(volume_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables
        self._avg_volume = 0.0
        self._std_dev_volume = 0.0
        self._sum_volume = 0.0
        self._sum_squares_volume = 0.0
        self._count = 0
        self._volume_values = []

        # Create Volume indicator (for visualization)
        volume = VolumeIndicator()

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, volume)
            self.DrawOwnTrades(area)

            # Create additional area for volume
            volume_area = self.CreateChartArea()
            if volume_area is not None:
                self.DrawIndicator(volume_area, volume)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0),  # We'll manage exits ourselves based on Volume
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle):
        """
        Process candle and execute trading logic based on volume statistics.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Volume value (for candles, this is TotalVolume)
        current_volume = candle.TotalVolume

        # Update Volume statistics
        self.UpdateVolumeStatistics(current_volume)

        # If we don't have enough data yet for statistics
        if self._count < self.AveragePeriod:
            return

        # For volume-based strategies, price direction is important
        price_direction = Sides.Buy if candle.ClosePrice > candle.OpenPrice else Sides.Sell

        # Check for entry conditions
        if self.Position == 0:
            # Volume is significantly below average - expecting a return to average trading activity
            if current_volume < self._avg_volume - self.DeviationMultiplier * self._std_dev_volume:
                # In low volume environments, we might look for potential market accumulation
                # and follow the small price movement which could be institutional accumulation
                if price_direction == Sides.Buy:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(
                        "Long entry: Volume = {0}, Avg = {1}, StdDev = {2}, Low volume with price up".format(
                            current_volume, self._avg_volume, self._std_dev_volume))
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(
                        "Short entry: Volume = {0}, Avg = {1}, StdDev = {2}, Low volume with price down".format(
                            current_volume, self._avg_volume, self._std_dev_volume))
            # Volume is significantly above average - potential high volume climax
            elif current_volume > self._avg_volume + self.DeviationMultiplier * self._std_dev_volume:
                # High volume often indicates climactic moves that might reverse
                # So we consider going against the price direction on high volume bars
                if price_direction == Sides.Sell:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(
                        "Contrarian long entry: Volume = {0}, Avg = {1}, StdDev = {2}, High volume with price down".format(
                            current_volume, self._avg_volume, self._std_dev_volume))
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(
                        "Contrarian short entry: Volume = {0}, Avg = {1}, StdDev = {2}, High volume with price up".format(
                            current_volume, self._avg_volume, self._std_dev_volume))
        # Check for exit conditions
        elif self.Position > 0:  # Long position
            # Exit long position when volume returns to average
            if current_volume > self._avg_volume or (
                current_volume > self._avg_volume * 0.8 and price_direction == Sides.Sell):
                self.ClosePosition()
                self.LogInfo("Long exit: Volume = {0}, Avg = {1}".format(current_volume, self._avg_volume))
        elif self.Position < 0:  # Short position
            # Exit short position when volume returns to average
            if current_volume > self._avg_volume or (
                current_volume > self._avg_volume * 0.8 and price_direction == Sides.Buy):
                self.ClosePosition()
                self.LogInfo("Short exit: Volume = {0}, Avg = {1}".format(current_volume, self._avg_volume))

    def UpdateVolumeStatistics(self, current_volume):
        """Update internal statistics for volume calculations."""
        # Add current value to the queue
        self._volume_values.append(current_volume)
        self._sum_volume += current_volume
        self._sum_squares_volume += current_volume * current_volume
        self._count += 1

        # If queue is larger than period, remove oldest value
        if len(self._volume_values) > self.AveragePeriod:
            oldest_volume = self._volume_values.pop(0)
            self._sum_volume -= oldest_volume
            self._sum_squares_volume -= oldest_volume * oldest_volume
            self._count -= 1

        # Calculate average and standard deviation
        if self._count > 0:
            self._avg_volume = self._sum_volume / self._count

            if self._count > 1:
                variance = (self._sum_squares_volume - (self._sum_volume * self._sum_volume) / self._count) / (self._count - 1)
                self._std_dev_volume = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_volume = 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volume_mean_reversion_strategy()
