import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class macd_sample_classic_strategy(Strategy):
    def __init__(self):
        super(macd_sample_classic_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._trend_ma_period = self.Param("TrendMaPeriod", 26) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._macd_open_level = self.Param("MacdOpenLevel", 0) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._macd_close_level = self.Param("MacdCloseLevel", 0) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 30) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._minimum_history_candles = self.Param("MinimumHistoryCandles", 30) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")

        self._point_size = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._trend_ma_current = None
        self._trend_ma_previous = None
        self._finished_candles = 0.0
        self._last_processed_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_sample_classic_strategy, self).OnReseted()
        self._point_size = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._trend_ma_current = None
        self._trend_ma_previous = None
        self._finished_candles = 0.0
        self._last_processed_time = None

    def OnStarted(self, time):
        super(macd_sample_classic_strategy, self).OnStarted(time)

        self._trend_ma = ExponentialMovingAverage()
        self._trend_ma.Length = self.trend_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_sample_classic_strategy()
