import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class bull_vs_medved_window_strategy(Strategy):
    def __init__(self):
        super(bull_vs_medved_window_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume for market orders", "Trading")
        self._candle_size_points = self.Param("CandleSizePoints", 75.0) \
            .SetDisplay("Body Size (points)", "Minimum body size for the latest candle", "Filters")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 0.8) \
            .SetDisplay("Stop Multiplier", "Coefficient applied to the candle body for stop-loss", "Risk")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 0.8) \
            .SetDisplay("Take Profit Multiplier", "Coefficient applied to the candle body for take-profit", "Risk")
        self._entry_window_minutes = self.Param("EntryWindowMinutes", 5) \
            .SetDisplay("Entry Window", "Duration of each trading window in minutes", "Timing")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for pattern detection", "Data")
        self._start_time0_hours = self.Param("StartTime0Hours", 0) \
            .SetDisplay("Window 1 Hour", "First window start hour", "Timing")
        self._start_time0_minutes = self.Param("StartTime0Minutes", 5) \
            .SetDisplay("Window 1 Minute", "First window start minute", "Timing")
        self._start_time1_hours = self.Param("StartTime1Hours", 4) \
            .SetDisplay("Window 2 Hour", "Second window start hour", "Timing")
        self._start_time1_minutes = self.Param("StartTime1Minutes", 5) \
            .SetDisplay("Window 2 Minute", "Second window start minute", "Timing")
        self._start_time2_hours = self.Param("StartTime2Hours", 8) \
            .SetDisplay("Window 3 Hour", "Third window start hour", "Timing")
        self._start_time2_minutes = self.Param("StartTime2Minutes", 5) \
            .SetDisplay("Window 3 Minute", "Third window start minute", "Timing")
        self._start_time3_hours = self.Param("StartTime3Hours", 12) \
            .SetDisplay("Window 4 Hour", "Fourth window start hour", "Timing")
        self._start_time3_minutes = self.Param("StartTime3Minutes", 5) \
            .SetDisplay("Window 4 Minute", "Fourth window start minute", "Timing")
        self._start_time4_hours = self.Param("StartTime4Hours", 16) \
            .SetDisplay("Window 5 Hour", "Fifth window start hour", "Timing")
        self._start_time4_minutes = self.Param("StartTime4Minutes", 5) \
            .SetDisplay("Window 5 Minute", "Fifth window start minute", "Timing")
        self._start_time5_hours = self.Param("StartTime5Hours", 20) \
            .SetDisplay("Window 6 Hour", "Sixth window start hour", "Timing")
        self._start_time5_minutes = self.Param("StartTime5Minutes", 5) \
            .SetDisplay("Window 6 Minute", "Sixth window start minute", "Timing")

        self._point_value = 0.0
        self._candle_size_threshold = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._prev_candle1 = None
        self._prev_candle2 = None
        self._order_placed_in_window = False
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def CandleSizePoints(self):
        return self._candle_size_points.Value

    @property
    def StopLossMultiplier(self):
        return self._stop_loss_multiplier.Value

    @property
    def TakeProfitMultiplier(self):
        return self._take_profit_multiplier.Value

    @property
    def EntryWindowMinutes(self):
        return self._entry_window_minutes.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(bull_vs_medved_window_strategy, self).OnStarted(time)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._point_value = float(ps) if ps is not None else 1.0
        if self._point_value <= 0:
            self._point_value = 1.0

        self._candle_size_threshold = float(self.CandleSizePoints) * self._point_value
        self._body_min_size = 10.0 * self._point_value
        self._pullback_size = 20.0 * self._point_value
        self._entry_window_sec = self.EntryWindowMinutes * 60

        self._entry_times = []
        for h_param, m_param in [
            (self._start_time0_hours, self._start_time0_minutes),
            (self._start_time1_hours, self._start_time1_minutes),
            (self._start_time2_hours, self._start_time2_minutes),
            (self._start_time3_hours, self._start_time3_minutes),
            (self._start_time4_hours, self._start_time4_minutes),
            (self._start_time5_hours, self._start_time5_minutes),
        ]:
            self._entry_times.append(h_param.Value * 60 + m_param.Value)

        self.Volume = float(self.OrderVolume)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._shift_history(candle)
            return

        if self._handle_position_exits(candle):
            self._shift_history(candle)
            return

        in_window = self._is_within_entry_window(candle.CloseTime)
        if not in_window:
            self._order_placed_in_window = False
            self._shift_history(candle)
            return

        if self._order_placed_in_window or self.Position != 0:
            self._shift_history(candle)
            return

        if self._prev_candle1 is None or self._prev_candle2 is None:
            self._shift_history(candle)
            return

        shift1 = candle
        shift2 = self._prev_candle1
        shift3 = self._prev_candle2

        placed = False
        is_bull = self._check_bull(shift3, shift2, shift1)
        is_bad_bull = self._check_bad_bull(shift3, shift2, shift1)
        is_cool_bull = self._check_cool_bull(shift2, shift1)
        is_bear = self._check_bear(shift1)

        close = float(shift1.ClosePrice)
        open_p = float(shift1.OpenPrice)
        body = abs(close - open_p)
        stop_dist = body * float(self.StopLossMultiplier)
        take_dist = body * float(self.TakeProfitMultiplier)
        ov = float(self.OrderVolume)

        if is_bull and not is_bad_bull:
            if ov > 0:
                self.BuyMarket(ov)
                self._entry_price = close
                self._long_stop = close - stop_dist if stop_dist > 0 else None
                self._long_take = close + take_dist if take_dist > 0 else None
                self._short_stop = None
                self._short_take = None
                placed = True
        elif is_cool_bull:
            if ov > 0:
                self.BuyMarket(ov)
                self._entry_price = close
                self._long_stop = close - stop_dist if stop_dist > 0 else None
                self._long_take = close + take_dist if take_dist > 0 else None
                self._short_stop = None
                self._short_take = None
                placed = True
        elif is_bear:
            if ov > 0:
                self.SellMarket(ov)
                self._entry_price = close
                self._short_stop = close + stop_dist if stop_dist > 0 else None
                self._short_take = close - take_dist if take_dist > 0 else None
                self._long_stop = None
                self._long_take = None
                placed = True

        if placed:
            self._order_placed_in_window = True

        self._shift_history(candle)

    def _handle_position_exits(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket(self.Position)
                self._reset_protection()
                return True
            if self._long_take is not None and high >= self._long_take:
                self.SellMarket(self.Position)
                self._reset_protection()
                return True
        elif self.Position < 0:
            vol = Math.Abs(self.Position)
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket(vol)
                self._reset_protection()
                return True
            if self._short_take is not None and low <= self._short_take:
                self.BuyMarket(vol)
                self._reset_protection()
                return True
        return False

    def _is_within_entry_window(self, time):
        if self._entry_window_sec <= 0:
            return False
        tod_minutes = time.Hour * 60 + time.Minute
        for start_min in self._entry_times:
            end_min = start_min + self.EntryWindowMinutes
            if tod_minutes >= start_min and tod_minutes <= end_min:
                return True
        return False

    def _check_bull(self, s3, s2, s1):
        return (float(s3.ClosePrice) > float(s2.OpenPrice) and
                (float(s2.ClosePrice) - float(s2.OpenPrice)) >= self._body_min_size and
                (float(s1.ClosePrice) - float(s1.OpenPrice)) >= self._candle_size_threshold)

    def _check_bad_bull(self, s3, s2, s1):
        return ((float(s3.ClosePrice) - float(s3.OpenPrice)) >= self._body_min_size and
                (float(s2.ClosePrice) - float(s2.OpenPrice)) >= self._body_min_size and
                (float(s1.ClosePrice) - float(s1.OpenPrice)) >= self._candle_size_threshold)

    def _check_cool_bull(self, s2, s1):
        return ((float(s2.OpenPrice) - float(s2.ClosePrice)) >= self._pullback_size and
                float(s2.ClosePrice) <= float(s1.OpenPrice) and
                float(s1.ClosePrice) > float(s2.OpenPrice) and
                (float(s1.ClosePrice) - float(s1.OpenPrice)) >= 0.4 * self._candle_size_threshold)

    def _check_bear(self, s1):
        return (float(s1.OpenPrice) - float(s1.ClosePrice)) >= self._candle_size_threshold

    def _shift_history(self, candle):
        self._prev_candle2 = self._prev_candle1
        self._prev_candle1 = candle

    def _reset_protection(self):
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = None

    def OnReseted(self):
        super(bull_vs_medved_window_strategy, self).OnReseted()
        self._point_value = 0.0
        self._candle_size_threshold = 0.0
        self._body_min_size = 0.0
        self._pullback_size = 0.0
        self._prev_candle1 = None
        self._prev_candle2 = None
        self._order_placed_in_window = False
        self._reset_protection()

    def CreateClone(self):
        return bull_vs_medved_window_strategy()
