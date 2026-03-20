import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ZigZag
from StockSharp.Algo.Strategies import Strategy


class tuyul_uncensored_strategy(Strategy):
    def __init__(self):
        super(tuyul_uncensored_strategy, self).__init__()

        self._volume = self.Param("VolumePerTrade", 0.03) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 1.2) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._zig_zag_depth = self.Param("ZigZagDepth", 12) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._zig_zag_deviation = self.Param("ZigZagDeviation", 0.05) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._zig_zag_backstep = self.Param("ZigZagBackstep", 3) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._wait_bars = self.Param("WaitBarsAfterSignal", 12) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._fast_ema_period = self.Param("FastEmaPeriod", 9) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._allow_monday = self.Param("AllowMonday", True) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._allow_tuesday = self.Param("AllowTuesday", True) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._allow_wednesday = self.Param("AllowWednesday", True) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._allow_thursday = self.Param("AllowThursday", True) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._allow_friday = self.Param("AllowFriday", True) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._fib_level = self.Param("FibLevel", 0.57) \
            .SetDisplay("Volume", "Order volume per trade", "General")

        self._pivots = None
        self._zig_zag = None
        self._fast_ema = None
        self._slow_ema = None
        self._last_zig_zag_high = 0.0
        self._last_zig_zag_low = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tuyul_uncensored_strategy, self).OnReseted()
        self._pivots = None
        self._zig_zag = None
        self._fast_ema = None
        self._slow_ema = None
        self._last_zig_zag_high = 0.0
        self._last_zig_zag_low = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0.0

    def OnStarted(self, time):
        super(tuyul_uncensored_strategy, self).OnStarted(time)

        self.__zig_zag = ZigZag()
        self.__zig_zag.Deviation = self.zig_zag_deviation
        self.__fast_ema = ExponentialMovingAverage()
        self.__fast_ema.Length = self.fast_ema_period
        self.__slow_ema = ExponentialMovingAverage()
        self.__slow_ema.Length = self.slow_ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__zig_zag, self.__fast_ema, self.__slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return tuyul_uncensored_strategy()
