import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_volume_strategy(Strategy):
    """
    Implementation of strategy - Ichimoku + Volume.
    Buy when price is above Kumo cloud, Tenkan-sen is above Kijun-sen, and volume is above average.
    Sell when price is below Kumo cloud, Tenkan-sen is below Kijun-sen, and volume is above average.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(ichimoku_volume_strategy, self).__init__()

        # Initialize ichimoku parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen period (fast)", "Ichimoku Parameters")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen period (slow)", "Ichimoku Parameters")

        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span Period", "Senkou Span B period", "Ichimoku Parameters")

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._average_volume = 0.0
        self._volume_counter = 0

    @property
    def tenkan_period(self):
        """Tenkan-sen period."""
        return self._tenkan_period.Value

    @tenkan_period.setter
    def tenkan_period(self, value):
        self._tenkan_period.Value = value

    @property
    def kijun_period(self):
        """Kijun-sen period."""
        return self._kijun_period.Value

    @kijun_period.setter
    def kijun_period(self, value):
        self._kijun_period.Value = value

    @property
    def senkou_span_period(self):
        """Senkou Span period."""
        return self._senkou_span_period.Value

    @senkou_span_period.setter
    def senkou_span_period(self, value):
        self._senkou_span_period.Value = value

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
        super(ichimoku_volume_strategy, self).OnReseted()
        self._average_volume = 0.0
        self._volume_counter = 0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(ichimoku_volume_strategy, self).OnStarted(time)

        # Create Ichimoku indicator
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.tenkan_period
        ichimoku.Kijun.Length = self.kijun_period
        ichimoku.SenkouB.Length = self.senkou_span_period

        # Reset volume tracking
        self._average_volume = 0.0
        self._volume_counter = 0

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind Ichimoku indicator to candles
        subscription.BindEx(ichimoku, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

        # Start protective orders (stop-loss)
        self.StartProtection(None, self.stop_loss)

    def ProcessCandle(self, candle, ichimoku_value):
        """Processes each finished candle and executes Ichimoku + Volume trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Ichimoku values
        try:
            if hasattr(ichimoku_value, 'Tenkan') and ichimoku_value.Tenkan is not None:
                tenkan = float(ichimoku_value.Tenkan)
            else:
                return

            if hasattr(ichimoku_value, 'Kijun') and ichimoku_value.Kijun is not None:
                kijun = float(ichimoku_value.Kijun)
            else:
                return

            if hasattr(ichimoku_value, 'SenkouA') and ichimoku_value.SenkouA is not None:
                senkou_a = float(ichimoku_value.SenkouA)
            else:
                return

            if hasattr(ichimoku_value, 'SenkouB') and ichimoku_value.SenkouB is not None:
                senkou_b = float(ichimoku_value.SenkouB)
            else:
                return
        except:
            return

        # Calculate Kumo cloud boundaries
        upper_kumo = max(senkou_a, senkou_b)
        lower_kumo = min(senkou_a, senkou_b)

        # Update average volume calculation
        current_volume = candle.TotalVolume

        if self._volume_counter < self.volume_avg_period:
            self._volume_counter += 1
            self._average_volume = ((self._average_volume * (self._volume_counter - 1)) + current_volume) / self._volume_counter
        else:
            self._average_volume = ((self._average_volume * (self.volume_avg_period - 1)) + current_volume) / self.volume_avg_period

        # Check if volume is above average
        is_volume_above_average = current_volume > self._average_volume

        self.LogInfo("Candle: {0}, Close: {1}, TenkanSen: {2}, KijunSen: {3}, Upper Kumo: {4}, Lower Kumo: {5}, Volume: {6}, Avg Volume: {7}".format(
            candle.OpenTime, candle.ClosePrice, tenkan, kijun, upper_kumo, lower_kumo, current_volume, self._average_volume))

        # Trading rules
        if candle.ClosePrice > upper_kumo and tenkan > kijun and is_volume_above_average and self.Position <= 0:
            # Buy signal - price above Kumo, Tenkan above Kijun, volume above average
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price above Kumo, Tenkan above Kijun, Volume above average. Volume: {0}".format(volume))
        elif candle.ClosePrice < lower_kumo and tenkan < kijun and is_volume_above_average and self.Position >= 0:
            # Sell signal - price below Kumo, Tenkan below Kijun, volume above average
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price below Kumo, Tenkan below Kijun, Volume above average. Volume: {0}".format(volume))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_volume_strategy()
