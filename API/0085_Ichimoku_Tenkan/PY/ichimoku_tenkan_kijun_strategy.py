import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_tenkan_kijun_strategy(Strategy):
    """Ichimoku Tenkan/Kijun Cross Strategy.

    Enters long when Tenkan crosses above Kijun and price is above Kumo.
    Enters short when Tenkan crosses below Kijun and price is below Kumo.
    """
    def __init__(self):
        super(ichimoku_tenkan_kijun_strategy, self).__init__()

        # Initialize strategy parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen calculation", "Ichimoku Settings") \
            .SetRange(7, 13) \
            .SetCanOptimize(True)

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Period for Kijun-sen calculation", "Ichimoku Settings") \
            .SetRange(20, 30) \
            .SetCanOptimize(True)

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B calculation", "Ichimoku Settings") \
            .SetRange(40, 60) \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._ichimoku = None

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
    def senkou_span_b_period(self):
        """Senkou Span B period."""
        return self._senkou_span_b_period.Value

    @senkou_span_b_period.setter
    def senkou_span_b_period(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnReseted(self):
        super(ichimoku_tenkan_kijun_strategy, self).OnReseted()
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.tenkan_period
        self._ichimoku.Kijun.Length = self.kijun_period
        self._ichimoku.SenkouB.Length = self.senkou_span_b_period
        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(ichimoku_tenkan_kijun_strategy, self).OnStarted(time)

        # Initialize Ichimoku indicator
        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicator and candle handler
        subscription.BindEx(self._ichimoku, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, ichimoku_value):
        """Process candle with Ichimoku indicator values.

        :param candle: Candle.
        :param ichimoku_value: Ichimoku indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current Ichimoku values
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

        # If first calculation, just store values
        if self._prev_tenkan == 0 or self._prev_kijun == 0:
            self._prev_tenkan = tenkan
            self._prev_kijun = kijun
            return

        # Check for Tenkan/Kijun cross
        bullish_cross = self._prev_tenkan <= self._prev_kijun and tenkan > kijun
        bearish_cross = self._prev_tenkan >= self._prev_kijun and tenkan < kijun

        # Determine if price is above or below Kumo (cloud)
        lower_kumo = min(senkou_a, senkou_b)
        upper_kumo = max(senkou_a, senkou_b)
        price_above_kumo = candle.ClosePrice > upper_kumo
        price_below_kumo = candle.ClosePrice < lower_kumo

        # Long entry: Bullish cross and price above Kumo
        if bullish_cross and price_above_kumo and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Long entry: Tenkan ({0}) crossed above Kijun ({1}) and price ({2}) above Kumo ({3})".format(
                    tenkan, kijun, candle.ClosePrice, upper_kumo))
        # Short entry: Bearish cross and price below Kumo
        elif bearish_cross and price_below_kumo and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Short entry: Tenkan ({0}) crossed below Kijun ({1}) and price ({2}) below Kumo ({3})".format(
                    tenkan, kijun, candle.ClosePrice, lower_kumo))

        # Update previous values
        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_tenkan_kijun_strategy()
