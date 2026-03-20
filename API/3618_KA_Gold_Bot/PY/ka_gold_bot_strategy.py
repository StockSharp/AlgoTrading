import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ka_gold_bot_strategy(Strategy):
    def __init__(self):
        super(ka_gold_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._keltner_period = self.Param("KeltnerPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_short_period = self.Param("EmaShortPeriod", 10) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_long_period = self.Param("EmaLongPeriod", 50) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._ema_short = None
        self._ema_long = None
        self._range_queue = new()
        self._range_sum = 0.0
        self._ema_keltner = None
        self._prev_ema_short = None
        self._prev_ema_long = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ka_gold_bot_strategy, self).OnReseted()
        self._ema_short = None
        self._ema_long = None
        self._range_queue = new()
        self._range_sum = 0.0
        self._ema_keltner = None
        self._prev_ema_short = None
        self._prev_ema_long = None

    def OnStarted(self, time):
        super(ka_gold_bot_strategy, self).OnStarted(time)

        self.__ema_short = ExponentialMovingAverage()
        self.__ema_short.Length = self.ema_short_period
        self.__ema_long = ExponentialMovingAverage()
        self.__ema_long.Length = self.ema_long_period
        self.__ema_keltner = ExponentialMovingAverage()
        self.__ema_keltner.Length = self.keltner_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema_short, self.__ema_long, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ka_gold_bot_strategy()
