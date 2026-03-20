import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_sample_classic_strategy(Strategy):
    """Classic MACD strategy replicating the original MetaTrader MACD Sample expert advisor.
    Uses MACD crossover with EMA trend filter, StartProtection for TP/trailing."""

    def __init__(self):
        super(macd_sample_classic_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal EMA", "Signal EMA period for MACD", "Indicators")
        self._trend_ma_period = self.Param("TrendMaPeriod", 26) \
            .SetDisplay("Trend EMA", "EMA period used for directional filter", "Indicators")
        self._macd_open_level = self.Param("MacdOpenLevel", 0.0) \
            .SetDisplay("MACD Open", "Entry threshold in MACD points", "Signals")
        self._macd_close_level = self.Param("MacdCloseLevel", 0.0) \
            .SetDisplay("MACD Close", "Exit threshold in MACD points", "Signals")
        self._take_profit_points = self.Param("TakeProfitPoints", 50.0) \
            .SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 30.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in price points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._minimum_history_candles = self.Param("MinimumHistoryCandles", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Warm-up candles", "Number of finished candles required before trading starts", "General")

        self._point_size = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._trend_ma_current = None
        self._trend_ma_previous = None
        self._finished_candles = 0
        self._last_processed_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def TrendMaPeriod(self):
        return self._trend_ma_period.Value

    @property
    def MacdOpenLevel(self):
        return self._macd_open_level.Value

    @property
    def MacdCloseLevel(self):
        return self._macd_close_level.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def MinimumHistoryCandles(self):
        return self._minimum_history_candles.Value

    def OnReseted(self):
        super(macd_sample_classic_strategy, self).OnReseted()
        self._point_size = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._trend_ma_current = None
        self._trend_ma_previous = None
        self._finished_candles = 0
        self._last_processed_time = None

    def OnStarted(self, time):
        super(macd_sample_classic_strategy, self).OnStarted(time)

        self._point_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._point_size = ps

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastEmaPeriod
        macd.Macd.LongMa.Length = self.SlowEmaPeriod
        macd.SignalMa.Length = self.SignalPeriod

        trend_ma = ExponentialMovingAverage()
        trend_ma.Length = self.TrendMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self._process_macd_values)
        subscription.Bind(trend_ma, self._process_trend_ma_value)
        subscription.Start()

        tp_distance = float(self.TakeProfitPoints) * self._point_size
        trailing_distance = float(self.TrailingStopPoints) * self._point_size

        if tp_distance > 0 or trailing_distance > 0:
            tp_unit = Unit(tp_distance, UnitTypes.Absolute) if tp_distance > 0 else None
            sl_unit = Unit(trailing_distance, UnitTypes.Absolute) if trailing_distance > 0 else None
            self.StartProtection(tp_unit, sl_unit, trailing_distance > 0)

    def _process_trend_ma_value(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        self._trend_ma_previous = self._trend_ma_current
        self._trend_ma_current = float(ma_value)

    def _process_macd_values(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if self._last_processed_time != candle.OpenTime:
            self._last_processed_time = candle.OpenTime
            self._finished_candles += 1

        if self._finished_candles < self.MinimumHistoryCandles:
            return

        macd_main_raw = macd_value.Macd
        signal_raw = macd_value.Signal

        if macd_main_raw is None or signal_raw is None:
            return

        macd_current = float(macd_main_raw)
        signal_current = float(signal_raw)

        if self._prev_macd is None or self._prev_signal is None or \
                self._trend_ma_current is None or self._trend_ma_previous is None:
            self._prev_macd = macd_current
            self._prev_signal = signal_current
            return

        macd_previous = self._prev_macd
        signal_previous = self._prev_signal
        trend_ma_current = self._trend_ma_current
        trend_ma_previous = self._trend_ma_previous

        macd_open_threshold = float(self.MacdOpenLevel) * self._point_size
        macd_close_threshold = float(self.MacdCloseLevel) * self._point_size

        is_trend_up = trend_ma_current > trend_ma_previous
        is_trend_down = trend_ma_current < trend_ma_previous

        buy_signal = macd_current < 0 and \
            macd_current > signal_current and \
            macd_previous < signal_previous and \
            abs(macd_current) > macd_open_threshold and \
            is_trend_up

        sell_signal = macd_current > 0 and \
            macd_current < signal_current and \
            macd_previous > signal_previous and \
            macd_current > macd_open_threshold and \
            is_trend_down

        exit_long_signal = macd_current > 0 and \
            macd_current < signal_current and \
            macd_previous > signal_previous and \
            macd_current > macd_close_threshold

        exit_short_signal = macd_current < 0 and \
            macd_current > signal_current and \
            macd_previous < signal_previous and \
            abs(macd_current) > macd_close_threshold

        if buy_signal and self.Position == 0:
            self.BuyMarket()
        elif sell_signal and self.Position == 0:
            self.SellMarket()
        elif exit_long_signal and self.Position > 0:
            self.SellMarket()
        elif exit_short_signal and self.Position < 0:
            self.BuyMarket()

        self._prev_macd = macd_current
        self._prev_signal = signal_current

    def CreateClone(self):
        return macd_sample_classic_strategy()
