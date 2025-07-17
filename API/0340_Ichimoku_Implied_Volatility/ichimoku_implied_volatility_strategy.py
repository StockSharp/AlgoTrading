import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

import random


class ichimoku_implied_volatility_strategy(Strategy):
    """
    Ichimoku with Implied Volatility strategy.
    Entry condition:
    Long: Price > Kumo && Tenkan > Kijun && IV > Avg(IV, N)
    Short: Price < Kumo && Tenkan < Kijun && IV > Avg(IV, N)
    Exit condition:
    Long: Price < Kumo
    Short: Price > Kumo
    """

    def __init__(self):
        super(ichimoku_implied_volatility_strategy, self).__init__()

        # Tenkan-Sen period.
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-Sen Period", "Tenkan-Sen (Conversion Line) period", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 13, 2)

        # Kijun-Sen period.
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-Sen Period", "Kijun-Sen (Base Line) period", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        # Senkou Span B period.
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (2nd Leading Span) period", "Ichimoku Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 4)

        # Implied Volatility averaging period.
        self._iv_period = self.Param("IVPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Type of candles to use.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._implied_volatility_history = []
        self._avg_implied_volatility = 0.0
        self._prev_price = 0.0
        self._prev_above_kumo = False
        self._prev_tenkan_above_kijun = False

    @property
    def TenkanPeriod(self):
        """Tenkan-Sen period."""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-Sen period."""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Senkou Span B period."""
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def IVPeriod(self):
        """Implied Volatility averaging period."""
        return self._iv_period.Value

    @IVPeriod.setter
    def IVPeriod(self, value):
        self._iv_period.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ichimoku_implied_volatility_strategy, self).OnStarted(time)

        self._prev_above_kumo = False
        self._prev_tenkan_above_kijun = False
        self._prev_price = 0.0
        self._avg_implied_volatility = 0.0
        self._implied_volatility_history.clear()

        # Create Ichimoku indicator
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.BindEx(ichimoku, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

        # Enable position protection using Kijun-Sen as stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(0)
        )
    def ProcessCandle(self, candle, ichimoku_value):
        """Process each candle and Ichimoku values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get Ichimoku values
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

        # Determine if price is above Kumo (cloud)
        kumo_top = Math.Max(senkou_a, senkou_b)
        kumo_bottom = Math.Min(senkou_a, senkou_b)
        price_above_kumo = candle.ClosePrice > kumo_top
        price_below_kumo = candle.ClosePrice < kumo_bottom

        # Check Tenkan/Kijun cross
        tenkan_above_kijun = tenkan > kijun

        # Update Implied Volatility (in a real system, this would come from market data)
        self.UpdateImpliedVolatility(candle)

        # Check IV condition
        iv_higher_than_average = self.GetImpliedVolatility() > self._avg_implied_volatility

        # First run, just store values
        if self._prev_price == 0:
            self._prev_price = candle.ClosePrice
            self._prev_above_kumo = price_above_kumo
            self._prev_tenkan_above_kijun = tenkan_above_kijun
            return

        # Trading logic based on Ichimoku and IV

        # Long entry condition
        if price_above_kumo and tenkan_above_kijun and iv_higher_than_average and self.Position <= 0:
            self.LogInfo("Long signal: Price above Kumo, Tenkan above Kijun, IV elevated")
            self.BuyMarket(self.Volume)
        # Short entry condition
        elif price_below_kumo and not tenkan_above_kijun and iv_higher_than_average and self.Position >= 0:
            self.LogInfo("Short signal: Price below Kumo, Tenkan below Kijun, IV elevated")
            self.SellMarket(self.Volume)

        # Exit conditions

        # Exit long if price falls below Kumo
        if self.Position > 0 and not price_above_kumo:
            self.LogInfo("Exit long: Price fell below Kumo")
            self.SellMarket(Math.Abs(self.Position))
        # Exit short if price rises above Kumo
        elif self.Position < 0 and not price_below_kumo:
            self.LogInfo("Exit short: Price rose above Kumo")
            self.BuyMarket(Math.Abs(self.Position))

        # Use Kijun-Sen as trailing stop
        self.ApplyKijunAsStop(candle.ClosePrice, kijun)

        # Update previous values
        self._prev_price = candle.ClosePrice
        self._prev_above_kumo = price_above_kumo
        self._prev_tenkan_above_kijun = tenkan_above_kijun

    def UpdateImpliedVolatility(self, candle):
        """Update implied volatility value.
        In a real implementation, this would fetch data from market."""
        # Simple IV simulation based on candle's high-low range
        # In reality, this would come from option pricing data
        iv = (candle.HighPrice - candle.LowPrice) / candle.OpenPrice * 100

        # Add some random fluctuation to simulate IV behavior
        iv *= 0.8 + 0.4 * random.random()

        # Add to history and maintain history length
        self._implied_volatility_history.append(iv)
        if len(self._implied_volatility_history) > self.IVPeriod:
            self._implied_volatility_history.pop(0)

        # Calculate average IV
        if self._implied_volatility_history:
            self._avg_implied_volatility = sum(self._implied_volatility_history) / len(self._implied_volatility_history)
        else:
            self._avg_implied_volatility = 0

        self.LogInfo(f"IV: {iv}, Avg IV: {self._avg_implied_volatility}")

    def GetImpliedVolatility(self):
        """Get current implied volatility."""
        return self._implied_volatility_history[-1] if self._implied_volatility_history else 0

    def ApplyKijunAsStop(self, price, kijun):
        """Use Kijun-Sen as a trailing stop level."""
        # Long position: exit if price drops below Kijun
        if self.Position > 0 and price < kijun:
            self.LogInfo("Kijun-Sen stop triggered for long position")
            self.SellMarket(Math.Abs(self.Position))
        # Short position: exit if price rises above Kijun
        elif self.Position < 0 and price > kijun:
            self.LogInfo("Kijun-Sen stop triggered for short position")
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_implied_volatility_strategy()
