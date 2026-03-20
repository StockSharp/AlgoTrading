import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sv_daily_breakout_strategy(Strategy):

    def __init__(self):
        super(sv_daily_breakout_strategy, self).__init__()
        self._use_manual_volume = self.Param("UseManualVolume", False)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 50)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._start_hour = self.Param("StartHour", 0)
        self._start_minute = self.Param("StartMinute", 0)
        self._shift = self.Param("Shift", 2)
        self._interval = self.Param("Interval", 10)
        self._fast_ma_period = self.Param("FastMaPeriod", 5)
        self._slow_ma_period = self.Param("SlowMaPeriod", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._high_history = []
        self._low_history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None
        self._current_day = None
        self._pip_size = 0.0

    @property
    def UseManualVolume(self):
        return self._use_manual_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def StartMinute(self):
        return self._start_minute.Value

    @property
    def Shift(self):
        return self._shift.Value

    @property
    def Interval(self):
        return self._interval.Value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(sv_daily_breakout_strategy, self).OnStarted(time)

        sec = self.Security
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 2
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        factor = 10.0 if decimals == 3 or decimals == 5 else 1.0
        self._pip_size = step * factor
        if self._pip_size <= 0:
            self._pip_size = step if step > 0 else 0.01

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        self._update_range_history(candle)
        self._update_trailing(candle)

        if self._check_protective_exits(candle):
            return

        if float(self.Position) != 0:
            return

        lowest, highest = self._try_get_range_extremes()
        if lowest is None or highest is None:
            return

        if highest < slow_val and lowest < fast_val:
            self._enter_position(True, candle)
            return

        if lowest > slow_val and highest > fast_val:
            self._enter_position(False, candle)

    def _enter_position(self, is_long, candle):
        entry_price = float(candle.ClosePrice)
        stop_distance = self.StopLossPips * self._pip_size if self.StopLossPips > 0 else 0.0

        volume = float(self.Volume)

        if volume <= 0:
            return

        pos = float(self.Position)
        if is_long:
            total_volume = volume + (abs(pos) if pos < 0 else 0.0)
            if total_volume <= 0:
                return
            self.BuyMarket(total_volume)
            self._entry_price = entry_price
            self._stop_price = entry_price - stop_distance if self.StopLossPips > 0 else None
            self._take_profit_price = entry_price + self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else None
        else:
            total_volume = volume + (pos if pos > 0 else 0.0)
            if total_volume <= 0:
                return
            self.SellMarket(total_volume)
            self._entry_price = entry_price
            self._stop_price = entry_price + stop_distance if self.StopLossPips > 0 else None
            self._take_profit_price = entry_price - self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else None

        self._trailing_stop_price = self._stop_price if self.TrailingStopPips > 0 else None

    def _update_range_history(self, candle):
        self._high_history.append(float(candle.HighPrice))
        self._low_history.append(float(candle.LowPrice))

        max_count = max(self.Shift + self.Interval + 5, 50)
        if len(self._high_history) > max_count:
            remove = len(self._high_history) - max_count
            self._high_history = self._high_history[remove:]
            self._low_history = self._low_history[remove:]

    def _try_get_range_extremes(self):
        required = self.Shift + self.Interval
        if required <= 0:
            return (None, None)

        if len(self._low_history) < required or len(self._high_history) < required:
            return (None, None)

        low = float("inf")
        high = float("-inf")
        total = len(self._low_history)

        for offset in range(self.Shift, self.Shift + self.Interval):
            index = total - 1 - offset
            if index < 0:
                return (None, None)
            low_val = self._low_history[index]
            high_val = self._high_history[index]
            if low_val < low:
                low = low_val
            if high_val > high:
                high = high_val

        if low == float("inf") or high == float("-inf"):
            return (None, None)

        return (low, high)

    def _update_trailing(self, candle):
        if self.TrailingStopPips <= 0 or self.TrailingStepPips <= 0 or self._entry_price is None:
            return

        trail_distance = self.TrailingStopPips * self._pip_size
        step_distance = self.TrailingStepPips * self._pip_size
        pos = float(self.Position)

        if pos > 0:
            current = float(candle.ClosePrice)
            entry = self._entry_price
            if current - entry > trail_distance + step_distance:
                threshold = current - (trail_distance + step_distance)
                if self._stop_price is None or self._stop_price < threshold:
                    new_stop = current - trail_distance
                    if self._stop_price is None or new_stop > self._stop_price:
                        self._stop_price = new_stop
                        self._trailing_stop_price = new_stop
        elif pos < 0:
            current = float(candle.ClosePrice)
            entry = self._entry_price
            if entry - current > trail_distance + step_distance:
                threshold = current + trail_distance + step_distance
                if self._stop_price is None or self._stop_price > threshold:
                    new_stop = current + trail_distance
                    if self._stop_price is None or new_stop < self._stop_price:
                        self._stop_price = new_stop
                        self._trailing_stop_price = new_stop

    def _check_protective_exits(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_trade_state()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(pos)
                self._reset_trade_state()
                return True
        elif pos < 0:
            volume = abs(pos)
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(volume)
                self._reset_trade_state()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(volume)
                self._reset_trade_state()
                return True
        elif self._entry_price is not None:
            self._reset_trade_state()
        return False

    def _reset_trade_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None

    def OnReseted(self):
        super(sv_daily_breakout_strategy, self).OnReseted()
        self._high_history = []
        self._low_history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._trailing_stop_price = None
        self._current_day = None
        self._pip_size = 0.0

    def CreateClone(self):
        return sv_daily_breakout_strategy()
