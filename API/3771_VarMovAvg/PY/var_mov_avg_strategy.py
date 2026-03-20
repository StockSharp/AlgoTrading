import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class var_mov_avg_strategy(Strategy):
    def __init__(self):
        super(var_mov_avg_strategy, self).__init__()

        self._ama_period = self.Param("AmaPeriod", 20) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._smoothing_power = self.Param("SmoothingPower", 1) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._signal_pips_bar_a = self.Param("SignalPipsBarA", 1) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._signal_pips_bar_b = self.Param("SignalPipsBarB", 1) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._signal_pips_trade = self.Param("SignalPipsTrade", 10) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._entry_pips_diff = self.Param("EntryPipsDiff", 500) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._stop_pips_diff = self.Param("StopPipsDiff", 34) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._stop_ma_period = self.Param("StopMaPeriod", 52) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._stop_ma_shift = self.Param("StopMaShift", 0) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._stop_ma_method = self.Param("StopMaMethod", MovingAverageMethods.Exponential) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("VMA Length", "Adaptive moving average period", "Indicators")

        self._vma = None
        self._stop_low_ma = None
        self._stop_high_ma = None
        self._low_ma_values = None
        self._high_ma_values = None
        self._long_signal = None
        self._short_signal = None
        self._is_long = False
        self._state = SignalStates.Neutral
        self._bar_a_reference = 0.0
        self._entry_price = 0.0
        self._closes = new()
        self._previous_ama = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(var_mov_avg_strategy, self).OnReseted()
        self._vma = None
        self._stop_low_ma = None
        self._stop_high_ma = None
        self._low_ma_values = None
        self._high_ma_values = None
        self._long_signal = None
        self._short_signal = None
        self._is_long = False
        self._state = SignalStates.Neutral
        self._bar_a_reference = 0.0
        self._entry_price = 0.0
        self._closes = new()
        self._previous_ama = None

    def OnStarted(self, time):
        super(var_mov_avg_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__vma = VariableMovingAverage()
        self.__vma.Length = self.ama_period
        self.__vma.FastPeriod = self.fast_period
        self.__vma.SlowPeriod = self.slow_period
        self.__vma.SmoothingPower = self.smoothing_power

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return var_mov_avg_strategy()
