import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_breakout_strategy(Strategy):
    """
    Strategy that trades on volume breakouts.
    When volume rises significantly above its average, it enters position in the direction determined by price.
    """

    def __init__(self):
        super(volume_breakout_strategy, self).__init__()

        # Initialize VolumeBreakoutStrategy.
        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for volume average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._volume_average = None
        self._volume_std_dev = None
        self._last_avg_volume = 0
        self._last_std_dev = 0

    @property
    def avg_period(self):
        """Period for volume average calculation."""
        return self._avg_period.Value

    @avg_period.setter
    def avg_period(self, value):
        self._avg_period.Value = value

    @property
    def multiplier(self):
        """Standard deviation multiplier for breakout detection."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss(self):
        """Stop-loss percentage."""
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(volume_breakout_strategy, self).OnReseted()
        self._last_avg_volume = 0
        self._last_std_dev = 0

    def OnStarted(self, time):
        super(volume_breakout_strategy, self).OnStarted(time)


        # Create indicators for volume analysis
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = self.avg_period
        self._volume_std_dev = SimpleMovingAverage()
        self._volume_std_dev.Length = self.avg_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind candles to processing method
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss, UnitTypes.Percent)
        )
        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Calculate volume indicators
        volume = float(candle.TotalVolume)

        # Calculate volume average
        avg_value = process_float(self._volume_average, volume, candle.ServerTime, candle.State == CandleStates.Finished)
        avg_volume = float(avg_value)

        # Calculate standard deviation approximation
        deviation = Math.Abs(volume - avg_volume)
        std_dev_value = process_float(self._volume_std_dev, deviation, candle.ServerTime, candle.State == CandleStates.Finished)
        std_dev = float(std_dev_value)

        # Skip the first N candles until we have enough data
        if not self._volume_average.IsFormed or not self._volume_std_dev.IsFormed:
            self._last_avg_volume = avg_volume
            self._last_std_dev = std_dev
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._last_avg_volume = avg_volume
            self._last_std_dev = std_dev
            return

        # Volume breakout detection (volume increases significantly above its average)
        if volume > avg_volume + self.multiplier * std_dev:
            # Determine direction based on price movement
            bullish = candle.ClosePrice > candle.OpenPrice

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Trade in the direction of price movement
            if bullish and self.Position <= 0:
                # Bullish breakout - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif not bullish and self.Position >= 0:
                # Bearish breakout - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit condition - volume returns to average
        elif (self.Position > 0 and volume < avg_volume) or (self.Position < 0 and volume < avg_volume):
            # Exit position
            self.ClosePosition()

        # Update last values
        self._last_avg_volume = avg_volume
        self._last_std_dev = std_dev

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volume_breakout_strategy()