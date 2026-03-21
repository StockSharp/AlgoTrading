import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class x2_ma_jfatl_strategy(Strategy):
    def __init__(self):
        super(x2_ma_jfatl_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")
        self._slow_length = self.Param("SlowLength", 13) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")
        self._filter_length = self.Param("FilterLength", 21) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")

        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x2_ma_jfatl_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0.0

    def OnStarted(self, time):
        super(x2_ma_jfatl_strategy, self).OnStarted(time)

        self._fast_ma = SMA()
        self._fast_ma.Length = self.fast_length
        self._slow_ma = JurikMovingAverage()
        self._slow_ma.Length = self.slow_length
        self._filter_ma = JurikMovingAverage()
        self._filter_ma.Length = self.filter_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self._filter_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return x2_ma_jfatl_strategy()
