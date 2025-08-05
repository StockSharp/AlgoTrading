import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_kumo_breakout_strategy(Strategy):
    """
    Strategy based on Ichimoku Kumo (cloud) breakout.
    It enters long position when price breaks above the cloud and Tenkan-sen crosses above Kijun-sen.
    It enters short position when price breaks below the cloud and Tenkan-sen crosses below Kijun-sen.
    
    """
    
    def __init__(self):
        super(ichimoku_kumo_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line (faster)", "Indicators")
        
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen line (slower)", "Indicators")
        
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state tracking
        self._prev_tenkan_value = 0.0
        self._prev_kijun_value = 0.0
        self._prev_is_tenkan_above_kijun = False
        self._prev_is_price_above_cloud = False

    @property
    def tenkan_period(self):
        """Period for Tenkan-sen line."""
        return self._tenkan_period.Value

    @tenkan_period.setter
    def tenkan_period(self, value):
        self._tenkan_period.Value = value

    @property
    def kijun_period(self):
        """Period for Kijun-sen line."""
        return self._kijun_period.Value

    @kijun_period.setter
    def kijun_period(self, value):
        self._kijun_period.Value = value

    @property
    def senkou_span_period(self):
        """Period for Senkou Span B (used with Senkou Span A to form the cloud)."""
        return self._senkou_span_period.Value

    @senkou_span_period.setter
    def senkou_span_period(self, value):
        self._senkou_span_period.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ichimoku_kumo_breakout_strategy, self).OnReseted()
        self._prev_tenkan_value = 0.0
        self._prev_kijun_value = 0.0
        self._prev_is_tenkan_above_kijun = False
        self._prev_is_price_above_cloud = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(ichimoku_kumo_breakout_strategy, self).OnStarted(time)

        # Create Ichimoku indicator
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.tenkan_period
        ichimoku.Kijun.Length = self.kijun_period
        ichimoku.SenkouB.Length = self.senkou_span_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value):
        """
        Processes each finished candle and executes Ichimoku-based trading logic.
        
        :param candle: The processed candle message.
        :param ichimoku_value: The current value of the Ichimoku indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from Ichimoku indicator
        if ichimoku_value.Tenkan is None:
            return
        tenkan = float(ichimoku_value.Tenkan)

        if ichimoku_value.Kijun is None:
            return
        kijun = float(ichimoku_value.Kijun)

        if ichimoku_value.SenkouA is None:
            return
        senkou_a = float(ichimoku_value.SenkouA)

        if ichimoku_value.SenkouB is None:
            return
        senkou_b = float(ichimoku_value.SenkouB)

        # Skip if any value is zero (indicator initializing)
        if tenkan == 0 or kijun == 0 or senkou_a == 0 or senkou_b == 0:
            self._prev_tenkan_value = tenkan
            self._prev_kijun_value = kijun
            return

        # Determine cloud boundaries
        upper_cloud = max(senkou_a, senkou_b)
        lower_cloud = min(senkou_a, senkou_b)

        # Check price position relative to cloud
        is_price_above_cloud = candle.ClosePrice > upper_cloud
        is_price_below_cloud = candle.ClosePrice < lower_cloud
        is_price_in_cloud = not is_price_above_cloud and not is_price_below_cloud

        # Check Tenkan/Kijun cross
        is_tenkan_above_kijun = tenkan > kijun
        is_tenkan_kijun_cross = (is_tenkan_above_kijun != self._prev_is_tenkan_above_kijun and 
                                self._prev_tenkan_value != 0)

        # Check cloud breakout
        is_breaking_above_cloud = is_price_above_cloud and not self._prev_is_price_above_cloud
        is_breaking_below_cloud = (is_price_below_cloud and 
                                 (self._prev_is_price_above_cloud or 
                                  (not self._prev_is_price_above_cloud and not is_price_below_cloud)))

        # Trading logic
        if is_price_above_cloud and is_tenkan_above_kijun and self.Position <= 0:
            # Bullish conditions met - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price ({0}) above cloud ({1}) and Tenkan ({2}) > Kijun ({3})".format(
                candle.ClosePrice, upper_cloud, tenkan, kijun))
        elif is_price_below_cloud and not is_tenkan_above_kijun and self.Position >= 0:
            # Bearish conditions met - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price ({0}) below cloud ({1}) and Tenkan ({2}) < Kijun ({3})".format(
                candle.ClosePrice, lower_cloud, tenkan, kijun))
        # Exit logic
        elif self.Position > 0 and is_price_below_cloud:
            # Exit long position
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Price ({0}) broke below cloud ({1})".format(
                candle.ClosePrice, lower_cloud))
        elif self.Position < 0 and is_price_above_cloud:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price ({0}) broke above cloud ({1})".format(
                candle.ClosePrice, upper_cloud))

        # Update state for the next candle
        self._prev_tenkan_value = tenkan
        self._prev_kijun_value = kijun
        self._prev_is_tenkan_above_kijun = is_tenkan_above_kijun
        self._prev_is_price_above_cloud = is_price_above_cloud

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ichimoku_kumo_breakout_strategy()
