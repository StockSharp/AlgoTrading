import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class starter_v6_mod_e_strategy(Strategy):
    def __init__(self):
        super(starter_v6_mod_e_strategy, self).__init__()

        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._laguerre_gamma = self.Param("LaguerreGamma", 0.7) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._laguerre_oversold = self.Param("LaguerreOversold", 0.5) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._laguerre_overbought = self.Param("LaguerreOverbought", 0.5) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_laguerre = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(starter_v6_mod_e_strategy, self).OnReseted()
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_laguerre = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(starter_v6_mod_e_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_ema_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return starter_v6_mod_e_strategy()
