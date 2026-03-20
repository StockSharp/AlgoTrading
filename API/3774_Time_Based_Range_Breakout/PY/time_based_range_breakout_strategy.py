import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class time_based_range_breakout_strategy(Strategy):
    """Breakout strategy that prepares daily buy/sell levels at a specified time.
    The offset and profit targets are derived from the average range of previous days."""

    def __init__(self):
        super(time_based_range_breakout_strategy, self).__init__()

        self._check_hour = self.Param("CheckHour", 8) \
            .SetDisplay("Check Hour", "Hour of the day used for daily calculations", "Schedule")
        self._check_minute = self.Param("CheckMinute", 0) \
            .SetDisplay("Check Minute", "Minute of the hour used for daily calculations", "Schedule")
        self._days_to_check = self.Param("DaysToCheck", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Days To Check", "Number of previous days used in averaging", "Averaging")
        self._check_mode = self.Param("CheckMode", 1) \
            .SetDisplay("Check Mode", "1 - use daily range, 2 - use absolute close difference", "Averaging")
        self._profit_factor = self.Param("ProfitFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit Factor", "Divisor applied to average range for take-profit", "Risk")
        self._loss_factor = self.Param("LossFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Loss Factor", "Divisor applied to average range for stop-loss", "Risk")
        self._offset_factor = self.Param("OffsetFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Offset Factor", "Divisor applied to average range for breakout levels", "Entries")
        self._close_mode = self.Param("CloseMode", 1) \
            .SetDisplay("Close Mode", "1 - keep positions overnight, 2 - close on new day", "Risk")
        self._trades_per_day = self.Param("TradesPerDay", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Trades Per Day", "Maximum entries allowed within one day", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series used by the strategy", "Data")
        self._last_open_hour = self.Param("LastOpenHour", 23) \
            .SetDisplay("Last Open Hour", "Hour after which new trades are not opened", "Schedule")

        self._range_history = []
        self._close_diff_history = []
        self._current_day = None
        self._levels_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._buy_breakout = 0.0
        self._sell_breakout = 0.0
        self._profit_distance = 0.0
        self._loss_distance = 0.0
        self._previous_check_close = None
        self._current_check_close = None
        self._trades_opened_today = 0
        self._levels_ready = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CheckHour(self):
        return self._check_hour.Value

    @property
    def CheckMinute(self):
        return self._check_minute.Value

    @property
    def DaysToCheck(self):
        return self._days_to_check.Value

    @property
    def CheckMode(self):
        return self._check_mode.Value

    @property
    def ProfitFactor(self):
        return self._profit_factor.Value

    @property
    def LossFactor(self):
        return self._loss_factor.Value

    @property
    def OffsetFactor(self):
        return self._offset_factor.Value

    @property
    def CloseMode(self):
        return self._close_mode.Value

    @property
    def TradesPerDay(self):
        return self._trades_per_day.Value

    @property
    def LastOpenHour(self):
        return self._last_open_hour.Value

    def OnReseted(self):
        super(time_based_range_breakout_strategy, self).OnReseted()
        self._range_history = []
        self._close_diff_history = []
        self._current_day = None
        self._levels_day = None
        self._day_high = 0.0
        self._day_low = 0.0
        self._buy_breakout = 0.0
        self._sell_breakout = 0.0
        self._profit_distance = 0.0
        self._loss_distance = 0.0
        self._previous_check_close = None
        self._current_check_close = None
        self._trades_opened_today = 0
        self._levels_ready = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(time_based_range_breakout_strategy, self).OnStarted(time)

        self._range_history = []
        self._close_diff_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _enqueue_with_limit(self, lst, value, limit):
        lst.append(value)
        while len(lst) > limit:
            lst.pop(0)

    def _finalize_previous_day(self):
        day_range = self._day_high - self._day_low
        if day_range > 0:
            self._enqueue_with_limit(self._range_history, day_range, self.DaysToCheck)

        if self._current_check_close is not None:
            if self._previous_check_close is not None:
                difference = abs(self._current_check_close - self._previous_check_close)
                if difference > 0:
                    self._enqueue_with_limit(self._close_diff_history, difference, self.DaysToCheck)
            self._previous_check_close = self._current_check_close

        self._current_check_close = None

    def _try_get_average(self):
        source = self._close_diff_history if self.CheckMode == 2 else self._range_history
        if len(source) == 0:
            return None
        return sum(source) / len(source)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        candle_date = candle.OpenTime.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # Update daily state
        if self._current_day is None or candle_date != self._current_day:
            if self._current_day is not None:
                self._finalize_previous_day()

            if self.CloseMode == 2 and self.Position != 0:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()

            self._current_day = candle_date
            self._day_high = high
            self._day_low = low
            self._levels_ready = False
            self._levels_day = None
            self._current_check_close = None
            self._trades_opened_today = 0
        else:
            if high > self._day_high:
                self._day_high = high
            if low < self._day_low:
                self._day_low = low

        # Try to calculate levels at the designated time
        if candle.OpenTime.Hour == self.CheckHour and candle.OpenTime.Minute == self.CheckMinute:
            self._current_check_close = close

            if self.Position != 0:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()

            average = self._try_get_average()
            if average is None:
                self._levels_ready = False
                self._levels_day = None
            else:
                offset_factor = float(self.OffsetFactor)
                profit_factor = float(self.ProfitFactor)
                loss_factor = float(self.LossFactor)

                offset = average / offset_factor if offset_factor > 0 else 0.0
                self._profit_distance = average / profit_factor if profit_factor > 0 else 0.0
                self._loss_distance = average / loss_factor if loss_factor > 0 else 0.0

                self._buy_breakout = self._day_high + offset
                self._sell_breakout = self._day_low - offset
                self._levels_ready = True
                self._levels_day = self._current_day

        # Manage open position
        if self.Position != 0:
            entry_price = self._entry_price
            if entry_price != 0:
                if self.Position > 0:
                    reached_profit = self._profit_distance > 0 and close - entry_price >= self._profit_distance
                    reached_loss = self._loss_distance > 0 and entry_price - close >= self._loss_distance
                    if reached_profit or reached_loss:
                        self.SellMarket()
                elif self.Position < 0:
                    reached_profit = self._profit_distance > 0 and entry_price - close >= self._profit_distance
                    reached_loss = self._loss_distance > 0 and close - entry_price >= self._loss_distance
                    if reached_profit or reached_loss:
                        self.BuyMarket()

        # Try to enter position
        if not self._levels_ready or self._levels_day is None or self._current_day is None:
            return
        if self._levels_day != self._current_day:
            return
        if self._trades_opened_today >= self.TradesPerDay:
            return
        if candle.OpenTime.Hour > self.LastOpenHour:
            return
        if self.Position != 0:
            return

        if close >= self._buy_breakout:
            self.BuyMarket()
            self._entry_price = close
            self._trades_opened_today += 1
        elif close <= self._sell_breakout:
            self.SellMarket()
            self._entry_price = close
            self._trades_opened_today += 1

    def CreateClone(self):
        return time_based_range_breakout_strategy()
