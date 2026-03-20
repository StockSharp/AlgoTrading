import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mean_reverse_strategy(Strategy):
    def __init__(self):
        super(mean_reverse_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 20) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 50) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 1000) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")

        self._fast_ma = null!
        self._slow_ma = null!
        self._atr = null!
        self._prev_fast_ma = None
        self._prev_slow_ma = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reverse_strategy, self).OnReseted()
        self._fast_ma = null!
        self._slow_ma = null!
        self._atr = null!
        self._prev_fast_ma = None
        self._prev_slow_ma = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(mean_reverse_strategy, self).OnStarted(time)

        self.__fast_ma = SimpleMovingAverage()
        self.__fast_ma.Length = self.fast_ma_period
        self.__slow_ma = SimpleMovingAverage()
        self.__slow_ma.Length = self.slow_ma_period
        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__fast_ma, self.__slow_ma, self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mean_reverse_strategy()
