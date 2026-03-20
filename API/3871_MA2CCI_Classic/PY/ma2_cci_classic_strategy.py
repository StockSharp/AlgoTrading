import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma2_cci_classic_strategy(Strategy):
    def __init__(self):
        super(ma2_cci_classic_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma2_cci_classic_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ma2_cci_classic_strategy, self).OnStarted(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.slow_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ma2_cci_classic_strategy()
