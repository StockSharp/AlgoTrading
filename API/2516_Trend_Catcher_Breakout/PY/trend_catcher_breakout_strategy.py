import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class trend_catcher_breakout_strategy(Strategy):
    def __init__(self):
        super(trend_catcher_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._close_on_opposite_signal = self.Param("CloseOnOppositeSignal", True)
        self._reverse_signals = self.Param("ReverseSignals", False)
        self._slow_ma_period = self.Param("SlowMaPeriod", 200)
        self._fast_ma_period = self.Param("FastMaPeriod", 50)
        self._fast_filter_period = self.Param("FastFilterPeriod", 25)
        self._sar_step = self.Param("SarStep", 0.004)
        self._sar_max = self.Param("SarMax", 0.2)
        self._auto_stop_loss = self.Param("AutoStopLoss", True)
        self._auto_take_profit = self.Param("AutoTakeProfit", True)
        self._min_stop_loss = self.Param("MinStopLoss", 0.001)
        self._max_stop_loss = self.Param("MaxStopLoss", 0.2)
        self._stop_loss_coefficient = self.Param("StopLossCoefficient", 1.0)
        self._take_profit_coefficient = self.Param("TakeProfitCoefficient", 1.0)
        self._manual_stop_loss = self.Param("ManualStopLoss", 0.002)
        self._manual_take_profit = self.Param("ManualTakeProfit", 0.02)
        self._breakeven_trigger = self.Param("BreakevenTrigger", 0.005)
        self._breakeven_offset = self.Param("BreakevenOffset", 0.0001)
        self._trailing_trigger = self.Param("TrailingTrigger", 0.005)
        self._trailing_step = self.Param("TrailingStep", 0.001)

        self._trade_monday = self.Param("TradeMonday", True)
        self._trade_tuesday = self.Param("TradeTuesday", True)
        self._trade_wednesday = self.Param("TradeWednesday", True)
        self._trade_thursday = self.Param("TradeThursday", True)
        self._trade_friday = self.Param("TradeFriday", True)
        self._risk_percent = self.Param("RiskPercent", 2.0)
        self._use_martingale = self.Param("UseMartingale", True)
        self._martingale_multiplier = self.Param("MartingaleMultiplier", 2.0)

        self._previous_close = 0.0
        self._previous_sar = None
        self._entry_price = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._last_trade_was_loss = False
        self._last_exit_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CloseOnOppositeSignal(self):
        return self._close_on_opposite_signal.Value

    @CloseOnOppositeSignal.setter
    def CloseOnOppositeSignal(self, value):
        self._close_on_opposite_signal.Value = value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    @ReverseSignals.setter
    def ReverseSignals(self, value):
        self._reverse_signals.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def FastFilterPeriod(self):
        return self._fast_filter_period.Value

    @FastFilterPeriod.setter
    def FastFilterPeriod(self, value):
        self._fast_filter_period.Value = value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @SarStep.setter
    def SarStep(self, value):
        self._sar_step.Value = value

    @property
    def SarMax(self):
        return self._sar_max.Value

    @SarMax.setter
    def SarMax(self, value):
        self._sar_max.Value = value

    @property
    def AutoStopLoss(self):
        return self._auto_stop_loss.Value

    @AutoStopLoss.setter
    def AutoStopLoss(self, value):
        self._auto_stop_loss.Value = value

    @property
    def AutoTakeProfit(self):
        return self._auto_take_profit.Value

    @AutoTakeProfit.setter
    def AutoTakeProfit(self, value):
        self._auto_take_profit.Value = value

    @property
    def MinStopLoss(self):
        return self._min_stop_loss.Value

    @MinStopLoss.setter
    def MinStopLoss(self, value):
        self._min_stop_loss.Value = value

    @property
    def MaxStopLoss(self):
        return self._max_stop_loss.Value

    @MaxStopLoss.setter
    def MaxStopLoss(self, value):
        self._max_stop_loss.Value = value

    @property
    def StopLossCoefficient(self):
        return self._stop_loss_coefficient.Value

    @StopLossCoefficient.setter
    def StopLossCoefficient(self, value):
        self._stop_loss_coefficient.Value = value

    @property
    def TakeProfitCoefficient(self):
        return self._take_profit_coefficient.Value

    @TakeProfitCoefficient.setter
    def TakeProfitCoefficient(self, value):
        self._take_profit_coefficient.Value = value

    @property
    def ManualStopLoss(self):
        return self._manual_stop_loss.Value

    @ManualStopLoss.setter
    def ManualStopLoss(self, value):
        self._manual_stop_loss.Value = value

    @property
    def ManualTakeProfit(self):
        return self._manual_take_profit.Value

    @ManualTakeProfit.setter
    def ManualTakeProfit(self, value):
        self._manual_take_profit.Value = value

    @property
    def BreakevenTrigger(self):
        return self._breakeven_trigger.Value

    @BreakevenTrigger.setter
    def BreakevenTrigger(self, value):
        self._breakeven_trigger.Value = value

    @property
    def BreakevenOffset(self):
        return self._breakeven_offset.Value

    @BreakevenOffset.setter
    def BreakevenOffset(self, value):
        self._breakeven_offset.Value = value

    @property
    def TrailingTrigger(self):
        return self._trailing_trigger.Value

    @TrailingTrigger.setter
    def TrailingTrigger(self, value):
        self._trailing_trigger.Value = value

    @property
    def TrailingStep(self):
        return self._trailing_step.Value

    @TrailingStep.setter
    def TrailingStep(self, value):
        self._trailing_step.Value = value

    def OnStarted2(self, time):
        super(trend_catcher_breakout_strategy, self).OnStarted2(time)

        self._previous_close = 0.0
        self._previous_sar = None
        self._entry_price = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._last_trade_was_loss = False
        self._last_exit_time = None

        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowMaPeriod
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        fast_filter_ma = ExponentialMovingAverage()
        fast_filter_ma.Length = self.FastFilterPeriod
        sar = ParabolicSar()
        sar.Acceleration = float(self.SarStep)
        sar.AccelerationStep = float(self.SarStep)
        sar.AccelerationMax = float(self.SarMax)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(slow_ma, fast_ma, fast_filter_ma, sar, self.ProcessCandle).Start()

    def _is_trading_day(self, day_of_week):
        from System import DayOfWeek
        if day_of_week == DayOfWeek.Monday:
            return bool(self._trade_monday.Value)
        if day_of_week == DayOfWeek.Tuesday:
            return bool(self._trade_tuesday.Value)
        if day_of_week == DayOfWeek.Wednesday:
            return bool(self._trade_wednesday.Value)
        if day_of_week == DayOfWeek.Thursday:
            return bool(self._trade_thursday.Value)
        if day_of_week == DayOfWeek.Friday:
            return bool(self._trade_friday.Value)
        return False

    def ProcessCandle(self, candle, slow_value, fast_value, fast_filter_value, sar_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        slow_val = float(slow_value)
        fast_val = float(fast_value)
        fast_filter_val = float(fast_filter_value)
        sar_val = float(sar_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        exit_triggered = self._manage_active_position(candle)
        if exit_triggered:
            self._previous_close = close
            self._previous_sar = sar_val
            return

        if not self._is_trading_day(candle.OpenTime.DayOfWeek):
            self._previous_close = close
            self._previous_sar = sar_val
            return

        long_signal = False
        short_signal = False

        if self._previous_sar is not None and self._previous_close != 0.0:
            long_signal = (close > sar_val and
                           self._previous_close < self._previous_sar and
                           fast_val > slow_val and
                           close > fast_filter_val)

            short_signal = (close < sar_val and
                            self._previous_close > self._previous_sar and
                            fast_val < slow_val and
                            close < fast_filter_val)

        if self.ReverseSignals:
            long_signal, short_signal = short_signal, long_signal

        if self.CloseOnOppositeSignal:
            if long_signal and self.Position < 0:
                self.BuyMarket()
                self._finalize_trade(close, candle.OpenTime, True)
            elif short_signal and self.Position > 0:
                self.SellMarket()
                self._finalize_trade(close, candle.OpenTime, False)

        can_open = (self.Position == 0 and
                    (self._last_exit_time is None or self._last_exit_time < candle.OpenTime))

        if can_open and long_signal:
            self._try_open_long(candle, sar_val, close)
        elif can_open and short_signal:
            self._try_open_short(candle, sar_val, close)

        self._previous_close = close
        self._previous_sar = sar_val

    def _manage_active_position(self, candle):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        be_trigger = float(self.BreakevenTrigger)
        be_offset = float(self.BreakevenOffset)
        trail_trigger = float(self.TrailingTrigger)
        trail_step = float(self.TrailingStep)

        if self.Position > 0 and self._entry_price is not None:
            exit_price = 0.0
            if self._stop_loss_price > 0.0 and low <= self._stop_loss_price:
                exit_price = self._stop_loss_price
            elif self._take_profit_price > 0.0 and high >= self._take_profit_price:
                exit_price = self._take_profit_price

            if exit_price > 0.0:
                self.SellMarket()
                self._finalize_trade(exit_price, candle.OpenTime, False)
                return True

            profit = close - self._entry_price
            if profit >= be_trigger:
                breakeven = self._entry_price + be_offset
                if self._stop_loss_price < breakeven:
                    self._stop_loss_price = breakeven
            if profit >= trail_trigger:
                new_stop = close - trail_step
                if self._stop_loss_price < new_stop:
                    self._stop_loss_price = new_stop

        elif self.Position < 0 and self._entry_price is not None:
            exit_price = 0.0
            if self._stop_loss_price > 0.0 and high >= self._stop_loss_price:
                exit_price = self._stop_loss_price
            elif self._take_profit_price > 0.0 and low <= self._take_profit_price:
                exit_price = self._take_profit_price

            if exit_price > 0.0:
                self.BuyMarket()
                self._finalize_trade(exit_price, candle.OpenTime, True)
                return True

            profit = self._entry_price - close
            if profit >= be_trigger:
                breakeven = self._entry_price - be_offset
                if self._stop_loss_price == 0.0 or self._stop_loss_price > breakeven:
                    self._stop_loss_price = breakeven
            if profit >= trail_trigger:
                new_stop = close + trail_step
                if self._stop_loss_price == 0.0 or self._stop_loss_price > new_stop:
                    self._stop_loss_price = new_stop

        return False

    def _try_open_long(self, candle, sar_val, close):
        stops = self._calculate_stops(close, sar_val, True)
        if stops is None:
            return

        stop_price, take_price = stops

        self.BuyMarket()
        self._entry_price = close
        self._stop_loss_price = stop_price
        self._take_profit_price = take_price

    def _try_open_short(self, candle, sar_val, close):
        stops = self._calculate_stops(close, sar_val, False)
        if stops is None:
            return

        stop_price, take_price = stops

        self.SellMarket()
        self._entry_price = close
        self._stop_loss_price = stop_price
        self._take_profit_price = take_price

    def _calculate_stops(self, entry_price, sar, is_long):
        sl_coeff = float(self.StopLossCoefficient)
        tp_coeff = float(self.TakeProfitCoefficient)
        min_sl = float(self.MinStopLoss)
        max_sl = float(self.MaxStopLoss)

        if self.AutoStopLoss:
            if sar == 0.0:
                return None
            distance = abs(entry_price - sar) * sl_coeff
        else:
            distance = float(self.ManualStopLoss)

        if distance <= 0.0:
            return None

        min_val = min(min_sl, max_sl)
        max_val = max(min_sl, max_sl)
        if distance < min_val:
            distance = min_val
        if distance > max_val:
            distance = max_val

        if is_long:
            stop_price = entry_price - distance
        else:
            stop_price = entry_price + distance

        if self.AutoTakeProfit:
            target_distance = distance * tp_coeff
        else:
            target_distance = float(self.ManualTakeProfit)

        take_price = 0.0
        if target_distance > 0.0:
            if is_long:
                take_price = entry_price + target_distance
            else:
                take_price = entry_price - target_distance

        return (stop_price, take_price)

    def _finalize_trade(self, exit_price, time, was_short):
        if self._entry_price is not None:
            if not was_short:
                self._last_trade_was_loss = exit_price <= self._entry_price
            else:
                self._last_trade_was_loss = exit_price >= self._entry_price
        else:
            self._last_trade_was_loss = False

        self._entry_price = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._last_exit_time = time

    def OnReseted(self):
        super(trend_catcher_breakout_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._previous_sar = None
        self._entry_price = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._last_trade_was_loss = False
        self._last_exit_time = None

    def CreateClone(self):
        return trend_catcher_breakout_strategy()
