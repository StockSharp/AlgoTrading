import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class russian20_time_filter_momentum_strategy(Strategy):
    def __init__(self):
        super(russian20_time_filter_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._moving_average_length = self.Param("MovingAverageLength", 20) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._momentum_period = self.Param("MomentumPeriod", 5) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._momentum_threshold = self.Param("MomentumThreshold", 100) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 20) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._use_time_filter = self.Param("UseTimeFilter", False) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._start_hour = self.Param("StartHour", 14) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._end_hour = self.Param("EndHour", 16) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")

        self._moving_average = None
        self._momentum = None
        self._previous_close = None
        self._entry_price = None
        self._pip_size = 0.0
        self._take_profit_offset = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(russian20_time_filter_momentum_strategy, self).OnReseted()
        self._moving_average = None
        self._momentum = None
        self._previous_close = None
        self._entry_price = None
        self._pip_size = 0.0
        self._take_profit_offset = 0.0

    def OnStarted(self, time):
        super(russian20_time_filter_momentum_strategy, self).OnStarted(time)

        self.__moving_average = SimpleMovingAverage()
        self.__moving_average.Length = self.moving_average_length
        self.__momentum = Momentum()
        self.__momentum.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__moving_average, self.__momentum, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return russian20_time_filter_momentum_strategy()
