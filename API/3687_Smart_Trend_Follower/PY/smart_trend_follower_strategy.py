import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class smart_trend_follower_strategy(Strategy):
    def __init__(self):
        super(smart_trend_follower_strategy, self).__init__()

        self._signal_mode = self.Param("SignalMode", SignalModes.CrossMa) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._initial_volume = self.Param("InitialVolume", 1) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._multiplier = self.Param("Multiplier", 2) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._layer_distance_pips = self.Param("LayerDistancePips", 200) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._slow_period = self.Param("SlowPeriod", 28) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._stochastic_slowing = self.Param("StochasticSlowing", 3) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 0) \
            .SetDisplay("Signal Mode", "Trading logic selection", "Signals")

        self._fast_sma = None
        self._slow_sma = None
        self._stochastic = None
        self._long_entries = new()
        self._short_entries = new()
        self._prev_fast = None
        self._prev_slow = None
        self._pip_size = 0.0
        self._long_exit_requested = False
        self._short_exit_requested = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smart_trend_follower_strategy, self).OnReseted()
        self._fast_sma = None
        self._slow_sma = None
        self._stochastic = None
        self._long_entries = new()
        self._short_entries = new()
        self._prev_fast = None
        self._prev_slow = None
        self._pip_size = 0.0
        self._long_exit_requested = False
        self._short_exit_requested = False

    def OnStarted(self, time):
        super(smart_trend_follower_strategy, self).OnStarted(time)

        self.__fast_sma = SimpleMovingAverage()
        self.__fast_sma.Length = Math.Max(1
        self.__slow_sma = SimpleMovingAverage()
        self.__slow_sma.Length = Math.Max(1

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__fast_sma, self.__slow_sma, _stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return smart_trend_follower_strategy()
