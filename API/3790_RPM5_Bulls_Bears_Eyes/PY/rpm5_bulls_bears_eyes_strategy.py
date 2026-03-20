import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rpm5_bulls_bears_eyes_strategy(Strategy):
    def __init__(self):
        super(rpm5_bulls_bears_eyes_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._power_period = self.Param("PowerPeriod", 13) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")

        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rpm5_bulls_bears_eyes_strategy, self).OnReseted()
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(rpm5_bulls_bears_eyes_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._bulls = BullPower()
        self._bulls.Length = self.power_period
        self._bears = BearPower()
        self._bears.Length = self.power_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._bulls, self._bears, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rpm5_bulls_bears_eyes_strategy()
