import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage as EMA, JurikMovingAverage, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class exp_ultra_fatl_duplex_strategy(Strategy):
    def __init__(self):
        super(exp_ultra_fatl_duplex_strategy, self).__init__()

        self._long_volume = self.Param("LongVolume", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._allow_long_entries = self.Param("AllowLongEntries", True) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._allow_long_exits = self.Param("AllowLongExits", True) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_candle_type = self.Param("LongCandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_applied_price = self.Param("LongAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_trend_method = self.Param("LongTrendMethod", SmoothMethods.Ema) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_start_length = self.Param("LongStartLength", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_phase = self.Param("LongPhase", 100) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_step = self.Param("LongStep", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_steps_total = self.Param("LongStepsTotal", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_smooth_method = self.Param("LongSmoothMethod", SmoothMethods.Ema) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_smooth_length = self.Param("LongSmoothLength", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_smooth_phase = self.Param("LongSmoothPhase", 100) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_stop_loss_points = self.Param("LongStopLossPoints", 1000) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._long_take_profit_points = self.Param("LongTakeProfitPoints", 2000) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_volume = self.Param("ShortVolume", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._allow_short_entries = self.Param("AllowShortEntries", True) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._allow_short_exits = self.Param("AllowShortExits", True) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_candle_type = self.Param("ShortCandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_applied_price = self.Param("ShortAppliedPrice", AppliedPrices.Close) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_trend_method = self.Param("ShortTrendMethod", SmoothMethods.Ema) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_start_length = self.Param("ShortStartLength", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_phase = self.Param("ShortPhase", 100) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_step = self.Param("ShortStep", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_steps_total = self.Param("ShortStepsTotal", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_smooth_method = self.Param("ShortSmoothMethod", SmoothMethods.Ema) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_smooth_length = self.Param("ShortSmoothLength", 3) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_smooth_phase = self.Param("ShortSmoothPhase", 100) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_stop_loss_points = self.Param("ShortStopLossPoints", 1000) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")
        self._short_take_profit_points = self.Param("ShortTakeProfitPoints", 2000) \
            .SetDisplay("Long Volume", "Order volume for long entries.", "Long")

        self._long_context = None
        self._short_context = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._price_step = 0.0
        self._price_chart_initialized = False
        self._strategy = None
        self._is_long = False
        self._candle_type = None
        self._applied_price = None
        self._trend_method = None
        self._start_length = 0.0
        self._phase = 0.0
        self._step = 0.0
        self._steps_total = 0.0
        self._smooth_method = None
        self._smooth_length = 0.0
        self._smooth_phase = 0.0
        self._signal_bar = 0.0
        self._volume = 0.0
        self._allow_entries = False
        self._allow_exits = False
        self._stop_loss_points = 0.0
        self._take_profit_points = 0.0
        self._price_step = 0.0
        self._ladder = new()
        self._previous_values = new()
        self._bulls_smoother = None
        self._bears_smoother = None
        self._history = new()
        self._fatl = new()
        self._subscription = None
        self._filled = 0.0

    def OnReseted(self):
        super(exp_ultra_fatl_duplex_strategy, self).OnReseted()
        self._long_context = None
        self._short_context = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._price_step = 0.0
        self._price_chart_initialized = False
        self._strategy = None
        self._is_long = False
        self._candle_type = None
        self._applied_price = None
        self._trend_method = None
        self._start_length = 0.0
        self._phase = 0.0
        self._step = 0.0
        self._steps_total = 0.0
        self._smooth_method = None
        self._smooth_length = 0.0
        self._smooth_phase = 0.0
        self._signal_bar = 0.0
        self._volume = 0.0
        self._allow_entries = False
        self._allow_exits = False
        self._stop_loss_points = 0.0
        self._take_profit_points = 0.0
        self._price_step = 0.0
        self._ladder = new()
        self._previous_values = new()
        self._bulls_smoother = None
        self._bears_smoother = None
        self._history = new()
        self._fatl = new()
        self._subscription = None
        self._filled = 0.0

    def OnStarted(self, time):
        super(exp_ultra_fatl_duplex_strategy, self).OnStarted(time)


    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return exp_ultra_fatl_duplex_strategy()
