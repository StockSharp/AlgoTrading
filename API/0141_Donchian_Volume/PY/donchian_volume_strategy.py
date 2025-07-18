import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage, DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class donchian_volume_strategy(Strategy):
    """
    Strategy that uses Donchian Channels to identify breakouts
    and volume confirmation to filter signals.
    Enters positions when price breaks above/below Donchian Channel with increased volume.

    """

    def __init__(self):
        super(donchian_volume_strategy, self).__init__()

        # Strategy constructor.
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Period", "Period for volume averaging", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm breakout", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 2.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._average_volume = 0.0

    @property
    def donchian_period(self):
        """Donchian Channels period."""
        return self._donchian_period.Value

    @donchian_period.setter
    def donchian_period(self, value):
        self._donchian_period.Value = value

    @property
    def volume_period(self):
        """Volume averaging period."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_multiplier(self):
        """Volume multiplier for breakout confirmation."""
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

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(donchian_volume_strategy, self).OnStarted(time)

        self._average_volume = 0.0

        # Create indicators
        donchian_high = Highest()
        donchian_high.Length = self.donchian_period

        donchian_low = Lowest()
        donchian_low.Length = self.donchian_period

        volume_average = SimpleMovingAverage()
        volume_average.Length = self.volume_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(volume_average, donchian_high, donchian_low, self.ProcessDonchian).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Create a composite indicator for visualization purposes
            donchian = DonchianChannels()
            donchian.Length = self.donchian_period

            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessDonchian(self, candle, volume_avg_value, highest_value, lowest_value):
        """Process Donchian Channel values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if volume_avg_value.IsFinal:
            self._average_volume = float(volume_avg_value)

        # Check if strategy is ready to trade
        if (not self.IsFormedAndOnlineAndAllowTrading()) or self._average_volume <= 0:
            return

        highest_dec = float(highest_value)
        lowest_dec = float(lowest_value)

        # Calculate middle line of Donchian Channel
        middle_line = (highest_dec + lowest_dec) / 2

        # Check if volume condition is met
        is_volume_high_enough = candle.TotalVolume > self._average_volume * self.volume_multiplier

        if is_volume_high_enough:
            # Long entry: price breaks above highest high with increased volume
            if candle.ClosePrice > highest_dec and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
            # Short entry: price breaks below lowest low with increased volume
            elif candle.ClosePrice < lowest_dec and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

        # Exit conditions based on middle line
        if self.Position > 0 and candle.ClosePrice < middle_line:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > middle_line:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_volume_strategy()
