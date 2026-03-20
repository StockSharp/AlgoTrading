import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_macd_position_averaging_v2_strategy(Strategy):
    def __init__(self):
        super(ma_macd_position_averaging_v2_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 15) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 100) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 200) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicator")

        self._fast = None
        self._slow = None
        self._ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(ma_macd_position_averaging_v2_strategy, self).OnReseted()
        self._fast = None
        self._slow = None
        self._ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(ma_macd_position_averaging_v2_strategy, self).OnStarted(time)

        self.__fast = WeightedMovingAverage()
        self.__fast.Length = self.fast_period
        self.__slow = WeightedMovingAverage()
        self.__slow.Length = self.slow_period
        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ma_macd_position_averaging_v2_strategy()
