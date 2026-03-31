import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import (ExponentialMovingAverage, DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal


class brandy_strategy(Strategy):
    def __init__(self):
        super(brandy_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 150.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._ma_close_period = self.Param("MaClosePeriod", 20)
        self._ma_close_shift = self.Param("MaCloseShift", 0)
        self._ma_close_signal_bar = self.Param("MaCloseSignalBar", 0)
        self._ma_open_period = self.Param("MaOpenPeriod", 70)
        self._ma_open_shift = self.Param("MaOpenShift", 0)
        self._ma_open_signal_bar = self.Param("MaOpenSignalBar", 0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._ma_open_indicator = None
        self._ma_close_indicator = None
        self._pip_size = 1.0
        self._ma_open_values = []
        self._ma_close_values = []
        self._max_open_queue_size = 2
        self._max_close_queue_size = 2
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(brandy_strategy, self).OnStarted2(time)

        self._ma_open_indicator = ExponentialMovingAverage()
        self._ma_open_indicator.Length = self._ma_open_period.Value
        self._ma_close_indicator = ExponentialMovingAverage()
        self._ma_close_indicator.Length = self._ma_close_period.Value

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        self._pip_size = step if step > 0 else 1.0

        shift_open = max(0, self._ma_open_shift.Value)
        shift_close = max(0, self._ma_close_shift.Value)
        open_depth = max(max(1 + shift_open, self._ma_open_signal_bar.Value + shift_open),
                         self._ma_close_signal_bar.Value + shift_open)
        close_depth = max(1 + shift_close, 1)
        self._max_open_queue_size = max(2, open_depth + 2)
        self._max_close_queue_size = max(2, close_depth + 2)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ma_open_indicator is None or self._ma_close_indicator is None:
            return

        open_source = float(candle.ClosePrice)
        close_source = float(candle.ClosePrice)

        d1 = DecimalIndicatorValue(self._ma_open_indicator, Decimal(float(open_source)), candle.OpenTime)
        d1.IsFinal = True
        ma_open_result = self._ma_open_indicator.Process(d1)
        d2 = DecimalIndicatorValue(self._ma_close_indicator, Decimal(float(close_source)), candle.OpenTime)
        d2.IsFinal = True
        ma_close_result = self._ma_close_indicator.Process(d2)

        if (ma_open_result.IsEmpty or ma_close_result.IsEmpty or
                not self._ma_open_indicator.IsFormed or not self._ma_close_indicator.IsFormed):
            return

        ma_open = float(ma_open_result.Value)
        ma_close = float(ma_close_result.Value)

        self._enqueue_value(self._ma_open_values, ma_open, self._max_open_queue_size)
        self._enqueue_value(self._ma_close_values, ma_close, self._max_close_queue_size)

        ma_open_prev = self._get_queue_value(self._ma_open_values, 1 + self._ma_open_shift.Value)
        ma_open_signal = self._get_queue_value(self._ma_open_values, self._ma_open_signal_bar.Value + self._ma_open_shift.Value)
        ma_close_prev = self._get_queue_value(self._ma_close_values, 1 + self._ma_close_shift.Value)
        ma_close_signal = self._get_queue_value(self._ma_close_values, self._ma_close_signal_bar.Value + self._ma_close_shift.Value)

        if ma_open_prev is None or ma_open_signal is None or ma_close_prev is None or ma_close_signal is None:
            return

        long_signal = ma_open_prev > ma_open_signal and ma_close_prev > ma_close_signal
        short_signal = ma_open_prev < ma_open_signal and ma_close_prev < ma_close_signal

        if self.Position == 0:
            if long_signal:
                self._open_long(float(candle.ClosePrice))
            elif short_signal:
                self._open_short(float(candle.ClosePrice))
        else:
            self._manage_open_position(candle, ma_open_prev, ma_open_signal)

    def _open_long(self, price):
        self._entry_price = price
        self._stop_price = price - self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
        self._take_price = price + self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
        self.BuyMarket()

    def _open_short(self, price):
        self._entry_price = price
        self._stop_price = price + self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
        self._take_price = price - self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
        self.SellMarket()

    def _manage_open_position(self, candle, ma_open_prev, ma_open_signal):
        if self.Position > 0:
            if ma_open_prev < ma_open_signal:
                self.SellMarket(self.Position)
                self._reset_position_state()
                return
            self._update_trailing_for_long(candle)
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(self.Position)
                self._reset_position_state()
                return
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_position_state()
        elif self.Position < 0:
            if ma_open_prev > ma_open_signal:
                self.BuyMarket(abs(self.Position))
                self._reset_position_state()
                return
            self._update_trailing_for_short(candle)
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(self.Position))
                self._reset_position_state()
                return
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_position_state()
        else:
            self._reset_position_state()

    def _update_trailing_for_long(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._entry_price is None:
            return
        trailing_stop = self._trailing_stop_pips.Value * self._pip_size
        trailing_step = self._trailing_step_pips.Value * self._pip_size
        current_price = float(candle.ClosePrice)
        if current_price - self._entry_price <= trailing_stop + trailing_step:
            return
        threshold = current_price - (trailing_stop + trailing_step)
        if self._stop_price is not None and self._stop_price >= threshold:
            return
        new_stop = current_price - trailing_stop
        if self._stop_price is None or new_stop > self._stop_price:
            self._stop_price = new_stop

    def _update_trailing_for_short(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._entry_price is None:
            return
        trailing_stop = self._trailing_stop_pips.Value * self._pip_size
        trailing_step = self._trailing_step_pips.Value * self._pip_size
        current_price = float(candle.ClosePrice)
        if self._entry_price - current_price <= trailing_stop + trailing_step:
            return
        threshold = current_price + trailing_stop + trailing_step
        if self._stop_price is not None and self._stop_price <= threshold:
            return
        new_stop = current_price + trailing_stop
        if self._stop_price is None or new_stop < self._stop_price:
            self._stop_price = new_stop

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def _enqueue_value(self, queue, value, max_size):
        queue.append(value)
        while len(queue) > max_size:
            queue.pop(0)

    def _get_queue_value(self, queue, index_from_current):
        if index_from_current < 0:
            return None
        if len(queue) <= index_from_current:
            return None
        target = len(queue) - 1 - index_from_current
        if target >= 0 and target < len(queue):
            return queue[target]
        return None

    def OnReseted(self):
        super(brandy_strategy, self).OnReseted()
        self._ma_open_indicator = None
        self._ma_close_indicator = None
        self._pip_size = 1.0
        self._ma_open_values = []
        self._ma_close_values = []
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return brandy_strategy()
