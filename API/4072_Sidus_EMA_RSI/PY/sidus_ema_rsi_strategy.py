import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class sidus_ema_rsi_strategy(Strategy):
    def __init__(self):
        super(sidus_ema_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._slow_period = self.Param("SlowPeriod", 12) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_period = self.Param("RsiPeriod", 21) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sidus_ema_rsi_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(sidus_ema_rsi_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return sidus_ema_rsi_strategy()
