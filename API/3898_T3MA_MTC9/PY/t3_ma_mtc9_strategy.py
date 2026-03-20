import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class t3_ma_mtc9_strategy(Strategy):
    def __init__(self):
        super(t3_ma_mtc9_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 150) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")

        self._prev_ema = 0.0
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(t3_ma_mtc9_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(t3_ma_mtc9_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._mom = Momentum()
        self._mom.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._mom, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return t3_ma_mtc9_strategy()
