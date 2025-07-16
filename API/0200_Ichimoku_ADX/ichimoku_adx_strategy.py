import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_adx_strategy(Strategy):
    """
    Strategy based on Ichimoku Cloud and ADX indicators.

    Entry criteria:
    Long: Price > Kumo (cloud) && Tenkan > Kijun && ADX > 25 (uptrend with strong movement)
    Short: Price < Kumo (cloud) && Tenkan < Kijun && ADX > 25 (downtrend with strong movement)

    Exit criteria:
    Long: Price < Kumo (price falls below cloud)
    Short: Price > Kumo (price rises above cloud)

    """

    def __init__(self):
        super(ichimoku_adx_strategy, self).__init__()

        # Period for Tenkan-sen calculation (conversion line).
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen (conversion line)", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 13, 2)

        # Period for Kijun-sen calculation (base line).
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Period for Kijun-sen (base line)", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 32, 3)

        # Period for Senkou Span B calculation (second cloud component).
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B (second cloud component)", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 5)

        # Period for ADX calculation.
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 5)

        # Threshold for ADX to confirm trend strength.
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20.0, 30.0, 5.0)

        # Type of candles to use.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous state tracking
        self._is_price_above_cloud = False
        self._is_tenkan_above_kijun = False
        self._last_adx_value = 0.0

    @property
    def TenkanPeriod(self):
        """Period for Tenkan-sen calculation (conversion line)."""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Period for Kijun-sen calculation (base line)."""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Period for Senkou Span B calculation (second cloud component)."""
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def AdxPeriod(self):
        """Period for ADX calculation."""
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        """Threshold for ADX to confirm trend strength."""
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ichimoku_adx_strategy, self).OnStarted(time)

        # Reset state tracking variables
        self._is_price_above_cloud = False
        self._is_tenkan_above_kijun = False
        self._last_adx_value = 0.0

        # Create indicators
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # We'll need to manually bind Ichimoku and ADX separately as they have different output values
        subscription.BindEx(ichimoku, adx, self.ProcessIndicators).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)

            # Create separate area for ADX
            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)

            self.DrawOwnTrades(area)

    # Process Ichimoku indicator data
    def ProcessIndicators(self, candle, ichimoku_value, adx_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if adx_value.MovingAverage is None:
            return
        adx = adx_value.MovingAverage

        self._last_adx_value = adx

        # Get Ichimoku values
        try:
            tenkan = ichimoku_value.Tenkan
            kijun = ichimoku_value.Kijun
            senkou_a = ichimoku_value.SenkouA
            senkou_b = ichimoku_value.SenkouB
        except AttributeError:
            return

        # Determine cloud boundaries
        cloud_top = Math.Max(senkou_a, senkou_b)
        cloud_bottom = Math.Min(senkou_a, senkou_b)

        # Update state
        is_price_above_cloud = candle.ClosePrice > cloud_top
        is_price_below_cloud = candle.ClosePrice < cloud_bottom
        is_tenkan_above_kijun = tenkan > kijun

        # Log current state
        self.LogInfo(
            "Close: {0}, Tenkan: {1:N2}, Kijun: {2:N2}, Cloud Top: {3:N2}, Cloud Bottom: {4:N2}, ADX: {5:N2}".format(
                candle.ClosePrice, tenkan, kijun, cloud_top, cloud_bottom, self._last_adx_value))

        is_price_relative_to_cloud_changed = self._is_price_above_cloud != is_price_above_cloud

        # Only make trading decisions if both Ichimoku and ADX have been calculated
        if self._last_adx_value <= 0:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_strong_trend = self._last_adx_value > self.AdxThreshold

        # Trading logic
        if self.Position == 0:  # No position
            if is_price_above_cloud and is_tenkan_above_kijun and is_strong_trend:
                # Buy signal: price above cloud, Tenkan above Kijun, strong trend
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: Price above cloud, Tenkan above Kijun, ADX = {0}".format(self._last_adx_value))
            elif is_price_below_cloud and not is_tenkan_above_kijun and is_strong_trend:
                # Sell signal: price below cloud, Tenkan below Kijun, strong trend
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: Price below cloud, Tenkan below Kijun, ADX = {0}".format(self._last_adx_value))
        elif is_price_relative_to_cloud_changed:  # Exit on cloud crossing
            if self.Position > 0 and not is_price_above_cloud:
                # Exit long position: price fell below cloud
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long position: Price fell into/below cloud")
            elif self.Position < 0 and not is_price_below_cloud:
                # Exit short position: price rose above cloud
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short position: Price rose into/above cloud")

        # Update tracking variables
        self._is_price_above_cloud = is_price_above_cloud
        self._is_tenkan_above_kijun = is_tenkan_above_kijun

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_adx_strategy()
