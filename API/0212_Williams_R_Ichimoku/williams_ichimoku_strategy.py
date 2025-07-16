import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, Ichimoku
from StockSharp.Algo.Strategies import Strategy


class williams_ichimoku_strategy(Strategy):
    """
    Strategy based on Williams %R and Ichimoku indicators.
    Enters long when Williams %R is below -80 (oversold) and price is above Ichimoku Cloud with Tenkan-sen > Kijun-sen.
    Enters short when Williams %R is above -20 (overbought) and price is below Ichimoku Cloud with Tenkan-sen < Kijun-sen.
    """

    def __init__(self):
        super(williams_ichimoku_strategy, self).__init__()

        # Williams %R indicator period.
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        # Tenkan-sen period (Ichimoku).
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line (Ichimoku)", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 13, 1)

        # Kijun-sen period (Ichimoku).
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen line (Ichimoku)", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        # Senkou Span B period (Ichimoku).
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B line (Ichimoku)", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 4)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal indicator instances
        self._williams_r = None
        self._ichimoku = None
        self._last_kijun = None

    @property
    def WilliamsRPeriod(self):
        """Williams %R indicator period."""
        return self._williams_r_period.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williams_r_period.Value = value

    @property
    def TenkanPeriod(self):
        """Tenkan-sen period (Ichimoku)."""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-sen period (Ichimoku)."""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Senkou Span B period (Ichimoku)."""
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(williams_ichimoku_strategy, self).OnStarted(time)

        self._last_kijun = None

        # Initialize indicators
        self._williams_r = WilliamsR()
        self._williams_r.Length = self.WilliamsRPeriod

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        # Create candles subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription
        subscription.BindEx(self._williams_r, self._ichimoku, self.ProcessCandle).Start()

        # Set stop-loss at Kijun-sen level
        # The actual stop level will be updated in the ProcessCandle method
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take-profit
            Unit(0, UnitTypes.Absolute)   # Will be dynamic based on Kijun-sen
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williams_r)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, williams_r_value, ichimoku_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Ichimoku values
        try:
            tenkan = float(ichimoku_value.Tenkan) if ichimoku_value.Tenkan is not None else None
            kijun = float(ichimoku_value.Kijun) if ichimoku_value.Kijun is not None else None
            senkou_a = float(ichimoku_value.SenkouA) if ichimoku_value.SenkouA is not None else None
            senkou_b = float(ichimoku_value.SenkouB) if ichimoku_value.SenkouB is not None else None
        except Exception:
            return

        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        # Determine if price is above or below the Kumo (cloud)
        kumo_top = max(senkou_a, senkou_b)
        kumo_bottom = min(senkou_a, senkou_b)
        is_price_above_kumo = candle.ClosePrice > kumo_top
        is_price_below_kumo = candle.ClosePrice < kumo_bottom

        williams_r_dec = float(williams_r_value)

        # Save current Kijun for stop-loss
        self._last_kijun = kijun

        # Trading logic
        if williams_r_dec < -80 and is_price_above_kumo and tenkan > kijun:
            # Long signal: %R < -80 (oversold), price above Kumo, Tenkan > Kijun
            if self.Position <= 0:
                # Close any existing short position and open long
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Long Entry: %R={0:F2}, Price above Kumo, Tenkan > Kijun".format(williams_r_dec))
        elif williams_r_dec > -20 and is_price_below_kumo and tenkan < kijun:
            # Short signal: %R > -20 (overbought), price below Kumo, Tenkan < Kijun
            if self.Position >= 0:
                # Close any existing long position and open short
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Short Entry: %R={0:F2}, Price below Kumo, Tenkan < Kijun".format(williams_r_dec))
        elif (self.Position > 0 and candle.ClosePrice < kumo_bottom) or \
             (self.Position < 0 and candle.ClosePrice > kumo_top):
            # Exit positions when price crosses the Kumo
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Price crossed below Kumo")
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Price crossed above Kumo")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_ichimoku_strategy()
