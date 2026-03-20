import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy


class ma_grid_strategy(Strategy):
    def __init__(self):
        super(ma_grid_strategy, self).__init__()

        self._volume_tolerance = self.Param("VolumeTolerance", 0.0000001) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")
        self._ma_period = self.Param("MaPeriod", 48) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")
        self._grid_amount = self.Param("GridAmount", 6) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")
        self._distance = self.Param("Distance", 0.005) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk")

        self._order_intents = new()
        self._ema = None
        self._effective_grid_amount = 0.0
        self._current_grid = 0.0
        self._next_grid_price = 0.0
        self._last_grid_price = 0.0
        self._is_grid_initialized = False
        self._long_exposure = 0.0
        self._short_exposure = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_grid_strategy, self).OnReseted()
        self._order_intents = new()
        self._ema = None
        self._effective_grid_amount = 0.0
        self._current_grid = 0.0
        self._next_grid_price = 0.0
        self._last_grid_price = 0.0
        self._is_grid_initialized = False
        self._long_exposure = 0.0
        self._short_exposure = 0.0

    def OnStarted(self, time):
        super(ma_grid_strategy, self).OnStarted(time)

        self.__ema = EMA()
        self.__ema.Length = self.ma_period

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
        return ma_grid_strategy()
