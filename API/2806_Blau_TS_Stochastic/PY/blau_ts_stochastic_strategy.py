import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, Highest, Lowest, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class blau_ts_stochastic_strategy(Strategy):
    def __init__(self):
        super(blau_ts_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._mode = self.Param("Mode", BlauSignalModes.Twist) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._applied_price = self.Param("AppliedPrice", AppliedPriceTypes.Close) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._smoothing = self.Param("Smoothing", BlauSmoothingTypes.Exponential) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._base_length = self.Param("BaseLength", 5) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._smooth_length1 = self.Param("SmoothLength1", 10) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._smooth_length2 = self.Param("SmoothLength2", 5) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._smooth_length3 = self.Param("SmoothLength3", 3) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._signal_length = self.Param("SignalLength", 3) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Candle Type", "Time frame for signal calculations", "General")

        self._highest = null!
        self._lowest = null!
        self._stoch_smooth1 = null!
        self._stoch_smooth2 = null!
        self._stoch_smooth3 = null!
        self._range_smooth1 = null!
        self._range_smooth2 = null!
        self._range_smooth3 = null!
        self._signal_smooth = null!
        self._hist_history = new()
        self._signal_history = new()
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(blau_ts_stochastic_strategy, self).OnReseted()
        self._highest = null!
        self._lowest = null!
        self._stoch_smooth1 = null!
        self._stoch_smooth2 = null!
        self._stoch_smooth3 = null!
        self._range_smooth1 = null!
        self._range_smooth2 = null!
        self._range_smooth3 = null!
        self._signal_smooth = null!
        self._hist_history = new()
        self._signal_history = new()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(blau_ts_stochastic_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.base_length
        self.__lowest = Lowest()
        self.__lowest.Length = self.base_length

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
        return blau_ts_stochastic_strategy()
