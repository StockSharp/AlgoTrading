import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class bss_triple_ema_separation_strategy(Strategy):
    def __init__(self):
        super(bss_triple_ema_separation_strategy, self).__init__()

        self._volume_tolerance = self.Param("VolumeTolerance", 1e-8)
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._max_positions = self.Param("MaxPositions", 2)
        self._minimum_distance = self.Param("MinimumDistance", 50.0)
        self._minimum_pause_seconds = self.Param("MinimumPauseSeconds", 600)
        self._first_ma_period = self.Param("FirstMaPeriod", 5)
        self._second_ma_period = self.Param("SecondMaPeriod", 25)
        self._third_ma_period = self.Param("ThirdMaPeriod", 125)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._first_ma = None
        self._second_ma = None
        self._third_ma = None
        self._last_entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bss_triple_ema_separation_strategy, self).OnStarted2(time)

        self._first_ma = ExponentialMovingAverage()
        self._first_ma.Length = self._first_ma_period.Value
        self._second_ma = ExponentialMovingAverage()
        self._second_ma.Length = self._second_ma_period.Value
        self._third_ma = ExponentialMovingAverage()
        self._third_ma.Length = self._third_ma_period.Value

        self._last_entry_time = None

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._first_ma, self._second_ma, self._third_ma, self._process_candle).Start()

    def _process_candle(self, candle, first_value, second_value, third_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._first_ma.IsFormed or not self._second_ma.IsFormed or not self._third_ma.IsFormed:
            return

        fv = float(first_value)
        sv = float(second_value)
        tv = float(third_value)
        min_distance = self._minimum_distance.Value

        long_spread_ok = tv - sv >= min_distance and sv - fv >= min_distance
        short_spread_ok = fv - sv >= min_distance and sv - tv >= min_distance

        if not long_spread_ok and not short_spread_ok:
            return

        time = candle.OpenTime

        if long_spread_ok:
            if self._try_close_opposite_positions(True):
                return
            if self._can_enter_position(time, True):
                self.BuyMarket(self._order_volume.Value)
                self._last_entry_time = time
            return

        if short_spread_ok:
            if self._try_close_opposite_positions(False):
                return
            if self._can_enter_position(time, False):
                self.SellMarket(self._order_volume.Value)
                self._last_entry_time = time

    def _can_enter_position(self, time, is_long):
        if not self._is_pause_elapsed(time):
            return False

        vol = self._order_volume.Value
        target_position = self.Position + (vol if is_long else -vol)
        max_exposure = self._max_positions.Value * vol

        return abs(target_position) <= max_exposure + self._volume_tolerance.Value

    def _is_pause_elapsed(self, time):
        pause_seconds = self._minimum_pause_seconds.Value
        if pause_seconds <= 0:
            return True
        if self._last_entry_time is None:
            return True
        return (time - self._last_entry_time) >= TimeSpan.FromSeconds(pause_seconds)

    def _try_close_opposite_positions(self, is_long):
        tol = self._volume_tolerance.Value
        if is_long:
            if self.Position < -tol:
                self.BuyMarket(abs(self.Position))
                return True
        else:
            if self.Position > tol:
                self.SellMarket(self.Position)
                return True
        return False

    def OnReseted(self):
        super(bss_triple_ema_separation_strategy, self).OnReseted()
        self._first_ma = None
        self._second_ma = None
        self._third_ma = None
        self._last_entry_time = None

    def CreateClone(self):
        return bss_triple_ema_separation_strategy()
