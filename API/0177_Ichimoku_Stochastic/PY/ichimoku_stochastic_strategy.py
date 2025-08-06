import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import Ichimoku, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_stochastic_strategy(Strategy):
    """
    Strategy based on Ichimoku Cloud and Stochastic Oscillator indicators.
    Enters long when price is above Kumo (cloud), Tenkan > Kijun, and Stochastic is oversold (< 20)
    Enters short when price is below Kumo, Tenkan < Kijun, and Stochastic is overbought (> 80)

    """

    def __init__(self):
        super(ichimoku_stochastic_strategy, self).__init__()

        # Tenkan-sen period
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        # Kijun-sen period
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen line", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        # Senkou Span period
        self._senkou_period = self.Param("SenkouPeriod", 52) \
            .SetDisplay("Senkou Span Period", "Period for Senkou Span B line", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 60, 5)

        # Stochastic %K period
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        # Stochastic %K smoothing period
        self._stoch_k = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Smoothing for Stochastic %K line", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        # Stochastic %D period
        self._stoch_d = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Period for Stochastic %D line", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        # Candle type for strategy calculation
        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def TenkanPeriod(self):
        """Tenkan-sen period"""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-sen period"""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouPeriod(self):
        """Senkou Span period"""
        return self._senkou_period.Value

    @SenkouPeriod.setter
    def SenkouPeriod(self, value):
        self._senkou_period.Value = value

    @property
    def StochPeriod(self):
        """Stochastic %K period"""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochK(self):
        """Stochastic %K smoothing period"""
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochD(self):
        """Stochastic %D period"""
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation"""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ichimoku_stochastic_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(ichimoku_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouPeriod

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochK
        stochastic.D.Length = self.StochD

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ichimoku, stochastic, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)

            # Create a separate area for Stochastic
            stoch_area = self.CreateChartArea()
            if stoch_area is not None:
                self.DrawIndicator(stoch_area, stochastic)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value, stoch_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get additional values from Ichimoku

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

        # Current price (close of the candle)
        price = float(candle.ClosePrice)

        # Check if price is above/below Kumo cloud
        is_above_kumo = price > Math.Max(senkou_a, senkou_b)
        is_below_kumo = price < Math.Min(senkou_a, senkou_b)

        # Check Tenkan/Kijun cross (trend direction)
        is_bullish_cross = tenkan > kijun
        is_bearish_cross = tenkan < kijun


        # Get Stochastic %K value
        if stoch_value.K is None:
            return
        stochastic_k = float(stoch_value.K)

        # Trading logic
        if is_above_kumo and is_bullish_cross and stochastic_k < 20 and self.Position <= 0:
            # Buy signal: price above cloud, bullish cross, and oversold stochastic
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

            # Use Kijun-sen as stop-loss
            self.RegisterOrder(self.CreateOrder(Sides.Sell, kijun, Math.Max(Math.Abs(self.Position + self.Volume), self.Volume)))
        elif is_below_kumo and is_bearish_cross and stochastic_k > 80 and self.Position >= 0:
            # Sell signal: price below cloud, bearish cross, and overbought stochastic
            self.SellMarket(self.Volume + Math.Abs(self.Position))

            # Use Kijun-sen as stop-loss
            self.RegisterOrder(self.CreateOrder(Sides.Buy, kijun, Math.Max(Math.Abs(self.Position + self.Volume), self.Volume)))
        # Exit conditions
        elif price < kijun and self.Position > 0:
            # Exit long position when price falls below Kijun-sen
            self.SellMarket(self.Position)
        elif price > kijun and self.Position < 0:
            # Exit short position when price rises above Kijun-sen
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ichimoku_stochastic_strategy()
