import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ichimoku_volatility_contraction_strategy(Strategy):
    """
    Ichimoku with Volatility Contraction strategy.
    Enters positions when Ichimoku signals a trend and volatility is contracting.
    """

    def __init__(self):
        super(ichimoku_volatility_contraction_strategy, self).__init__()

        # Initialize strategy parameters
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 11, 1)

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 4)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for Average True Range calculation", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._deviation_factor = self.Param("DeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Factor", "Factor multiplied by standard deviation to detect volatility contraction", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state variables for ATR statistics
        self._avg_atr = 0
        self._atr_std_dev = 0
        self._processed_candles = 0

    @property
    def tenkan_period(self):
        """Tenkan-sen (Conversion Line) period."""
        return self._tenkan_period.Value

    @tenkan_period.setter
    def tenkan_period(self, value):
        self._tenkan_period.Value = value

    @property
    def kijun_period(self):
        """Kijun-sen (Base Line) period."""
        return self._kijun_period.Value

    @kijun_period.setter
    def kijun_period(self, value):
        self._kijun_period.Value = value

    @property
    def senkou_span_b_period(self):
        """Senkou Span B (Leading Span B) period."""
        return self._senkou_span_b_period.Value

    @senkou_span_b_period.setter
    def senkou_span_b_period(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def atr_period(self):
        """ATR period for volatility calculation."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def deviation_factor(self):
        """Deviation factor for volatility contraction detection."""
        return self._deviation_factor.Value

    @deviation_factor.setter
    def deviation_factor(self, value):
        self._deviation_factor.Value = value

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
        """Initialize strategy."""
        super(ichimoku_volatility_contraction_strategy, self).OnStarted(time)

        # Initialize values
        self._avg_atr = 0
        self._atr_std_dev = 0
        self._processed_candles = 0

        # Create Ichimoku indicator
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.tenkan_period
        ichimoku.Kijun.Length = self.kijun_period
        ichimoku.SenkouB.Length = self.senkou_span_b_period

        # Create ATR indicator for volatility measurement
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to the subscription
        subscription.BindEx(ichimoku, atr, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Get ATR value and calculate statistics
        current_atr = to_float(atr_value)
        self._processed_candles += 1

        # Using exponential moving average approach for ATR statistics
        if self._processed_candles == 1:
            self._avg_atr = current_atr
            self._atr_std_dev = 0
        else:
            # Update average ATR with smoothing factor
            alpha = 2.0 / (self.atr_period + 1)
            old_avg = self._avg_atr
            self._avg_atr = alpha * current_atr + (1 - alpha) * self._avg_atr

            # Update standard deviation (simplified approach)
            atr_dev = abs(current_atr - old_avg)
            self._atr_std_dev = alpha * atr_dev + (1 - alpha) * self._atr_std_dev

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Ichimoku values
        ichimoku_typed = ichimoku_value

        if ichimoku_typed.Tenkan is None:
            return
        tenkan = float(ichimoku_typed.Tenkan)

        if ichimoku_typed.Kijun is None:
            return
        kijun = float(ichimoku_typed.Kijun)

        if ichimoku_typed.SenkouA is None:
            return
        senkou_a = float(ichimoku_typed.SenkouA)

        if ichimoku_typed.SenkouB is None:
            return
        senkou_b = float(ichimoku_typed.SenkouB)

        # Determine Kumo (cloud) boundaries
        upper_kumo = max(senkou_a, senkou_b)
        lower_kumo = min(senkou_a, senkou_b)

        # Check for volatility contraction
        is_volatility_contraction = current_atr < (self._avg_atr - self.deviation_factor * self._atr_std_dev)

        # Log the values
        self.LogInfo(
            f"Tenkan: {tenkan}, Kijun: {kijun}, Cloud: {lower_kumo}-{upper_kumo}, "
            f"ATR: {current_atr}, Avg ATR: {self._avg_atr}, Contraction: {is_volatility_contraction}"
        )

        # Trading logic with volatility contraction filter
        if is_volatility_contraction:
            # Bullish signal: Price above cloud and Tenkan above Kijun
            if candle.ClosePrice > upper_kumo and tenkan > kijun and self.Position <= 0:
                # Close any existing short position
                if self.Position < 0:
                    self.BuyMarket(abs(self.Position))
                # Open long position
                self.BuyMarket(self.Volume)
                self.LogInfo(
                    f"Long signal: Price ({candle.ClosePrice}) above cloud, Tenkan ({tenkan}) > Kijun ({kijun}) with volatility contraction"
                )
            # Bearish signal: Price below cloud and Tenkan below Kijun
            elif candle.ClosePrice < lower_kumo and tenkan < kijun and self.Position >= 0:
                # Close any existing long position
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                # Open short position
                self.SellMarket(self.Volume)
                self.LogInfo(
                    f"Short signal: Price ({candle.ClosePrice}) below cloud, Tenkan ({tenkan}) < Kijun ({kijun}) with volatility contraction"
                )

        # Exit logic
        if (self.Position > 0 and candle.ClosePrice < lower_kumo) or (self.Position < 0 and candle.ClosePrice > upper_kumo):
            # Close position when price crosses the cloud in the opposite direction
            self.ClosePosition()
            self.LogInfo(
                f"Exit signal: Price exited cloud in opposite direction. Position closed at {candle.ClosePrice}"
            )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_volatility_contraction_strategy()
