import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class three_indicators_strategy(Strategy):
    def __init__(self):
        super(three_indicators_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 11) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 53) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 26) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 40) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 23) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 2) \
            .SetDisplay("Candle type", "Primary timeframe", "General")

        self._previous_open = None
        self._previous_macd_main = None
        self._previous_composite_signal = 0.0
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_indicators_strategy, self).OnReseted()
        self._previous_open = None
        self._previous_macd_main = None
        self._previous_composite_signal = 0.0
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(three_indicators_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, stochastic, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return three_indicators_strategy()
