import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class ma_volume_strategy(Strategy):
    """Strategy that combines moving average and volume indicators to identify
    potential trend breakouts with volume confirmation."""

    def __init__(self):
        """Initializes a new instance of the :class:`ma_volume_strategy`."""
        super(ma_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetRange(5, 200) \
            .SetDisplay("MA Period", "Period for moving average calculation", "MA Settings") \
            .SetCanOptimize(True)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetRange(5, 100) \
            .SetDisplay("Volume MA Period", "Period for volume moving average calculation", "Volume Settings") \
            .SetCanOptimize(True)

        self._volume_threshold = self.Param("VolumeThreshold", 1.5) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Volume Threshold", "Volume threshold multiplier for volume confirmation", "Volume Settings") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        # Internal indicators
        self._price_sma = None
        self._volume_sma = None

    @property
    def candle_type(self):
        """Data type for candles."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ma_period(self):
        """Period for moving average calculation."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def volume_period(self):
        """Period for volume moving average calculation."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_threshold(self):
        """Volume threshold multiplier for volume confirmation."""
        return self._volume_threshold.Value

    @volume_threshold.setter
    def volume_threshold(self, value):
        self._volume_threshold.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up stop loss protection, indicators,
        subscriptions and charting.

        :param time: The time when the strategy started.
        """
        super(ma_volume_strategy, self).OnStarted(time)

        # Set up stop loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Initialize indicators
        self._price_sma = SimpleMovingAverage()
        self._price_sma.Length = self.ma_period
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self.volume_period

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Create custom processor to handle both price and volume indicators
        subscription.Bind(self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._price_sma)

            # Create volume area for volume indicator
            volume_area = self.CreateChartArea()
            self.DrawIndicator(volume_area, self._volume_sma)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """Process incoming candle with manual indicator updates.

        :param candle: Candle to process.
        """
        if candle.State != CandleStates.Finished:
            return

        # Process indicators
        sma_value = to_float(process_candle(self._price_sma, candle))

        # Handle volume
        volume_sma_value = to_float(
            process_float(
                self._volume_sma,
                candle.TotalVolume,
                candle.ServerTime,
                candle.State == CandleStates.Finished,
            )
        )

        if not self.IsFormedAndOnlineAndAllowTrading() or not self._price_sma.IsFormed or not self._volume_sma.IsFormed:
            return

        # Check if current volume is above threshold compared to average volume
        volume_confirmation = candle.TotalVolume > volume_sma_value * self.volume_threshold

        # Trading logic
        if volume_confirmation:
            if candle.ClosePrice > sma_value and self.Position <= 0:
                # Price above MA with volume confirmation - Long signal
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Buy signal: Price ({0}) above MA ({1:F4}) with volume confirmation ({2} > {3})".format(
                        candle.ClosePrice, sma_value, candle.TotalVolume, volume_sma_value * self.volume_threshold))
            elif candle.ClosePrice < sma_value and self.Position >= 0:
                # Price below MA with volume confirmation - Short signal
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Sell signal: Price ({0}) below MA ({1:F4}) with volume confirmation ({2} > {3})".format(
                        candle.ClosePrice, sma_value, candle.TotalVolume, volume_sma_value * self.volume_threshold))

        # Exit logic
        if self.Position > 0 and candle.ClosePrice < sma_value and volume_confirmation:
            # Exit long when price crosses below MA with volume confirmation
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit long: Price ({0}) crossed below MA ({1:F4}) with volume confirmation".format(
                    candle.ClosePrice, sma_value))
        elif self.Position < 0 and candle.ClosePrice > sma_value and volume_confirmation:
            # Exit short when price crosses above MA with volume confirmation
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit short: Price ({0}) crossed above MA ({1:F4}) with volume confirmation".format(
                    candle.ClosePrice, sma_value))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_volume_strategy()
