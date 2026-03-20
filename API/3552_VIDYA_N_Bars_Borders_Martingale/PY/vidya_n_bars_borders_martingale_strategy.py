import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vidya_n_bars_borders_martingale_strategy(Strategy):
    def __init__(self):
        super(vidya_n_bars_borders_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Trading candle type", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("Candle Type", "Trading candle type", "General")
        self._range_period = self.Param("RangePeriod", 10) \
            .SetDisplay("Candle Type", "Trading candle type", "General")
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 1.25) \
            .SetDisplay("Candle Type", "Trading candle type", "General")

        self._ema = None
        self._high_history = new()
        self._low_history = new()
        self._current_volume = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vidya_n_bars_borders_martingale_strategy, self).OnReseted()
        self._ema = None
        self._high_history = new()
        self._low_history = new()
        self._current_volume = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(vidya_n_bars_borders_martingale_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return vidya_n_bars_borders_martingale_strategy()
