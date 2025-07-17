import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class ichimoku_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Ichimoku Cloud width breakouts.
    When Ichimoku Cloud width increases significantly above its average,
    it enters position in the direction determined by price location relative to the cloud.
    """

    def __init__(self):
        """Initialize ichimoku_width_breakout_strategy."""
        super(ichimoku_width_breakout_strategy, self).__init__()

        # Strategy parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Indicators")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Period for Kijun-sen line", "Indicators")

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B line", "Indicators")

        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for cloud width average calculation", "Indicators")

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")

        # Internal state variables
        self._ichimoku = None
        self._width_average = None
        self._last_width = 0.0
        self._last_avg_width = 0.0

    # Properties
    @property
    def tenkan_period(self):
        """Tenkan-sen period for Ichimoku."""
        return self._tenkan_period.Value

    @tenkan_period.setter
    def tenkan_period(self, value):
        self._tenkan_period.Value = value

    @property
    def kijun_period(self):
        """Kijun-sen period for Ichimoku."""
        return self._kijun_period.Value

    @kijun_period.setter
    def kijun_period(self, value):
        self._kijun_period.Value = value

    @property
    def senkou_span_b_period(self):
        """Senkou Span B period for Ichimoku."""
        return self._senkou_span_b_period.Value

    @senkou_span_b_period.setter
    def senkou_span_b_period(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def avg_period(self):
        """Period for width average calculation."""
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

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(ichimoku_width_breakout_strategy, self).OnReseted()
        self._last_width = 0.0
        self._last_avg_width = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(ichimoku_width_breakout_strategy, self).OnStarted(time)

        self._last_width = 0.0
        self._last_avg_width = 0.0

        # Create indicators
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.tenkan_period
        self._ichimoku.Kijun.Length = self.kijun_period
        self._ichimoku.SenkouB.Length = self.senkou_span_b_period

        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.avg_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind Ichimoku to the candle subscription
        subscription.BindEx(self._ichimoku, self.ProcessIchimoku).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss, UnitTypes.Percent)
        )
        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

    def ProcessIchimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_value.IsFinal:
            return

        # Get current Ichimoku values
        # The structure of values depends on the implementation, this is just an example

        if ichimoku_value.Tenkan is None:
            return
        tenkan = float(ichimoku_value.Tenkan)

        if ichimoku_value.Kijun is None:
            return
        kijun = float(ichimoku_value.Kijun)

        if ichimoku_value.SenkouA is None:
            return
        senkou_span_a = float(ichimoku_value.SenkouA)

        if ichimoku_value.SenkouB is None:
            return
        senkou_span_b = float(ichimoku_value.SenkouB)

        # Calculate Cloud width (absolute difference between Senkou lines)
        width = Math.Abs(senkou_span_a - senkou_span_b)

        # Process width through average
        width_avg_value = process_float(self._width_average, width, candle.ServerTime, candle.State == CandleStates.Finished)
        avg_width = to_float(width_avg_value)

        # For first values, just save and skip
        if self._last_width == 0:
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Calculate width standard deviation (simplified approach)
        std_dev = Math.Abs(width - avg_width) * 1.5

        # Skip if indicators are not formed yet
        if not self._ichimoku.IsFormed or not self._width_average.IsFormed:
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Cloud width breakout detection
        if width > avg_width + self.multiplier * std_dev:
            # Determine trade direction based on price relative to cloud
            upper_cloud = max(senkou_span_a, senkou_span_b)
            lower_cloud = min(senkou_span_a, senkou_span_b)

            bullish = candle.ClosePrice > upper_cloud
            bearish = candle.ClosePrice < lower_cloud

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Only trade if price is clearly outside the cloud
            if bullish and self.Position <= 0:
                # Bullish signal - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif bearish and self.Position >= 0:
                # Bearish signal - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit condition - width returns to average or price enters cloud
        elif (self.Position > 0 or self.Position < 0) and (
                width < avg_width or (
                candle.ClosePrice > min(senkou_span_a, senkou_span_b) and
                candle.ClosePrice < max(senkou_span_a, senkou_span_b))):
            # Exit position when cloud width returns to normal or price enters cloud
            self.ClosePosition()

        # Update last values
        self._last_width = width
        self._last_avg_width = avg_width

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_width_breakout_strategy()
