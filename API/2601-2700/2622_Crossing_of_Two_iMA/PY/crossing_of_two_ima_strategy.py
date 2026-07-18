import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage
)


class crossing_of_two_ima_strategy(Strategy):
    """Two MA crossover strategy with optional third MA filter, trailing stop and simulated pending orders."""

    def __init__(self):
        super(crossing_of_two_ima_strategy, self).__init__()

        self._first_period = self.Param("FirstMaPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("First MA Period", "Period of the first moving average", "First MA")
        self._first_shift = self.Param("FirstMaShift", 3) \
            .SetDisplay("First MA Shift", "Shift applied to the first MA", "First MA")
        self._second_period = self.Param("SecondMaPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Second MA Period", "Period of the second moving average", "Second MA")
        self._second_shift = self.Param("SecondMaShift", 5) \
            .SetDisplay("Second MA Shift", "Shift applied to the second MA", "Second MA")
        self._use_third = self.Param("UseThirdMA", True) \
            .SetDisplay("Use Third MA", "Enable third MA as directional filter", "Third MA")
        self._third_period = self.Param("ThirdMaPeriod", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Third MA Period", "Period of the third moving average", "Third MA")
        self._third_shift = self.Param("ThirdMaShift", 8) \
            .SetDisplay("Third MA Shift", "Shift applied to the third MA", "Third MA")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Initial stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 4) \
            .SetDisplay("Trailing Step (pips)", "Progress before advancing trailing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self._first_values = []
        self._second_values = []
        self._third_values = []
        self._pip_size = 1.0
        self._entry_price = None
        self._active_sl = None
        self._active_tp = None
        self._is_long = False
        self._first_ma = None
        self._second_ma = None
        self._third_ma = None

    @property
    def FirstMaPeriod(self):
        return self._first_period.Value
    @property
    def FirstMaShift(self):
        return self._first_shift.Value
    @property
    def SecondMaPeriod(self):
        return self._second_period.Value
    @property
    def SecondMaShift(self):
        return self._second_shift.Value
    @property
    def UseThirdMA(self):
        return self._use_third.Value
    @property
    def ThirdMaPeriod(self):
        return self._third_period.Value
    @property
    def ThirdMaShift(self):
        return self._third_shift.Value
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
    def CandleType(self):
        return self._candle_type.Value

    def _create_ma(self, period):
        ma = SimpleMovingAverage()
        ma.Length = max(1, period)
        return ma

    def OnStarted2(self, time):
        super(crossing_of_two_ima_strategy, self).OnStarted2(time)

        self._first_ma = self._create_ma(self.FirstMaPeriod)
        self._second_ma = self._create_ma(self.SecondMaPeriod)
        self._third_ma = self._create_ma(self.ThirdMaPeriod) if self.UseThirdMA else None

        sec = self.Security
        self._pip_size = 1.0
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            self._pip_size = float(sec.PriceStep)
            decimals = sec.Decimals if sec.Decimals is not None else 0
            if decimals == 3 or decimals == 5:
                self._pip_size *= 10.0

        subscription = self.SubscribeCandles(self.CandleType)
        if self.UseThirdMA and self._third_ma is not None:
            subscription.Bind(self._first_ma, self._second_ma, self._third_ma, self._process_3).Start()
        else:
            subscription.Bind(self._first_ma, self._second_ma, self._process_2).Start()

    def _process_2(self, candle, first_val, second_val):
        self._process_internal(candle, first_val, second_val, None)

    def _process_3(self, candle, first_val, second_val, third_val):
        self._process_internal(candle, first_val, second_val, third_val)

    def _process_internal(self, candle, first_val, second_val, third_val):
        if candle.State != CandleStates.Finished:
            return

        self._manage_position(candle)

        fv = float(first_val)
        sv = float(second_val)

        self._update_series(self._first_values, self.FirstMaShift, fv)
        self._update_series(self._second_values, self.SecondMaShift, sv)

        if self.UseThirdMA and third_val is not None:
            self._update_series(self._third_values, self.ThirdMaShift, float(third_val))

        if not self._first_ma.IsFormed or not self._second_ma.IsFormed:
            return

        third_current = None
        if self.UseThirdMA:
            if self._third_ma is None or not self._third_ma.IsFormed:
                return
            third_current = self._get_series_val(self._third_values, self.ThirdMaShift, 0)

        f0 = self._get_series_val(self._first_values, self.FirstMaShift, 0)
        f1 = self._get_series_val(self._first_values, self.FirstMaShift, 1)
        s0 = self._get_series_val(self._second_values, self.SecondMaShift, 0)
        s1 = self._get_series_val(self._second_values, self.SecondMaShift, 1)

        if f0 is None or f1 is None or s0 is None or s1 is None:
            return

        sl = self.StopLossPips * self._pip_size if self.StopLossPips > 0 else 0.0
        tp = self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 else 0.0

        close = float(candle.ClosePrice)

        if f0 > s0 and f1 < s1:
            if not self.UseThirdMA or third_current is None or third_current < f0:
                self._enter_long(close, sl, tp)
                return

        if f0 < s0 and f1 > s1:
            if not self.UseThirdMA or third_current is None or third_current > f0:
                self._enter_short(close, sl, tp)
                return

    def _enter_long(self, close, sl_offset, tp_offset):
        if self.Position > 0:
            return
        self.BuyMarket()
        self._entry_price = close
        self._active_sl = close - sl_offset if sl_offset > 0 else None
        self._active_tp = close + tp_offset if tp_offset > 0 else None
        self._is_long = True

    def _enter_short(self, close, sl_offset, tp_offset):
        if self.Position < 0:
            return
        self.SellMarket()
        self._entry_price = close
        self._active_sl = close + sl_offset if sl_offset > 0 else None
        self._active_tp = close - tp_offset if tp_offset > 0 else None
        self._is_long = False

    def _manage_position(self, candle):
        if self.Position == 0:
            return

        if self._is_long and self.Position > 0:
            if self._active_tp is not None and float(candle.HighPrice) >= self._active_tp:
                self.SellMarket()
                self._reset_position()
                return
            if self._active_sl is not None and float(candle.LowPrice) <= self._active_sl:
                self.SellMarket()
                self._reset_position()
                return
            self._update_trailing_long(candle)
        elif not self._is_long and self.Position < 0:
            if self._active_tp is not None and float(candle.LowPrice) <= self._active_tp:
                self.BuyMarket()
                self._reset_position()
                return
            if self._active_sl is not None and float(candle.HighPrice) >= self._active_sl:
                self.BuyMarket()
                self._reset_position()
                return
            self._update_trailing_short(candle)

    def _update_trailing_long(self, candle):
        if self.TrailingStopPips <= 0:
            return
        trail_dist = self.TrailingStopPips * self._pip_size
        trail_step = self.TrailingStepPips * self._pip_size
        target = float(candle.ClosePrice) - trail_dist
        if self._active_sl is None or target <= self._active_sl:
            return
        if trail_step <= 0 or self._active_sl < target - trail_step:
            self._active_sl = target

    def _update_trailing_short(self, candle):
        if self.TrailingStopPips <= 0:
            return
        trail_dist = self.TrailingStopPips * self._pip_size
        trail_step = self.TrailingStepPips * self._pip_size
        target = float(candle.ClosePrice) + trail_dist
        if self._active_sl is None or target >= self._active_sl:
            return
        if trail_step <= 0 or self._active_sl > target + trail_step:
            self._active_sl = target

    def _reset_position(self):
        self._entry_price = None
        self._active_sl = None
        self._active_tp = None
        self._is_long = False

    def _update_series(self, values, shift, value):
        values.append(value)
        max_size = max(shift + 3, 3)
        while len(values) > max_size:
            values.pop(0)

    def _get_series_val(self, values, shift, index):
        target = len(values) - 1 - shift - index
        if target < 0 or target >= len(values):
            return None
        return values[target]

    def OnReseted(self):
        super(crossing_of_two_ima_strategy, self).OnReseted()
        self._first_values = []
        self._second_values = []
        self._third_values = []
        self._reset_position()
        self._first_ma = None
        self._second_ma = None
        self._third_ma = None

    def CreateClone(self):
        return crossing_of_two_ima_strategy()
