import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bss_triple_ema_separation_strategy(Strategy):
    def __init__(self):
        super(bss_triple_ema_separation_strategy, self).__init__()

        self._volume_tolerance = self.Param("VolumeTolerance", 1e-8) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._max_positions = self.Param("MaxPositions", 2) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._minimum_distance = self.Param("MinimumDistance", 50) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._minimum_pause_seconds = self.Param("MinimumPauseSeconds", 600) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._first_ma_period = self.Param("FirstMaPeriod", 5) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._first_ma_method = self.Param("FirstMaMethod", MaMethods.Exponential) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._second_ma_period = self.Param("SecondMaPeriod", 25) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._second_ma_method = self.Param("SecondMaMethod", MaMethods.Exponential) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._third_ma_period = self.Param("ThirdMaPeriod", 125) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._third_ma_method = self.Param("ThirdMaMethod", MaMethods.Exponential) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Volume Tolerance", "Tolerance when comparing volume values", "Risk")

        self._first_ma = null!
        self._second_ma = null!
        self._third_ma = null!
        self._last_entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bss_triple_ema_separation_strategy, self).OnReseted()
        self._first_ma = null!
        self._second_ma = null!
        self._third_ma = null!
        self._last_entry_time = None

    def OnStarted(self, time):
        super(bss_triple_ema_separation_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_firstMa, _secondMa, _thirdMa, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bss_triple_ema_separation_strategy()
