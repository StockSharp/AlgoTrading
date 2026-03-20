import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class elderv30aug05v_strategy(Strategy):
    def __init__(self):
        super(elderv30aug05v_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")

        self._prev_ema = 0.0
        self._prev_momentum = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elderv30aug05v_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_momentum = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(elderv30aug05v_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._momentum = Momentum()
        self._momentum.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._momentum, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return elderv30aug05v_strategy()
