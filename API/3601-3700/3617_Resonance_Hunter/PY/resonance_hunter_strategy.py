import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class resonance_hunter_strategy(Strategy):
    def __init__(self):
        super(resonance_hunter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_k_period = self.Param("FastKPeriod", 8)
        self._slow_k_period = self.Param("SlowKPeriod", 21)
        self._d_period = self.Param("DPeriod", 3)

        self._highs = []
        self._lows = []
        self._bar_count = 0
        self._fast_k_history = []
        self._slow_k_history = []
        self._prev_fast_k = None
        self._prev_slow_k = None
        self._prev_fast_d = None
        self._prev_slow_d = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastKPeriod(self):
        return self._fast_k_period.Value

    @FastKPeriod.setter
    def FastKPeriod(self, value):
        self._fast_k_period.Value = value

    @property
    def SlowKPeriod(self):
        return self._slow_k_period.Value

    @SlowKPeriod.setter
    def SlowKPeriod(self, value):
        self._slow_k_period.Value = value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    def OnReseted(self):
        super(resonance_hunter_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._bar_count = 0
        self._fast_k_history = []
        self._slow_k_history = []
        self._prev_fast_k = None
        self._prev_slow_k = None
        self._prev_fast_d = None
        self._prev_slow_d = None

    def OnStarted2(self, time):
        super(resonance_hunter_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []
        self._bar_count = 0
        self._fast_k_history = []
        self._slow_k_history = []
        self._prev_fast_k = None
        self._prev_slow_k = None
        self._prev_fast_d = None
        self._prev_slow_d = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _calc_stochastic_k(self, close, period):
        if self._bar_count < period:
            return None
        highs_slice = self._highs[-period:]
        lows_slice = self._lows[-period:]
        hh = max(highs_slice)
        ll = min(lows_slice)
        r = hh - ll
        if r <= 0:
            return 50.0
        return (close - ll) / r * 100.0

    def _add_to_smoothing(self, history, value, period):
        history.append(value)
        while len(history) > period:
            history.pop(0)
        if len(history) < period:
            return None
        return sum(history) / len(history)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._highs.append(high)
        self._lows.append(low)
        self._bar_count += 1

        # Keep buffer manageable
        max_buf = max(self.SlowKPeriod, self.FastKPeriod) + 10
        while len(self._highs) > max_buf:
            self._highs.pop(0)
            self._lows.pop(0)

        if self._bar_count < self.SlowKPeriod:
            return

        fast_k = self._calc_stochastic_k(close, self.FastKPeriod)
        slow_k = self._calc_stochastic_k(close, self.SlowKPeriod)

        if fast_k is None or slow_k is None:
            return

        d_period = self.DPeriod
        fast_d = self._add_to_smoothing(self._fast_k_history, fast_k, d_period)
        slow_d = self._add_to_smoothing(self._slow_k_history, slow_k, d_period)

        if (fast_d is None or slow_d is None or
                self._prev_fast_k is None or self._prev_slow_k is None or
                self._prev_fast_d is None or self._prev_slow_d is None):
            self._prev_fast_k = fast_k
            self._prev_slow_k = slow_k
            self._prev_fast_d = fast_d
            self._prev_slow_d = slow_d
            return

        # Resonance buy: both stochastics cross above their D lines
        fast_bull_cross = self._prev_fast_k < self._prev_fast_d and fast_k > fast_d
        slow_bull_cross = self._prev_slow_k < self._prev_slow_d and slow_k > slow_d
        both_oversold = fast_k < 30 and slow_k < 30

        # Resonance sell: both stochastics cross below their D lines
        fast_bear_cross = self._prev_fast_k > self._prev_fast_d and fast_k < fast_d
        slow_bear_cross = self._prev_slow_k > self._prev_slow_d and slow_k < slow_d
        both_overbought = fast_k > 70 and slow_k > 70

        buy_signal = (fast_bull_cross and (slow_bull_cross or slow_k > slow_d)) and both_oversold
        sell_signal = (fast_bear_cross and (slow_bear_cross or slow_k < slow_d)) and both_overbought

        if buy_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_fast_k = fast_k
        self._prev_slow_k = slow_k
        self._prev_fast_d = fast_d
        self._prev_slow_d = slow_d

    def CreateClone(self):
        return resonance_hunter_strategy()
