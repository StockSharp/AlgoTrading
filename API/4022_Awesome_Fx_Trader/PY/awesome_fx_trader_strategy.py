import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class awesome_fx_trader_strategy(Strategy):
    def __init__(self):
        super(awesome_fx_trader_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 8) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 13) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
        self._trend_lwma_period = self.Param("TrendLwmaPeriod", 34) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
        self._trend_smoothing_period = self.Param("TrendSmoothingPeriod", 6) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fast EMA", "Period of the fast EMA driving the oscillator", "Awesome Oscillator")

        self._fast_ema = None
        self._slow_ema = None
        self._trend_lwma = None
        self._previous_ao = 0.0
        self._has_previous_ao = False
        self._is_ao_increasing = False
        self._previous_lwma = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(awesome_fx_trader_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._trend_lwma = None
        self._previous_ao = 0.0
        self._has_previous_ao = False
        self._is_ao_increasing = False
        self._previous_lwma = 0.0

    def OnStarted(self, time):
        super(awesome_fx_trader_strategy, self).OnStarted(time)

        self.__fast_ema = EMA()
        self.__fast_ema.Length = self.fast_ema_period
        self.__slow_ema = EMA()
        self.__slow_ema.Length = self.slow_ema_period
        self.__trend_lwma = WeightedMovingAverage()
        self.__trend_lwma.Length = self.trend_lwma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ema, self.__slow_ema, self.__trend_lwma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return awesome_fx_trader_strategy()
