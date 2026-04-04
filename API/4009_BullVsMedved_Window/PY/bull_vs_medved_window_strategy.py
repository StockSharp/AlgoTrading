import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Array, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class bull_vs_medved_window_strategy(Strategy):
    def __init__(self):
        super(bull_vs_medved_window_strategy, self).__init__()

        self._candle_size_points = self.Param("CandleSizePoints", Decimal(75)) \
            .SetDisplay("Body Size (points)", "Minimum body size for the latest candle", "Filters")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", Decimal(0.8)) \
            .SetDisplay("Stop Multiplier", "Coefficient applied to the candle body for stop-loss", "Risk")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", Decimal(0.8)) \
            .SetDisplay("Take Profit Multiplier", "Coefficient applied to the candle body for take-profit", "Risk")
        self._entry_window_minutes = self.Param("EntryWindowMinutes", 10) \
            .SetDisplay("Entry Window", "Duration of each trading window in minutes", "Timing")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for pattern detection", "Data")
        self._start_time0 = self.Param("StartTime0", TimeSpan(0, 5, 0)) \
            .SetDisplay("Start Time #1", "First trading window start", "Timing")
        self._start_time1 = self.Param("StartTime1", TimeSpan(4, 5, 0)) \
            .SetDisplay("Start Time #2", "Second trading window start", "Timing")
        self._start_time2 = self.Param("StartTime2", TimeSpan(8, 5, 0)) \
            .SetDisplay("Start Time #3", "Third trading window start", "Timing")
        self._start_time3 = self.Param("StartTime3", TimeSpan(12, 5, 0)) \
            .SetDisplay("Start Time #4", "Fourth trading window start", "Timing")
        self._start_time4 = self.Param("StartTime4", TimeSpan(16, 5, 0)) \
            .SetDisplay("Start Time #5", "Fifth trading window start", "Timing")
        self._start_time5 = self.Param("StartTime5", TimeSpan(20, 5, 0)) \
            .SetDisplay("Start Time #6", "Sixth trading window start", "Timing")

        self._point_value = Decimal(0)
        self._candle_size_threshold = Decimal(0)
        self._body_min_size = Decimal(0)
        self._pullback_size = Decimal(0)
        self._entry_window = TimeSpan.Zero

        self._prev_candle1 = None
        self._prev_candle2 = None
        self._entry_times = []
        self._order_placed_in_window = False

        self._entry_price = Decimal(0)

        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._exit_requested = False

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

    def OnStarted2(self, time):
        super(bull_vs_medved_window_strategy, self).OnStarted2(time)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._point_value = Decimal(ps) if ps is not None else Decimal(1)
        if self._point_value <= Decimal(0):
            self._point_value = Decimal(1)

        self._candle_size_threshold = Decimal(self.CandleSizePoints) * self._point_value
        self._body_min_size = Decimal(10) * self._point_value
        self._pullback_size = Decimal(20) * self._point_value
        self._entry_window = TimeSpan.FromMinutes(float(self.EntryWindowMinutes))

        self._entry_times = [
            self._start_time0.Value,
            self._start_time1.Value,
            self._start_time2.Value,
            self._start_time3.Value,
            self._start_time4.Value,
            self._start_time5.Value,
        ]

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._handle_position_exits(candle):
            self._shift_history(candle)
            return

        in_window = self._is_within_entry_window(candle.CloseTime)
        if not in_window:
            self._order_placed_in_window = False
            self._shift_history(candle)
            return

        if self._order_placed_in_window or self.Position != Decimal(0):
            self._shift_history(candle)
            return

        if self._prev_candle1 is None or self._prev_candle2 is None:
            self._shift_history(candle)
            return

        shift1 = candle
        shift2 = self._prev_candle1
        shift3 = self._prev_candle2

        placed_order = False

        is_bull = self._check_bull(shift3, shift2, shift1)
        is_bad_bull = self._check_bad_bull(shift3, shift2, shift1)
        is_cool_bull = self._check_cool_bull(shift2, shift1)
        is_bear = self._check_bear(shift1)

        if is_bull and not is_bad_bull:
            placed_order = self._try_buy_market(shift1)
        elif is_cool_bull:
            placed_order = self._try_buy_market(shift1)
        elif is_bear:
            placed_order = self._try_sell_market(shift1)

        if placed_order:
            self._order_placed_in_window = True

        self._shift_history(candle)

    def _handle_position_exits(self, candle):
        if self.Position > Decimal(0):
            if not self._exit_requested and self._long_stop is not None and candle.LowPrice <= self._long_stop:
                self._exit_requested = True
                self.SellMarket()
                self._reset_protection()
                return True
            if not self._exit_requested and self._long_take is not None and candle.HighPrice >= self._long_take:
                self._exit_requested = True
                self.SellMarket()
                self._reset_protection()
                return True
        elif self.Position < Decimal(0):
            if not self._exit_requested and self._short_stop is not None and candle.HighPrice >= self._short_stop:
                self._exit_requested = True
                self.BuyMarket()
                self._reset_protection()
                return True
            if not self._exit_requested and self._short_take is not None and candle.LowPrice <= self._short_take:
                self._exit_requested = True
                self.BuyMarket()
                self._reset_protection()
                return True
        return False

    def _try_buy_market(self, reference_candle):
        body = Math.Abs(reference_candle.ClosePrice - reference_candle.OpenPrice)
        stop_distance = self._round_to_point(body * self.StopLossMultiplier)
        take_distance = self._round_to_point(body * self.TakeProfitMultiplier)

        price = reference_candle.ClosePrice

        self.BuyMarket()

        self._entry_price = price
        self._long_stop = self._normalize_price(price - stop_distance) if stop_distance > Decimal(0) else None
        self._long_take = self._normalize_price(price + take_distance) if take_distance > Decimal(0) else None
        self._short_stop = None
        self._short_take = None
        self._exit_requested = False

        return True

    def _try_sell_market(self, reference_candle):
        body = Math.Abs(reference_candle.ClosePrice - reference_candle.OpenPrice)
        stop_distance = self._round_to_point(body * self.StopLossMultiplier)
        take_distance = self._round_to_point(body * self.TakeProfitMultiplier)

        price = reference_candle.ClosePrice

        self.SellMarket()

        self._entry_price = price
        self._short_stop = self._normalize_price(price + stop_distance) if stop_distance > Decimal(0) else None
        self._short_take = self._normalize_price(price - take_distance) if take_distance > Decimal(0) else None
        self._long_stop = None
        self._long_take = None
        self._exit_requested = False

        return True

    def _is_within_entry_window(self, time):
        if self._entry_window <= TimeSpan.Zero:
            return False

        tod = time.TimeOfDay

        for start in self._entry_times:
            end = start.Add(self._entry_window)
            if tod >= start and tod <= end:
                return True

        return False

    def _check_bull(self, s3, s2, s1):
        return (s3.ClosePrice > s2.OpenPrice and
                (s2.ClosePrice - s2.OpenPrice) >= self._body_min_size and
                (s1.ClosePrice - s1.OpenPrice) >= self._candle_size_threshold)

    def _check_bad_bull(self, s3, s2, s1):
        return ((s3.ClosePrice - s3.OpenPrice) >= self._body_min_size and
                (s2.ClosePrice - s2.OpenPrice) >= self._body_min_size and
                (s1.ClosePrice - s1.OpenPrice) >= self._candle_size_threshold)

    def _check_cool_bull(self, s2, s1):
        return ((s2.OpenPrice - s2.ClosePrice) >= self._pullback_size and
                s2.ClosePrice <= s1.OpenPrice and
                s1.ClosePrice > s2.OpenPrice and
                (s1.ClosePrice - s1.OpenPrice) >= Decimal(0.4) * self._candle_size_threshold)

    def _check_bear(self, s1):
        return (s1.OpenPrice - s1.ClosePrice) >= self._candle_size_threshold

    def _normalize_price(self, price):
        if self._point_value <= Decimal(0):
            return price
        steps = price / self._point_value
        rounded_steps = Decimal.Round(steps, 0)
        return rounded_steps * self._point_value

    def _round_to_point(self, value):
        if self._point_value <= Decimal(0):
            return value
        steps = value / self._point_value
        rounded_steps = Decimal.Round(steps, 0)
        return rounded_steps * self._point_value

    def _shift_history(self, candle):
        self._prev_candle2 = self._prev_candle1
        self._prev_candle1 = candle

    def _reset_protection(self):
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def OnReseted(self):
        super(bull_vs_medved_window_strategy, self).OnReseted()

        self._point_value = Decimal(0)
        self._candle_size_threshold = Decimal(0)
        self._body_min_size = Decimal(0)
        self._pullback_size = Decimal(0)
        self._entry_window = TimeSpan.Zero

        self._prev_candle1 = None
        self._prev_candle2 = None
        self._entry_times = []
        self._order_placed_in_window = False

        self._entry_price = Decimal(0)

        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._exit_requested = False

    def CreateClone(self):
        return bull_vs_medved_window_strategy()
