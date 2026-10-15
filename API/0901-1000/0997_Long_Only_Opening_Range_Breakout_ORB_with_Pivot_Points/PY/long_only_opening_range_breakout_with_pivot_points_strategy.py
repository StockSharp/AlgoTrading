import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class long_only_opening_range_breakout_with_pivot_points_strategy(Strategy):
    """
    Long-only opening range breakout with daily pivot points.
    The opening range is the high of the first RangeMinutes after SessionStart (UTC). After the range closes, a close above the
    range high goes long when the R1 pivot of the previous day sits above that high, at most MaxTradesPerDay times a day.
    The initial stop is a percentage below entry or the previous candle low. It trails up to the pivot P, R1 and R2 when price reaches
    R1, R2 and R3, and at every daily close it is raised to StopLossPercent below that close.
    InitialSlType: Percentage or PreviousLow.
    """

    def __init__(self):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).__init__()
        self._range_minutes = self.Param("RangeMinutes", 15).SetGreaterThanZero().SetDisplay("Range Minutes", "Length of the opening range in minutes", "Session")
        self._session_start = self.Param("SessionStart", TimeSpan(9, 30, 0)).SetDisplay("Session Start", "Session start time (UTC)", "Session")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 1).SetGreaterThanZero().SetDisplay("Max Trades Per Day", "Maximum entries per day", "Session")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._initial_sl_type = self.Param("InitialSlType", "Percentage").SetDisplay("Initial SL Type", "Initial stop loss type", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._day_high = Decimal(0)
        self._day_low = Decimal(0)
        self._day_close = Decimal(0)
        self._range_high = None
        self._trades_today = 0
        self._pivot = None
        self._r1 = Decimal(0)
        self._r2 = Decimal(0)
        self._r3 = Decimal(0)
        self._prev_low = None
        self._stop_price = None

    def OnReseted(self):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(long_only_opening_range_breakout_with_pivot_points_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        day = open_time.Date

        if self._current_day is None or self._current_day != day:
            if self._current_day is not None:
                self._start_new_day()
            self._current_day = day
            self._day_high = candle.HighPrice
            self._day_low = candle.LowPrice
        else:
            self._day_high = max(self._day_high, candle.HighPrice)
            self._day_low = min(self._day_low, candle.LowPrice)

        self._day_close = candle.ClosePrice

        prev_low = self._prev_low
        self._prev_low = candle.LowPrice

        time_of_day = open_time.TimeOfDay
        session_start = self._session_start.Value
        range_end = session_start + TimeSpan.FromMinutes(self._range_minutes.Value)

        if time_of_day >= session_start and time_of_day < range_end:
            self._range_high = candle.HighPrice if self._range_high is None else max(self._range_high, candle.HighPrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._pivot is not None and self._stop_price is not None:
                level = self._stop_price
                if candle.HighPrice > self._r3:
                    level = self._r2
                elif candle.HighPrice > self._r2:
                    level = self._r1
                elif candle.HighPrice > self._r1:
                    level = self._pivot
                self._stop_price = max(self._stop_price, level)

            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                self._stop_price = None
            return

        if self.Position != 0 or time_of_day < range_end or self._trades_today >= self._max_trades_per_day.Value:
            return

        if self._range_high is None or self._pivot is None:
            return

        if candle.ClosePrice <= self._range_high or self._r1 <= self._range_high:
            return

        self.BuyMarket(self.Volume)
        self._trades_today += 1

        if str(self._initial_sl_type.Value) == "PreviousLow" and prev_low is not None:
            self._stop_price = prev_low
        else:
            self._stop_price = candle.ClosePrice * (Decimal(1) - Decimal(self._stop_loss_percent.Value) / Decimal(100))

    def _start_new_day(self):
        pivot = (self._day_high + self._day_low + self._day_close) / Decimal(3)
        self._pivot = pivot
        self._r1 = Decimal(2) * pivot - self._day_low
        self._r2 = pivot + (self._day_high - self._day_low)
        self._r3 = self._day_high + Decimal(2) * (pivot - self._day_low)

        # The stop also trails the daily close.
        if self.Position > 0 and self._stop_price is not None:
            self._stop_price = max(self._stop_price, self._day_close * (Decimal(1) - Decimal(self._stop_loss_percent.Value) / Decimal(100)))

        self._range_high = None
        self._trades_today = 0

    def CreateClone(self):
        return long_only_opening_range_breakout_with_pivot_points_strategy()
