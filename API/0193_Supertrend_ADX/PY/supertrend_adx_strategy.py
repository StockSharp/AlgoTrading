import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SuperTrend, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_adx_strategy(Strategy):
    """
    Strategy based on Supertrend indicator and ADX for trend strength confirmation.

    Entry criteria:
    Long: Price > Supertrend && ADX > 25 (uptrend with strong movement)
    Short: Price < Supertrend && ADX > 25 (downtrend with strong movement)

    Exit criteria:
    Long: Price < Supertrend (price falls below Supertrend)
    Short: Price > Supertrend (price rises above Supertrend)
    """

    def __init__(self):
        super(supertrend_adx_strategy, self).__init__()

        # Constructor.
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Period for ATR calculation in Supertrend", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 5)

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 1.0)

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20.0, 30.0, 5.0)

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._last_supertrend = 0
        self._is_above_supertrend = False

    @property
    def SupertrendPeriod(self):
        """Period for Supertrend calculation."""
        return self._supertrend_period.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrend_period.Value = value

    @property
    def SupertrendMultiplier(self):
        """Multiplier for Supertrend calculation."""
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

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

    def OnReseted(self):
        super(supertrend_adx_strategy, self).OnReseted()

        self._last_supertrend = 0
        self._is_above_supertrend = False

    def OnStarted(self, time):
        super(supertrend_adx_strategy, self).OnStarted(time)

        # Create indicators
        atr = AverageTrueRange()
        atr.Length = self.SupertrendPeriod
        supertrend = SuperTrend()
        supertrend.Length = self.SupertrendPeriod
        supertrend.Multiplier = self.SupertrendMultiplier
        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        # Create subscription and bind ATR to Supertrend
        subscription = self.SubscribeCandles(self.CandleType)

        # Process candles with Supertrend and ADX indicators
        subscription.BindEx(supertrend, adx, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, supertrend_value, adx_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return


        # Determine current position relative to Supertrend
        is_above_supertrend = candle.ClosePrice > supertrend_value.Value
        is_strong_trend = adx_value.MovingAverage > self.AdxThreshold

        # Log current state
        self.LogInfo(f"Close: {candle.ClosePrice}, Supertrend: {supertrend_value}, ADX: {adx_value}, Above: {is_above_supertrend}, Strong Trend: {is_strong_trend}")

        # Check for trend change (crossing Supertrend line)
        trend_changed = is_above_supertrend != self._is_above_supertrend and self._last_supertrend > 0

        # Trading logic
        if self.Position == 0:  # No position
            if is_above_supertrend and is_strong_trend:
                # Buy signal
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Buy signal: Price above Supertrend with strong trend (ADX: {adx_value})")
            elif not is_above_supertrend and is_strong_trend:
                # Sell signal
                self.SellMarket(self.Volume)
                self.LogInfo(f"Sell signal: Price below Supertrend with strong trend (ADX: {adx_value})")
        elif trend_changed:  # Exit on trend change
            if self.Position > 0 and not is_above_supertrend:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit long position: Price crossed below Supertrend ({supertrend_value})")
            elif self.Position < 0 and is_above_supertrend:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit short position: Price crossed above Supertrend ({supertrend_value})")

        # Save current state
        self._last_supertrend = supertrend_value.Value
        self._is_above_supertrend = is_above_supertrend

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_adx_strategy()
