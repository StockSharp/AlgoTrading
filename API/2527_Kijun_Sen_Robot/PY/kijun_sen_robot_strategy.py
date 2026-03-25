import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku, WeightedMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class kijun_sen_robot_strategy(Strategy):
    def __init__(self):
        super(kijun_sen_robot_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 6)
        self._kijun_period = self.Param("KijunPeriod", 12)
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 24)
        self._lwma_period = self.Param("LwmaPeriod", 20)
        self._ma_filter_pips = self.Param("MaFilterPips", 20.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._break_even_pips = self.Param("BreakEvenPips", 9.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 120.0)
        self._trading_start_hour = self.Param("TradingStartHour", 7)
        self._trading_end_hour = self.Param("TradingEndHour", 19)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._previous_close = None
        self._previous_ma = None
        self._previous_prev_ma = None
        self._previous_kijun = None
        self._previous_prev_kijun = None
        self._pending_long_level = None
        self._pending_short_level = None
        self._is_long = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._sl_distance = 0.0
        self._tp_distance = 0.0
        self._be_distance = 0.0
        self._be_step = 0.0
        self._trail_distance = 0.0
        self._be_applied = False

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def LwmaPeriod(self):
        return self._lwma_period.Value

    @LwmaPeriod.setter
    def LwmaPeriod(self, value):
        self._lwma_period.Value = value

    @property
    def MaFilterPips(self):
        return self._ma_filter_pips.Value

    @MaFilterPips.setter
    def MaFilterPips(self, value):
        self._ma_filter_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def BreakEvenPips(self):
        return self._break_even_pips.Value

    @BreakEvenPips.setter
    def BreakEvenPips(self, value):
        self._break_even_pips.Value = value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def TradingStartHour(self):
        return self._trading_start_hour.Value

    @TradingStartHour.setter
    def TradingStartHour(self, value):
        self._trading_start_hour.Value = value

    @property
    def TradingEndHour(self):
        return self._trading_end_hour.Value

    @TradingEndHour.setter
    def TradingEndHour(self, value):
        self._trading_end_hour.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(kijun_sen_robot_strategy, self).OnStarted(time)

        self._previous_close = None
        self._previous_ma = None
        self._previous_prev_ma = None
        self._previous_kijun = None
        self._previous_prev_kijun = None
        self._pending_long_level = None
        self._pending_short_level = None
        self._reset_position_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        lwma = WeightedMovingAverage()
        lwma.Length = self.LwmaPeriod

        self._ichimoku = ichimoku
        self._lwma = lwma

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ichimoku, lwma, self.ProcessCandle).Start()

        # protection handled manually via SL/TP/trailing

    def ProcessCandle(self, candle, ichi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not ma_value.IsFinal:
            return

        ma_current = float(ma_value)

        if not ichi_value.IsFinal:
            self._update_history(candle, None, ma_current)
            return

        kijun = ichi_value.Kijun
        if kijun is None:
            self._update_history(candle, None, ma_current)
            return

        kijun_val = float(kijun)

        self._manage_open_position(candle, ma_current)

        if self.IsFormedAndOnlineAndAllowTrading():
            hour = candle.OpenTime.Hour
            if hour >= int(self.TradingStartHour) and hour < int(self.TradingEndHour):
                self._evaluate_entry_signals(candle, kijun_val, ma_current)

        self._update_history(candle, kijun_val, ma_current)

    def _manage_open_position(self, candle, ma_current):
        if self.Position == 0:
            if self._is_long is not None or self._entry_price is not None:
                self._reset_position_state()
            return

        is_long = self.Position > 0
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        actual_entry = self._entry_price if self._entry_price is not None else close
        if self._is_long is None or self._is_long != is_long or self._entry_price is None:
            entry_val = actual_entry if actual_entry != 0.0 else close
            self._setup_position_state(is_long, entry_val)
        elif actual_entry != 0.0 and self._entry_price != actual_entry:
            self._entry_price = actual_entry
            if self._is_long:
                self._stop_loss_price = self._entry_price - self._sl_distance if self._sl_distance > 0.0 else None
                self._take_profit_price = self._entry_price + self._tp_distance if self._tp_distance > 0.0 else None
            else:
                self._stop_loss_price = self._entry_price + self._sl_distance if self._sl_distance > 0.0 else None
                self._take_profit_price = self._entry_price - self._tp_distance if self._tp_distance > 0.0 else None

        entry = self._entry_price if self._entry_price is not None else close
        self._is_long = is_long

        if self._previous_ma is not None and self._previous_prev_ma is not None and self._stop_loss_price is not None:
            if is_long and self._stop_loss_price < entry and self._previous_ma < self._previous_prev_ma:
                self._close_position_and_reset()
                return
            if not is_long and self._stop_loss_price > entry and self._previous_ma > self._previous_prev_ma:
                self._close_position_and_reset()
                return

        if is_long:
            self._apply_be_trailing_long(candle, entry)
            if self._stop_loss_price is not None and low <= self._stop_loss_price:
                self._close_position_and_reset()
                return
            if self._take_profit_price is not None and high >= self._take_profit_price:
                self._close_position_and_reset()
                return
        else:
            self._apply_be_trailing_short(candle, entry)
            if self._stop_loss_price is not None and high >= self._stop_loss_price:
                self._close_position_and_reset()
                return
            if self._take_profit_price is not None and low <= self._take_profit_price:
                self._close_position_and_reset()
                return

    def _apply_be_trailing_long(self, candle, entry):
        close = float(candle.ClosePrice)
        if not self._be_applied and self._be_distance > 0.0:
            if close - entry >= self._be_distance:
                new_stop = entry + (self._be_step if self._be_step > 0.0 else 0.0)
                if self._stop_loss_price is None or new_stop > self._stop_loss_price:
                    self._stop_loss_price = new_stop
                self._be_applied = True

        if self._trail_distance > 0.0 and close - entry >= self._trail_distance:
            new_stop = close - self._trail_distance
            if self._stop_loss_price is None or new_stop > self._stop_loss_price:
                self._stop_loss_price = new_stop

    def _apply_be_trailing_short(self, candle, entry):
        close = float(candle.ClosePrice)
        if not self._be_applied and self._be_distance > 0.0:
            if entry - close >= self._be_distance:
                new_stop = entry - (self._be_step if self._be_step > 0.0 else 0.0)
                if self._stop_loss_price is None or new_stop < self._stop_loss_price:
                    self._stop_loss_price = new_stop
                self._be_applied = True

        if self._trail_distance > 0.0 and entry - close >= self._trail_distance:
            new_stop = close + self._trail_distance
            if self._stop_loss_price is None or new_stop < self._stop_loss_price:
                self._stop_loss_price = new_stop

    def _evaluate_entry_signals(self, candle, kijun, ma_current):
        if self._previous_close is None or self._previous_ma is None or self._previous_kijun is None:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        prev_close = self._previous_close
        prev_kijun = self._previous_kijun
        prev_ma = self._previous_ma
        filter_offset = self._convert_pips(float(self.MaFilterPips))

        kijun_not_falling = self._previous_prev_kijun is None or kijun >= self._previous_prev_kijun
        kijun_not_rising = self._previous_prev_kijun is None or kijun <= self._previous_prev_kijun

        if self._pending_long_level is None:
            price_closed_above = close > kijun
            price_opened_below = open_price < kijun
            price_was_below = prev_close < prev_kijun
            price_touched_below = low <= kijun
            if price_closed_above and (price_opened_below or price_was_below or price_touched_below) and kijun_not_falling:
                if filter_offset <= 0.0 or ma_current < kijun - filter_offset:
                    self._pending_long_level = kijun
                    self._pending_short_level = None

        if self._pending_short_level is None:
            price_closed_below = close < kijun
            price_opened_above = open_price > kijun
            price_was_above = prev_close > prev_kijun
            price_touched_above = high >= kijun
            if price_closed_below and (price_opened_above or price_was_above or price_touched_above) and kijun_not_rising:
                if filter_offset <= 0.0 or ma_current > kijun + filter_offset:
                    self._pending_short_level = kijun
                    self._pending_long_level = None

        ma_trend_up = ma_current > prev_ma
        ma_trend_down = ma_current < prev_ma

        volume = float(self.Volume) + abs(float(self.Position))
        if volume <= 0:
            return

        if self._pending_long_level is not None and ma_trend_up and self.Position <= 0:
            self.BuyMarket(volume)
            self._setup_position_state(True, close)
            self._pending_long_level = None
            self._pending_short_level = None
            return

        if self._pending_short_level is not None and ma_trend_down and self.Position >= 0:
            self.SellMarket(volume)
            self._setup_position_state(False, close)
            self._pending_long_level = None
            self._pending_short_level = None

    def _update_history(self, candle, kijun, ma_current):
        self._previous_prev_kijun = self._previous_kijun
        self._previous_kijun = kijun
        self._previous_prev_ma = self._previous_ma
        self._previous_ma = ma_current
        self._previous_close = float(candle.ClosePrice)

    def _setup_position_state(self, is_long, entry_price):
        self._is_long = is_long
        self._entry_price = entry_price
        self._be_applied = False

        self._sl_distance = self._convert_pips(float(self.StopLossPips))
        self._tp_distance = self._convert_pips(float(self.TakeProfitPips))
        self._be_distance = self._convert_pips(float(self.BreakEvenPips))
        self._be_step = self._convert_pips(1.0)
        self._trail_distance = self._convert_pips(float(self.TrailingStopPips))

        if self._sl_distance > 0.0:
            self._stop_loss_price = entry_price - self._sl_distance if is_long else entry_price + self._sl_distance
        else:
            self._stop_loss_price = None

        if self._tp_distance > 0.0:
            self._take_profit_price = entry_price + self._tp_distance if is_long else entry_price - self._tp_distance
        else:
            self._take_profit_price = None

    def _close_position_and_reset(self):
        if self.Position > 0:
            self.SellMarket(abs(float(self.Position)))
        elif self.Position < 0:
            self.BuyMarket(abs(float(self.Position)))
        self._reset_position_state()

    def _reset_position_state(self):
        self._is_long = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._sl_distance = 0.0
        self._tp_distance = 0.0
        self._be_distance = 0.0
        self._be_step = 0.0
        self._trail_distance = 0.0
        self._be_applied = False

    def _convert_pips(self, value):
        if value <= 0.0:
            return 0.0
        return value * self._get_pip_step()

    def _get_pip_step(self):
        if self.Security is None or self.Security.PriceStep is None:
            return 1.0
        step = float(self.Security.PriceStep)
        if step <= 0.0:
            return 1.0
        decimals = self._get_decimal_places(step)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def _get_decimal_places(self, value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10.0
            decimals += 1
        return decimals

    def OnReseted(self):
        super(kijun_sen_robot_strategy, self).OnReseted()
        self._previous_close = None
        self._previous_ma = None
        self._previous_prev_ma = None
        self._previous_kijun = None
        self._previous_prev_kijun = None
        self._pending_long_level = None
        self._pending_short_level = None
        self._reset_position_state()

    def CreateClone(self):
        return kijun_sen_robot_strategy()
