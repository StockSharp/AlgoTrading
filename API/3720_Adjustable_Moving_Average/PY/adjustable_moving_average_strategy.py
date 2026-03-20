import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adjustable_moving_average_strategy(Strategy):
    def __init__(self):
        super(adjustable_moving_average_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._ma_method = self.Param("MaMethod", MovingAverageMethods.Exponential) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._min_gap_points = self.Param("MinGapPoints", 3) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 0) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._trailing_points = self.Param("TrailStopPoints", 0) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._entry_mode = self.Param("Mode", EntryModes.Both) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._session_start = self.Param("SessionStart", TimeSpan.Zero) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._session_end = self.Param("SessionEnd", new TimeSpan(23, 59, 0) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._close_outside_session = self.Param("CloseOutsideSession", True) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._trail_outside_session = self.Param("TrailOutsideSession", True) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._fixed_lot = self.Param("FixedLot", 0.1) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._enable_auto_lot = self.Param("EnableAutoLot", False) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._lot_per10k = self.Param("LotPer10kFreeMargin", 1) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._max_slippage = self.Param("MaxSlippage", 3) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._trade_comment = self.Param("TradeComment", "AdjustableMovingAverageEA") \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")

        self._fast_ma = None
        self._slow_ma = None
        self._point_value = 0.0
        self._min_gap_threshold = 0.0
        self._previous_signal = 0.0
        self._has_initial_signal = False
        self._long_trailing_stop = None
        self._short_trailing_stop = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adjustable_moving_average_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._point_value = 0.0
        self._min_gap_threshold = 0.0
        self._previous_signal = 0.0
        self._has_initial_signal = False
        self._long_trailing_stop = None
        self._short_trailing_stop = None

    def OnStarted(self, time):
        super(adjustable_moving_average_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_fastMa, _slowMa, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return adjustable_moving_average_strategy()
