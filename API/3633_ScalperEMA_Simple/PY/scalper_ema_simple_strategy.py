import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class scalper_ema_simple_strategy(Strategy):
    def __init__(self):
        super(scalper_ema_simple_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 20) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._stoch_oversold = self.Param("StochOversold", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._stoch_overbought = self.Param("StochOverbought", 90) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(scalper_ema_simple_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(scalper_ema_simple_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_ema_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_ema_period
        self._stoch_k = StochasticK()
        self._stoch_k.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._stoch_k, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return scalper_ema_simple_strategy()
