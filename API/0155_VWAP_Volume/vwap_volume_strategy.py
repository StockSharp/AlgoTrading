import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_volume_strategy(Strategy):
    """
    Strategy combining VWAP and Volume indicators.
    Buys/sells on VWAP breakouts confirmed by above-average volume.

    """

    def __init__(self):
        super(vwap_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume MA Period", "Period for volume moving average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._volume_threshold = self.Param("VolumeThreshold", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Threshold", "Multiplier for average volume to confirm signal", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(1.2, 2.0, 0.2)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicator for volume
        self._volumeMA = None

    @property
    def volume_period(self):
        """Period for volume moving average."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_threshold(self):
        """Volume threshold as percentage of average volume."""
        return self._volume_threshold.Value

    @volume_threshold.setter
    def volume_threshold(self, value):
        self._volume_threshold.Value = value

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

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(vwap_volume_strategy, self).OnStarted(time)

        # Create indicators
        vwap = VolumeWeightedMovingAverage()
        self._volumeMA = SimpleMovingAverage()
        self._volumeMA.Length = self.volume_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Create custom bind for processing VWAP and volume data
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)

            # Create second area for volume
            volume_area = self.CreateChartArea()
            self.DrawIndicator(volume_area, self._volumeMA)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        # Process volume with indicator
        volume_ma = to_float(
            process_float(
                self._volumeMA,
                candle.TotalVolume,
                candle.ServerTime,
                candle.State == CandleStates.Finished,
            )
        )

        # Calculate VWAP manually for the current candle
        vwap = 0
        typical_price = float((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3)

        if candle.TotalVolume > 0:
            # Simple VWAP calculation for a single candle
            vwap = typical_price

        # Skip if volume MA is not formed yet
        if not self._volumeMA.IsFormed:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if volume is above threshold
        is_high_volume = candle.TotalVolume > volume_ma * self.volume_threshold

        # Trading logic
        if candle.ClosePrice > vwap and is_high_volume and self.Position <= 0:
            # Price breaks above VWAP with high volume - Buy
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif candle.ClosePrice < vwap and is_high_volume and self.Position >= 0:
            # Price breaks below VWAP with high volume - Sell
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        elif self.Position > 0 and candle.ClosePrice < vwap:
            # Exit long position when price crosses below VWAP
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > vwap:
            # Exit short position when price crosses above VWAP
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_volume_strategy()