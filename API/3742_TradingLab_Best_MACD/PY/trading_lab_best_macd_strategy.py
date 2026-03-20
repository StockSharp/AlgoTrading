import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, Highest, Lowest, MovingAverageConvergenceDivergenceSignal
)
from StockSharp.Algo.Strategies import Strategy


class trading_lab_best_macd_strategy(Strategy):
    """MACD + SMA trend + Highest/Lowest box with signal validity counters and virtual SL/TP."""

    def __init__(self):
        super(trading_lab_best_macd_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Fixed volume sent with each market order", "Risk")
        self._signal_validity = self.Param("SignalValidity", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Validity", "Number of candles a MACD or box trigger remains active", "Filters")
        self._ma_length = self.Param("MaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Simple moving average period used as the trend filter", "Filters")
        self._box_period = self.Param("BoxPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Box Period", "Lookback length for the support/resistance box", "Filters")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast Length", "Fast EMA length for MACD", "Indicators")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow Length", "Slow EMA length for MACD", "Indicators")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal Length", "Signal line length for MACD", "Indicators")
        self._stop_distance_points = self.Param("StopDistancePoints", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Distance (points)", "Protective stop distance from the moving average", "Risk")
        self._risk_reward_multiplier = self.Param("RiskRewardMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk-Reward Multiplier", "Multiplier applied to derive the take-profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data type used to subscribe for candles", "General")

        self._sma = None
        self._highest = None
        self._lowest = None
        self._macd = None

        self._resistance_counter = 0
        self._support_counter = 0
        self._macd_down_counter = 0
        self._macd_up_counter = 0

        self._prev_macd_main = None
        self._prev_macd_signal = None

        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None

        self._active_stop = None
        self._active_take = None
        self._active_side = None

        self._previous_high = None
        self._previous_low = None
        self._has_previous_candle = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def SignalValidity(self):
        return self._signal_validity.Value

    @property
    def MaLength(self):
        return self._ma_length.Value

    @property
    def BoxPeriod(self):
        return self._box_period.Value

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @property
    def MacdSignalLength(self):
        return self._macd_signal_length.Value

    @property
    def StopDistancePoints(self):
        return self._stop_distance_points.Value

    @property
    def RiskRewardMultiplier(self):
        return self._risk_reward_multiplier.Value

    def OnReseted(self):
        super(trading_lab_best_macd_strategy, self).OnReseted()
        self._sma = None
        self._highest = None
        self._lowest = None
        self._macd = None
        self._resistance_counter = 0
        self._support_counter = 0
        self._macd_down_counter = 0
        self._macd_up_counter = 0
        self._prev_macd_main = None
        self._prev_macd_signal = None
        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None
        self._active_stop = None
        self._active_take = None
        self._active_side = None
        self._previous_high = None
        self._previous_low = None
        self._has_previous_candle = False

    def OnStarted(self, time):
        super(trading_lab_best_macd_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaLength

        self._highest = Highest()
        self._highest.Length = self.BoxPeriod

        self._lowest = Lowest()
        self._lowest.Length = self.BoxPeriod

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFastLength
        self._macd.Macd.LongMa.Length = self.MacdSlowLength
        self._macd.SignalMa.Length = self.MacdSignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._sma, self._highest, self._lowest, self._macd, self._process_candle).Start()

    def _process_candle(self, candle, sma_val, highest_val, lowest_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        self._check_protective_levels(candle)

        sma_value = float(sma_val.ToDecimal())
        resistance_value = float(highest_val.ToDecimal())
        support_value = float(lowest_val.ToDecimal())

        macd_main_raw = macd_val.Macd
        macd_signal_raw = macd_val.Signal

        if macd_main_raw is None or macd_signal_raw is None:
            self._update_previous_candle(candle, None, None)
            return

        macd_main = float(macd_main_raw)
        macd_signal = float(macd_signal_raw)

        if not self._sma.IsFormed or not self._highest.IsFormed or \
                not self._lowest.IsFormed or not self._macd.IsFormed:
            self._update_previous_candle(candle, macd_main, macd_signal)
            return

        if self._resistance_counter > 0:
            self._resistance_counter -= 1
        if self._support_counter > 0:
            self._support_counter -= 1
        if self._macd_down_counter > 0:
            self._macd_down_counter -= 1
        if self._macd_up_counter > 0:
            self._macd_up_counter -= 1

        point = self._get_point_value()

        if self._has_previous_candle:
            if self._previous_high is not None and resistance_value > 0 and self._previous_high > resistance_value:
                self._resistance_counter = self.SignalValidity
            if self._previous_low is not None and support_value > 0 and self._previous_low < support_value:
                self._support_counter = self.SignalValidity

        if self._prev_macd_main is not None and self._prev_macd_signal is not None:
            if macd_main < macd_signal and self._prev_macd_main > self._prev_macd_signal and macd_main > 0:
                self._macd_down_counter = self.SignalValidity
            if macd_main > macd_signal and self._prev_macd_main < self._prev_macd_signal and macd_main < 0:
                self._macd_up_counter = self.SignalValidity

        volume = float(self.OrderVolume)
        close = float(candle.ClosePrice)

        if volume > 0:
            long_signal_active = self._macd_up_counter > 0 and close > sma_value
            if long_signal_active and self.Position <= 0:
                stop_offset = float(self.StopDistancePoints) * point
                adjusted_distance = close - sma_value + stop_offset
                if adjusted_distance > 0:
                    self._planned_stop = sma_value - stop_offset
                    self._planned_take = close + adjusted_distance * float(self.RiskRewardMultiplier)
                    self._planned_side = 1
                    self.BuyMarket(volume)
                    self._activate_planned_levels(1)

            short_signal_active = self._macd_down_counter > 0 and close < sma_value
            if short_signal_active and self.Position >= 0:
                stop_offset = float(self.StopDistancePoints) * point
                adjusted_distance = sma_value - close + stop_offset
                if adjusted_distance > 0:
                    self._planned_stop = sma_value + stop_offset
                    self._planned_take = close - adjusted_distance * float(self.RiskRewardMultiplier)
                    self._planned_side = -1
                    self.SellMarket(volume)
                    self._activate_planned_levels(-1)

        self._update_previous_candle(candle, macd_main, macd_signal)

    def _activate_planned_levels(self, direction):
        if direction == 1 and self._planned_side == 1:
            self._active_stop = self._planned_stop
            self._active_take = self._planned_take
            self._active_side = 1
        elif direction == -1 and self._planned_side == -1:
            self._active_stop = self._planned_stop
            self._active_take = self._planned_take
            self._active_side = -1
        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None

    def _check_protective_levels(self, candle):
        if self._active_side is None or self.Position == 0:
            return

        if self._active_side == 1 and self.Position > 0:
            if self._active_stop is not None and float(candle.LowPrice) <= self._active_stop:
                self._clear_planned_levels()
                self._clear_active_levels()
                self._close_position()
                return
            if self._active_take is not None and float(candle.HighPrice) >= self._active_take:
                self._clear_planned_levels()
                self._clear_active_levels()
                self._close_position()
                return
        elif self._active_side == -1 and self.Position < 0:
            if self._active_stop is not None and float(candle.HighPrice) >= self._active_stop:
                self._clear_planned_levels()
                self._clear_active_levels()
                self._close_position()
                return
            if self._active_take is not None and float(candle.LowPrice) <= self._active_take:
                self._clear_planned_levels()
                self._clear_active_levels()
                self._close_position()

    def _update_previous_candle(self, candle, macd_main, macd_signal):
        self._previous_high = float(candle.HighPrice)
        self._previous_low = float(candle.LowPrice)
        self._has_previous_candle = True
        self._prev_macd_main = macd_main
        self._prev_macd_signal = macd_signal

    def _get_point_value(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0:
                return step
        return 0.0001

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))

    def _clear_planned_levels(self):
        self._planned_stop = None
        self._planned_take = None
        self._planned_side = None

    def _clear_active_levels(self):
        self._active_stop = None
        self._active_take = None
        self._active_side = None

    def CreateClone(self):
        return trading_lab_best_macd_strategy()
