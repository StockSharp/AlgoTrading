import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class ravi_iao_strategy(Strategy):
    def __init__(self):
        super(ravi_iao_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(10) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._slow_length = self.Param("SlowLength", 72) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._threshold = self.Param("Threshold", 0.3) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")

        self._ao_average = None
        self._prev_ravi = None
        self._prev_prev_ravi = None
        self._prev_ac = None
        self._prev_prev_ac = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ravi_iao_strategy, self).OnReseted()
        self._ao_average = None
        self._prev_ravi = None
        self._prev_prev_ravi = None
        self._prev_ac = None
        self._prev_prev_ac = None

    def OnStarted(self, time):
        super(ravi_iao_strategy, self).OnStarted(time)

        self._fast_ma = SMA()
        self._fast_ma.Length = self.fast_length
        self._slow_ma = SMA()
        self._slow_ma.Length = self.slow_length
        self.__ao_average = SMA()
        self.__ao_average.Length = 5

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(new IIndicator[] { fastMa, self._slow_ma, ao }, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ravi_iao_strategy()
