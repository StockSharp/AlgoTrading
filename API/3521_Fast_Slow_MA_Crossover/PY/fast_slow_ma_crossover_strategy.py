import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy


class fast_slow_ma_crossover_strategy(Strategy):
    def __init__(self):
        super(fast_slow_ma_crossover_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 30) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._slow_ma_period = self.Param("SlowMaPeriod", 80) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._take_profit_pips = self.Param("TakeProfitPips", 80) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._stop_loss_pips = self.Param("StopLossPips", 80) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._start_time = self.Param("StartTime", new TimeSpan(8, 0, 0) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._stop_time = self.Param("StopTime", new TimeSpan(18, 0, 0) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(120) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")

        self._pip_size = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._last_signal_time = None
        self._has_active_position = False
        self._is_long_position = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fast_slow_ma_crossover_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._last_signal_time = None
        self._has_active_position = False
        self._is_long_position = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._target_price = 0.0

    def OnStarted(self, time):
        super(fast_slow_ma_crossover_strategy, self).OnStarted(time)

        self._fast_ma = EMA()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = EMA()
        self._slow_ma.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fast_slow_ma_crossover_strategy()
