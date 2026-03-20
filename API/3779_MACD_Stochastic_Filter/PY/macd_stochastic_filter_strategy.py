import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class macd_stochastic_filter_strategy(Strategy):
    def __init__(self):
        super(macd_stochastic_filter_strategy, self).__init__()

        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 26) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._stoch_k_length = self.Param("StochKLength", 14) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._stoch_d_length = self.Param("StochDLength", 3) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")

        self._prev_macd = None
        self._prev_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_stochastic_filter_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_signal = None

    def OnStarted(self, time):
        super(macd_stochastic_filter_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, stochastic, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_stochastic_filter_strategy()
