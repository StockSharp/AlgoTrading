import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, MovingAverageConvergenceDivergence, ExponentialMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class momo_trades_strategy(Strategy):
    def __init__(self):
        super(momo_trades_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 22)
        self._ma_bar_shift = self.Param("MaBarShift", 6)
        self._macd_fast = self.Param("MacdFast", 12)
        self._macd_slow = self.Param("MacdSlow", 26)
        self._macd_signal = self.Param("MacdSignal", 9)
        self._macd_bar_shift = self.Param("MacdBarShift", 2)
        self._stop_loss_pips = self.Param("StopLossPips", 25.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 0.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._breakeven_pips = self.Param("BreakevenPips", 10.0)
        self._price_shift_pips = self.Param("PriceShiftPips", 5.0)
        self._close_end_day = self.Param("CloseEndDay", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._sma = None
        self._macd = None
        self._macd_history = [0.0] * 64
        self._ma_history = [0.0] * 64
        self._close_history = [0.0] * 64
        self._macd_count = 0
        self._ma_count = 0
        self._close_count = 0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._breakeven_trigger = None
        self._trailing_distance = None
        self._trailing_step = None
        self._is_long = False
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @property
    def MaBarShift(self):
        return self._ma_bar_shift.Value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @property
    def MacdBarShift(self):
        return self._macd_bar_shift.Value

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
    def BreakevenPips(self):
        return self._breakeven_pips.Value

    @property
    def PriceShiftPips(self):
        return self._price_shift_pips.Value

    @property
    def CloseEndDay(self):
        return self._close_end_day.Value

    def OnStarted2(self, time):
        super(momo_trades_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.SmaPeriod

        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.MacdSlow
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.MacdFast
        self._macd = MovingAverageConvergenceDivergence(slow_ema, fast_ema)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._macd, self._process_candle).Start()

    def _process_candle(self, candle, sma_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._macd.IsFormed:
            return

        self._push_value(self._close_history, float(candle.ClosePrice), "close")
        self._push_value(self._ma_history, float(sma_value), "ma")
        self._push_value(self._macd_history, float(macd_value), "macd")

        self._manage_active_position(candle)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if self.Position != 0:
            return

        if self.CloseEndDay and self._should_close_for_day(candle):
            return

        shift = self.MaBarShift
        if self._close_count <= shift or self._ma_count <= shift:
            return

        shifted_close = self._close_history[shift]
        shifted_ma = self._ma_history[shift]

        price_shift = self._get_pip_value(self.PriceShiftPips)

        ema_buy = shifted_close - shifted_ma > price_shift
        ema_sell = shifted_ma - shifted_close > price_shift

        macd_buy = self._check_macd_pattern(True)
        macd_sell = self._check_macd_pattern(False)

        if macd_buy and ema_buy:
            self._enter_long(float(candle.ClosePrice))
            self._cooldown = 5
        elif macd_sell and ema_sell:
            self._enter_short(float(candle.ClosePrice))
            self._cooldown = 5

    def _manage_active_position(self, candle):
        if self.Position == 0:
            return

        if self.CloseEndDay and self._should_close_for_day(candle):
            self._close_position()
            return

        close = float(candle.ClosePrice)

        if self._trailing_distance is not None and self._trailing_step is not None:
            if self._is_long:
                if close - self._entry_price > self._trailing_distance + self._trailing_step:
                    new_stop = close - self._trailing_distance
                    if self._stop_price is None or new_stop > self._stop_price:
                        self._stop_price = new_stop
            else:
                if self._entry_price - close > self._trailing_distance + self._trailing_step:
                    new_stop = close + self._trailing_distance
                    if self._stop_price is None or new_stop < self._stop_price:
                        self._stop_price = new_stop
        elif self._breakeven_trigger is not None:
            if self._is_long:
                if close > self._breakeven_trigger:
                    self._stop_price = self._entry_price
                    self._breakeven_trigger = None
            else:
                if close < self._breakeven_trigger:
                    self._stop_price = self._entry_price
                    self._breakeven_trigger = None

        if self._is_long:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_position_state()
                return
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset_position_state()
        else:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_position_state()
                return
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset_position_state()

    def _enter_long(self, price):
        self.BuyMarket()
        self._entry_price = price
        self._is_long = True

        stop = self._get_pip_value(self.StopLossPips)
        take = self._get_pip_value(self.TakeProfitPips)
        trail = self._get_pip_value(self.TrailingStopPips)
        step = self._get_pip_value(self.TrailingStepPips)
        breakeven = self._get_pip_value(self.BreakevenPips)

        self._stop_price = price - stop if self.StopLossPips > 0 else None
        self._take_price = price + take if self.TakeProfitPips > 0 else None

        if self.TakeProfitPips <= 0 and self.BreakevenPips > 0:
            self._breakeven_trigger = price + breakeven
        else:
            self._breakeven_trigger = None

        if self.TakeProfitPips > 0 and self.TrailingStopPips > 0 and self.TrailingStepPips > 0:
            self._trailing_distance = trail
            self._trailing_step = step
        else:
            self._trailing_distance = None
            self._trailing_step = None

    def _enter_short(self, price):
        self.SellMarket()
        self._entry_price = price
        self._is_long = False

        stop = self._get_pip_value(self.StopLossPips)
        take = self._get_pip_value(self.TakeProfitPips)
        trail = self._get_pip_value(self.TrailingStopPips)
        step = self._get_pip_value(self.TrailingStepPips)
        breakeven = self._get_pip_value(self.BreakevenPips)

        self._stop_price = price + stop if self.StopLossPips > 0 else None
        self._take_price = price - take if self.TakeProfitPips > 0 else None

        if self.TakeProfitPips <= 0 and self.BreakevenPips > 0:
            self._breakeven_trigger = price - breakeven
        else:
            self._breakeven_trigger = None

        if self.TakeProfitPips > 0 and self.TrailingStopPips > 0 and self.TrailingStepPips > 0:
            self._trailing_distance = trail
            self._trailing_step = step
        else:
            self._trailing_distance = None
            self._trailing_step = None

    def _check_macd_pattern(self, is_long):
        base = self.MacdBarShift
        required = base + 8
        if self._macd_count <= required:
            return False

        v3 = self._macd_history[base + 3]
        v4 = self._macd_history[base + 4]
        v5 = self._macd_history[base + 5]
        v6 = self._macd_history[base + 6]
        v7 = self._macd_history[base + 7]
        v8 = self._macd_history[base + 8]

        if is_long:
            return v3 > v4 and v4 > v5 and v5 >= 0 and v6 <= 0 and v6 > v7 and v7 > v8
        return v3 < v4 and v4 < v5 and v5 <= 0 and v6 >= 0 and v6 < v7 and v7 < v8

    def _push_value(self, buf, value, which):
        length = len(buf)
        if which == "close":
            cnt = self._close_count
        elif which == "ma":
            cnt = self._ma_count
        else:
            cnt = self._macd_count

        if cnt < length:
            cnt += 1

        for i in range(cnt - 1, 0, -1):
            buf[i] = buf[i - 1]
        buf[0] = value

        if which == "close":
            self._close_count = cnt
        elif which == "ma":
            self._ma_count = cnt
        else:
            self._macd_count = cnt

    def _get_pip_value(self, pips):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0001
        return pips * step * 10.0

    def _should_close_for_day(self, candle):
        t = candle.CloseTime
        day = t.DayOfWeek
        end_hour = 21 if day == 5 else 23
        return t.Hour >= end_hour

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._reset_position_state()

    def _reset_position_state(self):
        self._stop_price = None
        self._take_price = None
        self._breakeven_trigger = None
        self._trailing_distance = None
        self._trailing_step = None
        self._is_long = False
        self._entry_price = 0.0

    def OnReseted(self):
        super(momo_trades_strategy, self).OnReseted()
        self._macd_history = [0.0] * 64
        self._ma_history = [0.0] * 64
        self._close_history = [0.0] * 64
        self._macd_count = 0
        self._ma_count = 0
        self._close_count = 0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._breakeven_trigger = None
        self._trailing_distance = None
        self._trailing_step = None
        self._is_long = False
        self._cooldown = 0

    def CreateClone(self):
        return momo_trades_strategy()
