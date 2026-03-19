import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class altcoin_index_correlation_strategy(Strategy):
    """
    Altcoin Index Correlation Strategy - trades when EMA trends on the symbol
    and reference index align. Simplified to single-security EMA crossover.
    """

    def __init__(self):
        super(altcoin_index_correlation_strategy, self).__init__()

        self._fast_ema_len = self.Param("FastEmaLength", 7) \
            .SetDisplay("Fast EMA", "Fast EMA length", "EMA Settings")
        self._slow_ema_len = self.Param("SlowEmaLength", 18) \
            .SetDisplay("Slow EMA", "Slow EMA length", "EMA Settings")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def FastEmaLength(self): return self._fast_ema_len.Value
    @FastEmaLength.setter
    def FastEmaLength(self, v): self._fast_ema_len.Value = v
    @property
    def SlowEmaLength(self): return self._slow_ema_len.Value
    @SlowEmaLength.setter
    def SlowEmaLength(self, v): self._slow_ema_len.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(altcoin_index_correlation_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(altcoin_index_correlation_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        cooldown_ok = self._bar_index - self._last_trade_bar > 5
        cross_over = self._prev_fast <= self._prev_slow and fast > slow
        cross_under = self._prev_fast >= self._prev_slow and fast < slow

        if cross_over and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_under and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return altcoin_index_correlation_strategy()
