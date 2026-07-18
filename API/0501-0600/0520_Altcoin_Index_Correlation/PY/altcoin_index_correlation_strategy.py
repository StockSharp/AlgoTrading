import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *

class altcoin_index_correlation_strategy(Strategy):
    """
    Altcoin Index Correlation Strategy - trades when EMA trends on the symbol
    and reference index align.
    """

    def __init__(self):
        super(altcoin_index_correlation_strategy, self).__init__()

        self._fast_ema_len = self.Param("FastEmaLength", 7) \
            .SetDisplay("Fast EMA", "Fast EMA length", "EMA Settings")
        self._slow_ema_len = self.Param("SlowEmaLength", 18) \
            .SetDisplay("Slow EMA", "Slow EMA length", "EMA Settings")
        self._index_fast_ema_len = self.Param("IndexFastEmaLength", 47) \
            .SetDisplay("Index Fast EMA", "Fast EMA length for index", "Index Reference")
        self._index_slow_ema_len = self.Param("IndexSlowEmaLength", 50) \
            .SetDisplay("Index Slow EMA", "Slow EMA length for index", "Index Reference")
        self._skip_index = self.Param("SkipIndexReference", False) \
            .SetDisplay("Skip Index", "Ignore index correlation", "Index Reference")
        self._inverse_signal = self.Param("InverseSignal", False) \
            .SetDisplay("Inverse Signal", "Use inverse correlation logic", "Index Reference")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._index_security_param = self.Param[Security]("IndexSecurity") \
            .SetDisplay("Index Security", "Reference index security", "Data")
        self._index_fast = 0.0
        self._index_slow = 0.0
        self._index_ready = False
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
    def IndexFastEmaLength(self): return self._index_fast_ema_len.Value
    @IndexFastEmaLength.setter
    def IndexFastEmaLength(self, v): self._index_fast_ema_len.Value = v
    @property
    def IndexSlowEmaLength(self): return self._index_slow_ema_len.Value
    @IndexSlowEmaLength.setter
    def IndexSlowEmaLength(self, v): self._index_slow_ema_len.Value = v
    @property
    def SkipIndexReference(self): return self._skip_index.Value
    @SkipIndexReference.setter
    def SkipIndexReference(self, v): self._skip_index.Value = v
    @property
    def InverseSignal(self): return self._inverse_signal.Value
    @InverseSignal.setter
    def InverseSignal(self, v): self._inverse_signal.Value = v
    @property
    def IndexSecurity(self): return self._index_security_param.Value
    @IndexSecurity.setter
    def IndexSecurity(self, v): self._index_security_param.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(altcoin_index_correlation_strategy, self).OnReseted()
        self._index_fast = 0.0
        self._index_slow = 0.0
        self._index_ready = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(altcoin_index_correlation_strategy, self).OnStarted2(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessMainCandle).Start()

        if self.IndexSecurity is not None:
            index_fast_ema = ExponentialMovingAverage()
            index_fast_ema.Length = self.IndexFastEmaLength
            index_slow_ema = ExponentialMovingAverage()
            index_slow_ema.Length = self.IndexSlowEmaLength

            index_sub = self.SubscribeCandles(self.CandleType, security=self._index_security)
            index_sub.Bind(index_fast_ema, index_slow_ema, self.ProcessIndexCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def ProcessIndexCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        self._index_fast = float(fast)
        self._index_slow = float(slow)
        self._index_ready = True

    def ProcessMainCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast)
        slow = float(slow)

        self._bar_index += 1

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        cooldown_ok = self._bar_index - self._last_trade_bar > 5

        cross_over = self._prev_fast <= self._prev_slow and fast > slow
        cross_under = self._prev_fast >= self._prev_slow and fast < slow

        if self.SkipIndexReference or not self._index_ready:
            go_long = cross_over
            go_short = cross_under
        else:
            go_long = cross_over and self._index_fast > self._index_slow
            go_short = cross_under and self._index_fast < self._index_slow

            if self.InverseSignal:
                go_long = cross_over and self._index_fast < self._index_slow
                go_short = cross_under and self._index_fast > self._index_slow

        if go_long and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif go_short and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return altcoin_index_correlation_strategy()
