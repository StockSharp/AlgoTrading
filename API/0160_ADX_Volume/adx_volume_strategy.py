import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_volume_strategy(Strategy):
    """
    Implementation of strategy - ADX + Volume.
    Enter trades when ADX is above threshold with above average volume.
    Direction determined by DI+ and DI- comparison.
    """

    def __init__(self):
        super(adx_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX indicator", "ADX Parameters")

        self._adx_threshold = self.Param("AdxThreshold", 25) \
            .SetRange(10, 50) \
            .SetDisplay("ADX Threshold", "Threshold above which trend is considered strong", "ADX Parameters")

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # For volume tracking
        self._average_volume = 0
        self._volume_counter = 0

    @property
    def adx_period(self):
        """ADX period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def adx_threshold(self):
        """ADX threshold value to determine strong trend."""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def volume_avg_period(self):
        """Volume average period."""
        return self._volume_avg_period.Value

    @volume_avg_period.setter
    def volume_avg_period(self, value):
        self._volume_avg_period.Value = value

    @property
    def stop_loss(self):
        """Stop-loss value."""
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def candle_type(self):
        """Candle type used for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(adx_volume_strategy, self).OnReseted()
        self._average_volume = 0
        self._volume_counter = 0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(adx_volume_strategy, self).OnStarted(time)

        # Create ADX indicator
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period

        # Reset volume tracking
        self._average_volume = 0
        self._volume_counter = 0

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind ADX indicator to candles
        subscription.BindEx(adx, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(Unit(0, UnitTypes.Absolute), self.stop_loss)

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update average volume calculation
        current_volume = candle.TotalVolume

        if self._volume_counter < self.volume_avg_period:
            self._volume_counter += 1
            self._average_volume = ((self._average_volume * (self._volume_counter - 1)) + current_volume) / self._volume_counter
        else:
            self._average_volume = (self._average_volume * (self.volume_avg_period - 1) + current_volume) / self.volume_avg_period

        # Cast indicator value
        di_plus_value = adx_value.Dx.Plus
        di_minus_value = adx_value.Dx.Minus
        adx_ma = adx_value.MovingAverage

        # Check if volume is above average
        is_volume_above_average = current_volume > self._average_volume

        self.LogInfo("Candle: {0}, Close: {1}, ADX: {2}, DI+: {3}, DI-: {4}, Volume: {5}, Avg Volume: {6}".format(
            candle.OpenTime, candle.ClosePrice, adx_ma, di_plus_value, di_minus_value, current_volume, self._average_volume))

        # Trading rules
        if adx_ma > self.adx_threshold and is_volume_above_average:
            # Strong trend detected with above average volume
            if di_plus_value > di_minus_value and self.Position <= 0:
                # Bullish trend - DI+ > DI-
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Buy signal: Strong trend (ADX: {0}) with DI+ > DI- and high volume. Volume: {1}".format(
                    adx_ma, volume))
            elif di_minus_value > di_plus_value and self.Position >= 0:
                # Bearish trend - DI- > DI+
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Sell signal: Strong trend (ADX: {0}) with DI- > DI+ and high volume. Volume: {1}".format(
                    adx_ma, volume))
        # Exit conditions
        elif adx_ma < self.adx_threshold * 0.8:
            # Trend weakening - exit all positions
            if self.Position > 0:
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: ADX weakening below {0}. Position: {1}".format(
                    self.adx_threshold * 0.8, self.Position))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: ADX weakening below {0}. Position: {1}".format(
                    self.adx_threshold * 0.8, self.Position))
        # Check if DI+/DI- cross to exit positions
        elif di_plus_value < di_minus_value and self.Position > 0:
            # DI+ crosses below DI- while in long position
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: DI+ crossed below DI-. Position: {0}".format(self.Position))
        elif di_plus_value > di_minus_value and self.Position < 0:
            # DI+ crosses above DI- while in short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: DI+ crossed above DI-. Position: {0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_volume_strategy()
