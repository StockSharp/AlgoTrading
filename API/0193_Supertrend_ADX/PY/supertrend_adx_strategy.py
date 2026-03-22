import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_adx_strategy(Strategy):
    """
    Strategy based on Supertrend indicator trend direction changes.
    Trades on supertrend trend flips with cooldown.
    """

    def __init__(self):
        super(supertrend_adx_strategy, self).__init__()

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Period for ATR calculation in Supertrend", "Indicators")

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Indicators")

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 30.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetRange(1, 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._last_supertrend = 0
        self._is_above_supertrend = False
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_adx_strategy, self).OnReseted()
        self._last_supertrend = 0
        self._is_above_supertrend = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(supertrend_adx_strategy, self).OnStarted(time)
        self._last_supertrend = 0
        self._is_above_supertrend = False
        self._cooldown = 0

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = self._supertrend_multiplier.Value
        dummyEma = ExponentialMovingAverage()
        dummyEma.Length = 10

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(supertrend, dummyEma, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, st_val, dummy_val):
        if candle.State != CandleStates.Finished:
            return

        is_up_trend = st_val.IsUpTrend
        trend_changed = is_up_trend != self._is_above_supertrend and self._last_supertrend > 0

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and trend_changed:
            if is_up_trend and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = cooldown_val
            elif not is_up_trend and self.Position >= 0:
                self.SellMarket()
                self._cooldown = cooldown_val

        self._last_supertrend = 1
        self._is_above_supertrend = is_up_trend

    def CreateClone(self):
        return supertrend_adx_strategy()
