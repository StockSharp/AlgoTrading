import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ivan_cci_averaging_strategy(Strategy):
    def __init__(self):
        super(ivan_cci_averaging_strategy, self).__init__()

        self._use_averaging = self.Param("UseAveraging", False)
        self._stop_loss_ma_period = self.Param("StopLossMaPeriod", 36)
        self._risk_percent = self.Param("RiskPercent", 10.0)
        self._use_zero_bar = self.Param("UseZeroBar", False)
        self._reverse_level = self.Param("ReverseLevel", 100.0)
        self._global_signal_level = self.Param("GlobalSignalLevel", 100.0)
        self._min_stop_distance = self.Param("MinStopDistance", 0.005)
        self._trailing_step = self.Param("TrailingStep", 0.001)
        self._break_even_distance = self.Param("BreakEvenDistance", 0.0005)
        self._profit_protection_factor = self.Param("ProfitProtectionFactor", 1.5)
        self._minimum_volume = self.Param("MinimumVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._last_cci100 = None
        self._prev_cci100 = None
        self._last_cci13 = None
        self._prev_cci13 = None
        self._global_buy_signal = False
        self._global_sell_signal = False
        self._close_all = False
        self._initial_balance = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_breakeven_activated = False
        self._short_breakeven_activated = False
        self._has_long_entry = False
        self._has_short_entry = False

    @property
    def UseAveraging(self):
        return self._use_averaging.Value

    @UseAveraging.setter
    def UseAveraging(self, value):
        self._use_averaging.Value = value

    @property
    def StopLossMaPeriod(self):
        return self._stop_loss_ma_period.Value

    @StopLossMaPeriod.setter
    def StopLossMaPeriod(self, value):
        self._stop_loss_ma_period.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def UseZeroBar(self):
        return self._use_zero_bar.Value

    @UseZeroBar.setter
    def UseZeroBar(self, value):
        self._use_zero_bar.Value = value

    @property
    def ReverseLevel(self):
        return self._reverse_level.Value

    @ReverseLevel.setter
    def ReverseLevel(self, value):
        self._reverse_level.Value = value

    @property
    def GlobalSignalLevel(self):
        return self._global_signal_level.Value

    @GlobalSignalLevel.setter
    def GlobalSignalLevel(self, value):
        self._global_signal_level.Value = value

    @property
    def MinStopDistance(self):
        return self._min_stop_distance.Value

    @MinStopDistance.setter
    def MinStopDistance(self, value):
        self._min_stop_distance.Value = value

    @property
    def TrailingStep(self):
        return self._trailing_step.Value

    @TrailingStep.setter
    def TrailingStep(self, value):
        self._trailing_step.Value = value

    @property
    def BreakEvenDistance(self):
        return self._break_even_distance.Value

    @BreakEvenDistance.setter
    def BreakEvenDistance(self, value):
        self._break_even_distance.Value = value

    @property
    def ProfitProtectionFactor(self):
        return self._profit_protection_factor.Value

    @ProfitProtectionFactor.setter
    def ProfitProtectionFactor(self, value):
        self._profit_protection_factor.Value = value

    @property
    def MinimumVolume(self):
        return self._minimum_volume.Value

    @MinimumVolume.setter
    def MinimumVolume(self, value):
        self._minimum_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(ivan_cci_averaging_strategy, self).OnStarted2(time)

        self._cci100 = CommodityChannelIndex()
        self._cci100.Length = 100
        self._cci13 = CommodityChannelIndex()
        self._cci13.Length = 13
        self._stop_ma = SmoothedMovingAverage()
        self._stop_ma.Length = self.StopLossMaPeriod

        self._last_cci100 = None
        self._prev_cci100 = None
        self._last_cci13 = None
        self._prev_cci13 = None
        self._global_buy_signal = False
        self._global_sell_signal = False
        self._close_all = False
        self._has_long_entry = False
        self._has_short_entry = False
        self._long_breakeven_activated = False
        self._short_breakeven_activated = False
        self._long_stop = 0.0
        self._short_stop = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._cci100, self._cci13, self._stop_ma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, cci100_value, cci13_value, stop_ma_value):
        if candle.State != CandleStates.Finished:
            return

        cci100 = float(cci100_value)
        cci13 = float(cci13_value)
        stop_ma = float(stop_ma_value)
        close = float(candle.ClosePrice)

        if not self._cci100.IsFormed or not self._cci13.IsFormed or not self._stop_ma.IsFormed:
            self._update_history(cci100, cci13)
            return

        if self.UseZeroBar:
            current_cci = cci100
            previous_cci = self._last_cci100
            short_cci = cci13
        else:
            current_cci = self._last_cci100
            previous_cci = self._prev_cci100
            short_cci = self._last_cci13

        if current_cci is None or previous_cci is None or short_cci is None:
            self._update_history(cci100, cci13)
            return

        reverse_level = float(self.ReverseLevel)
        global_level = float(self.GlobalSignalLevel)

        if (previous_cci > reverse_level and current_cci < reverse_level) or \
           (previous_cci < -reverse_level and current_cci > -reverse_level):
            self._global_buy_signal = False
            self._global_sell_signal = False
            self._close_all = True
        elif not self._close_all:
            if current_cci > global_level and not self._global_buy_signal:
                self._global_buy_signal = True
                self._global_sell_signal = False
                self._try_enter_long(candle, stop_ma)
            elif current_cci < -global_level and not self._global_sell_signal:
                self._global_buy_signal = False
                self._global_sell_signal = True
                self._try_enter_short(candle, stop_ma)
            elif self.UseAveraging:
                if self._global_buy_signal and short_cci < -global_level:
                    self._try_enter_long(candle, stop_ma)
                elif self._global_sell_signal and short_cci > global_level:
                    self._try_enter_short(candle, stop_ma)

        self._manage_positions(candle, stop_ma)

        if self._close_all:
            self._close_position()
            self._close_all = False

        self._update_history(cci100, cci13)

    def _manage_positions(self, candle, stop_ma_value):
        close = float(candle.ClosePrice)

        if self.Position > 0 and self._has_long_entry:
            be = float(self.BreakEvenDistance)
            if be > 0.0 and not self._long_breakeven_activated and close >= self._long_entry_price + be:
                self._long_stop = self._long_entry_price
                self._long_breakeven_activated = True

            trailing_step = float(self.TrailingStep)
            if stop_ma_value < close:
                if stop_ma_value - trailing_step > self._long_stop:
                    self._long_stop = stop_ma_value

            if self._long_stop > 0.0 and close <= self._long_stop:
                self.SellMarket()
                self._has_long_entry = False
                self._long_breakeven_activated = False

        elif self.Position < 0 and self._has_short_entry:
            be = float(self.BreakEvenDistance)
            if be > 0.0 and not self._short_breakeven_activated and close <= self._short_entry_price - be:
                self._short_stop = self._short_entry_price
                self._short_breakeven_activated = True

            trailing_step = float(self.TrailingStep)
            if stop_ma_value > close:
                if self._short_stop == 0.0 or stop_ma_value + trailing_step < self._short_stop:
                    self._short_stop = stop_ma_value

            if self._short_stop > 0.0 and close >= self._short_stop:
                self.BuyMarket()
                self._has_short_entry = False
                self._short_breakeven_activated = False

    def _try_enter_long(self, candle, stop_ma_value):
        close = float(candle.ClosePrice)
        if stop_ma_value >= close:
            return

        distance = close - stop_ma_value
        if distance < float(self.MinStopDistance):
            return

        self.BuyMarket()
        self._long_entry_price = close
        self._long_stop = stop_ma_value
        self._long_breakeven_activated = False
        self._has_long_entry = True
        self._has_short_entry = False

    def _try_enter_short(self, candle, stop_ma_value):
        close = float(candle.ClosePrice)
        if stop_ma_value <= close:
            return

        distance = stop_ma_value - close
        if distance < float(self.MinStopDistance):
            return

        self.SellMarket()
        self._short_entry_price = close
        self._short_stop = stop_ma_value
        self._short_breakeven_activated = False
        self._has_short_entry = True
        self._has_long_entry = False

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._has_long_entry = False
        self._has_short_entry = False
        self._long_breakeven_activated = False
        self._short_breakeven_activated = False
        self._long_stop = 0.0
        self._short_stop = 0.0

    def _update_history(self, cci100, cci13):
        self._prev_cci100 = self._last_cci100
        self._last_cci100 = cci100
        self._prev_cci13 = self._last_cci13
        self._last_cci13 = cci13

    def OnReseted(self):
        super(ivan_cci_averaging_strategy, self).OnReseted()
        self._last_cci100 = None
        self._prev_cci100 = None
        self._last_cci13 = None
        self._prev_cci13 = None
        self._global_buy_signal = False
        self._global_sell_signal = False
        self._close_all = False
        self._initial_balance = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._long_breakeven_activated = False
        self._short_breakeven_activated = False
        self._has_long_entry = False
        self._has_short_entry = False

    def CreateClone(self):
        return ivan_cci_averaging_strategy()
