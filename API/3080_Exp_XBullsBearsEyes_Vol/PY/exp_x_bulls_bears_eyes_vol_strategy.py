import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, ExponentialMovingAverage as EMA, JurikMovingAverage, SimpleMovingAverage as SMA, SmoothedMovingAverage, TripleExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class exp_x_bulls_bears_eyes_vol_strategy(Strategy):
    def __init__(self):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).__init__()

        self._primary_volume = self.Param("PrimaryVolume", 0.1) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._secondary_volume = self.Param("SecondaryVolume", 0.2) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._indicator_period = self.Param("IndicatorPeriod", 13) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._gamma = self.Param("Gamma", 0.6) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._volume_type = self.Param("VolumeType", AppliedVolumes.Tick) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._high_level2 = self.Param("HighLevel2", 25) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._high_level1 = self.Param("HighLevel1", 10) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._low_level1 = self.Param("LowLevel1", -10) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._low_level2 = self.Param("LowLevel2", -25) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._smooth_method = self.Param("SmoothingMethod", SmoothMethods.Sma) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._smooth_length = self.Param("SmoothingLength", 12) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._smooth_phase = self.Param("SmoothingPhase", 15) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")

        self._indicator = None
        self._color_history = new()
        self._last_long_primary_signal_time = None
        self._last_long_secondary_signal_time = None
        self._last_short_primary_signal_time = None
        self._last_short_secondary_signal_time = None
        self._is_long_primary_open = False
        self._is_long_secondary_open = False
        self._is_short_primary_open = False
        self._is_short_secondary_open = False
        self._ema = None
        self._value_smoother = None
        self._volume_smoother = None
        self._volume_type = None
        self._gamma = 0.0
        self._high_level2 = 0.0
        self._high_level1 = 0.0
        self._low_level1 = 0.0
        self._low_level2 = 0.0
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).OnReseted()
        self._indicator = None
        self._color_history = new()
        self._last_long_primary_signal_time = None
        self._last_long_secondary_signal_time = None
        self._last_short_primary_signal_time = None
        self._last_short_secondary_signal_time = None
        self._is_long_primary_open = False
        self._is_long_secondary_open = False
        self._is_short_primary_open = False
        self._is_short_secondary_open = False
        self._ema = None
        self._value_smoother = None
        self._volume_smoother = None
        self._volume_type = None
        self._gamma = 0.0
        self._high_level2 = 0.0
        self._high_level1 = 0.0
        self._low_level1 = 0.0
        self._low_level2 = 0.0
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0

    def OnStarted(self, time):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).OnStarted(time)


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
        return exp_x_bulls_bears_eyes_vol_strategy()
