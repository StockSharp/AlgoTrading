import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class myfriend_forex_instruments_strategy(Strategy):
    def __init__(self):
        super(myfriend_forex_instruments_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 16) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 9) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(myfriend_forex_instruments_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(myfriend_forex_instruments_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period
        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = self.fast_period
        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._fast_sma, self._slow_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return myfriend_forex_instruments_strategy()
