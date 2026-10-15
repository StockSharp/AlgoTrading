import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class up3x1_dynamic_sizing_strategy(Strategy):
    def __init__(self):
        super(up3x1_dynamic_sizing_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 24)
        self._medium_period = self.Param("MediumPeriod", 60)
        self._slow_period = self.Param("SlowPeriod", 120)
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.015)
        self._stop_loss_offset = self.Param("StopLossOffset", 0.01)
        self._trailing_stop_offset = self.Param("TrailingStopOffset", 0.004)
        self._base_volume = self.Param("BaseVolume", 0.1)
        self._risk_fraction = self.Param("RiskFraction", 0.02)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._has_prev_values = False
        self._prev_fast = 0.0
        self._prev_medium = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._losses = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def MediumPeriod(self):
        return self._medium_period.Value

    @MediumPeriod.setter
    def MediumPeriod(self, value):
        self._medium_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def TakeProfitOffset(self):
        return self._take_profit_offset.Value

    @TakeProfitOffset.setter
    def TakeProfitOffset(self, value):
        self._take_profit_offset.Value = value

    @property
    def StopLossOffset(self):
        return self._stop_loss_offset.Value

    @StopLossOffset.setter
    def StopLossOffset(self, value):
        self._stop_loss_offset.Value = value

    @property
    def TrailingStopOffset(self):
        return self._trailing_stop_offset.Value

    @TrailingStopOffset.setter
    def TrailingStopOffset(self, value):
        self._trailing_stop_offset.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def RiskFraction(self):
        return self._risk_fraction.Value

    @RiskFraction.setter
    def RiskFraction(self, value):
        self._risk_fraction.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(up3x1_dynamic_sizing_strategy, self).OnStarted2(time)

        self._has_prev_values = False
        self._prev_fast = 0.0
        self._prev_medium = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        medium_ema = ExponentialMovingAverage()
        medium_ema.Length = self.MediumPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        self._fast_ema = fast_ema
        self._medium_ema = medium_ema
        self._slow_ema = slow_ema

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, medium_ema, slow_ema, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast_value, medium_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        medium_val = float(medium_value)
        slow_val = float(slow_value)

        if not self._fast_ema.IsFormed or not self._medium_ema.IsFormed or not self._slow_ema.IsFormed:
            return

        if not self._has_prev_values:
            self._prev_fast = fast_val
            self._prev_medium = medium_val
            self._prev_slow = slow_val
            self._has_prev_values = True
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_offset = float(self.TakeProfitOffset)
        sl_offset = float(self.StopLossOffset)
        trail_offset = float(self.TrailingStopOffset)

        if self.Position > 0:
            if self._try_handle_long_exit(candle, fast_val, medium_val, slow_val):
                self._prev_fast = fast_val
                self._prev_medium = medium_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0:
            if self._try_handle_short_exit(candle, fast_val, medium_val, slow_val):
                self._prev_fast = fast_val
                self._prev_medium = medium_val
                self._prev_slow = slow_val
                return
        else:
            bullish_setup = (self._prev_fast < self._prev_medium and
                             self._prev_medium < self._prev_slow and
                             medium_val < fast_val and fast_val < slow_val)
            bearish_setup = (self._prev_fast > self._prev_medium and
                             self._prev_medium > self._prev_slow and
                             medium_val > fast_val and fast_val > slow_val)

            if bullish_setup:
                self.BuyMarket()
                self._entry_price = close
                self._highest_price = high
                self._lowest_price = low
            elif bearish_setup:
                self.SellMarket()
                self._entry_price = close
                self._highest_price = high
                self._lowest_price = low

        self._prev_fast = fast_val
        self._prev_medium = medium_val
        self._prev_slow = slow_val

    def _try_handle_long_exit(self, candle, fast_val, medium_val, slow_val):
        if self._entry_price <= 0.0:
            return False

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_offset = float(self.TakeProfitOffset)
        sl_offset = float(self.StopLossOffset)
        trail_offset = float(self.TrailingStopOffset)

        exit_price = 0.0

        if tp_offset > 0.0:
            target = self._entry_price + tp_offset
            if high >= target:
                exit_price = target

        if exit_price == 0.0 and sl_offset > 0.0:
            stop = self._entry_price - sl_offset
            if low <= stop:
                exit_price = stop

        if high > self._highest_price:
            self._highest_price = high

        if exit_price == 0.0 and trail_offset > 0.0 and self._highest_price - self._entry_price > trail_offset:
            trail = self._highest_price - trail_offset
            if low <= trail:
                exit_price = trail

        if exit_price == 0.0:
            reversal = (self._prev_fast > self._prev_medium and
                        self._prev_medium > self._prev_slow and
                        slow_val < fast_val and fast_val < medium_val)
            if reversal:
                exit_price = close

        if exit_price == 0.0:
            return False

        self._exit_position(exit_price)
        return True

    def _try_handle_short_exit(self, candle, fast_val, medium_val, slow_val):
        if self._entry_price <= 0.0:
            return False

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tp_offset = float(self.TakeProfitOffset)
        sl_offset = float(self.StopLossOffset)
        trail_offset = float(self.TrailingStopOffset)

        exit_price = 0.0

        if tp_offset > 0.0:
            target = self._entry_price - tp_offset
            if low <= target:
                exit_price = target

        if exit_price == 0.0 and sl_offset > 0.0:
            stop = self._entry_price + sl_offset
            if high >= stop:
                exit_price = stop

        if self._lowest_price == 0.0 or low < self._lowest_price:
            self._lowest_price = low

        if exit_price == 0.0 and trail_offset > 0.0 and self._entry_price - self._lowest_price > trail_offset:
            trail = self._lowest_price + trail_offset
            if high >= trail:
                exit_price = trail

        if exit_price == 0.0:
            reversal = (self._prev_fast > self._prev_medium and
                        self._prev_medium > self._prev_slow and
                        slow_val < fast_val and fast_val < medium_val)
            if reversal:
                exit_price = close

        if exit_price == 0.0:
            return False

        self._exit_position(exit_price)
        return True

    def _exit_position(self, exit_price):
        is_long = self.Position > 0

        if is_long:
            pnl = exit_price - self._entry_price
            self.SellMarket()
        else:
            pnl = self._entry_price - exit_price
            self.BuyMarket()

        if pnl < 0.0:
            self._losses += 1

        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def OnReseted(self):
        super(up3x1_dynamic_sizing_strategy, self).OnReseted()
        self._has_prev_values = False
        self._prev_fast = 0.0
        self._prev_medium = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._losses = 0

    def CreateClone(self):
        return up3x1_dynamic_sizing_strategy()
