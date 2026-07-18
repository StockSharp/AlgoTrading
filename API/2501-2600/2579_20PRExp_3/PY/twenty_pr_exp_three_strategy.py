import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class twenty_pr_exp_three_strategy(Strategy):
    def __init__(self):
        super(twenty_pr_exp_three_strategy, self).__init__()

        self._take_profit_points = self.Param("TakeProfitPoints", 20.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 10.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 10.0)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._gap_points = self.Param("GapPoints", 100.0)
        self._session_start_hour = self.Param("SessionStartHour", 12)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._volume_candle_type = self.Param("VolumeCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._daily_high = 0.0
        self._daily_low = 0.0
        self._daily_mid = 0.0
        self._daily_range = 0.0
        self._current_day = None
        self._previous_close = 0.0
        self._has_previous_close = False
        self._current_volume_bar = 0.0
        self._previous_volume_bar = 0.0
        self._long_entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_entry_price = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @TrailingStepPoints.setter
    def TrailingStepPoints(self, value):
        self._trailing_step_points.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def GapPoints(self):
        return self._gap_points.Value

    @GapPoints.setter
    def GapPoints(self, value):
        self._gap_points.Value = value

    @property
    def SessionStartHour(self):
        return self._session_start_hour.Value

    @SessionStartHour.setter
    def SessionStartHour(self, value):
        self._session_start_hour.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def VolumeCandleType(self):
        return self._volume_candle_type.Value

    @VolumeCandleType.setter
    def VolumeCandleType(self, value):
        self._volume_candle_type.Value = value

    def _get_point_value(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        return step

    def OnStarted2(self, time):
        super(twenty_pr_exp_three_strategy, self).OnStarted2(time)

        self._daily_high = 0.0
        self._daily_low = 0.0
        self._daily_mid = 0.0
        self._daily_range = 0.0
        self._current_day = None
        self._previous_close = 0.0
        self._has_previous_close = False
        self._current_volume_bar = 0.0
        self._previous_volume_bar = 0.0
        self._reset_long_state()
        self._reset_short_state()

        sar = ParabolicSar()
        sar.Acceleration = 0.005
        sar.AccelerationMax = 0.01

        main_sub = self.SubscribeCandles(self.CandleType)
        main_sub.Bind(sar, self._process_main_candle).Start()

        volume_sub = self.SubscribeCandles(self.VolumeCandleType)
        volume_sub.Bind(self._process_volume_candle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent))

    def _process_volume_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._previous_volume_bar = self._current_volume_bar
        self._current_volume_bar = float(candle.TotalVolume)

    def _process_main_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_daily_levels(candle)

        if self.Position != 0:
            self._update_previous_close(candle)
            return

        signal = self._get_trade_signal(candle)

        if signal > 0:
            self.BuyMarket()
        elif signal < 0:
            self.SellMarket()

        self._update_previous_close(candle)

    def _update_daily_levels(self, candle):
        candle_day = candle.OpenTime.Date

        if self._current_day is None or self._current_day != candle_day:
            self._current_day = candle_day
            self._daily_high = float(candle.HighPrice)
            self._daily_low = float(candle.LowPrice)
        else:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            if high > self._daily_high:
                self._daily_high = high
            if self._daily_low == 0.0 or low < self._daily_low:
                self._daily_low = low

        self._daily_mid = (self._daily_high + self._daily_low) / 2.0
        self._daily_range = self._daily_high - self._daily_low

    def _get_trade_signal(self, candle):
        point_value = self._get_point_value()
        range_threshold = float(self.GapPoints) * point_value
        has_range = self._daily_range > 0.0 and self._daily_range > range_threshold
        close = float(candle.ClosePrice)

        if not has_range:
            return 0

        if close >= self._daily_high and self._daily_high > 0.0:
            return 1

        if close <= self._daily_low and self._daily_low > 0.0:
            return -1

        return 0

    def _update_previous_close(self, candle):
        self._previous_close = float(candle.ClosePrice)
        self._has_previous_close = True

    def _reset_long_state(self):
        self._long_entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0

    def _reset_short_state(self):
        self._short_entry_price = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    def OnReseted(self):
        super(twenty_pr_exp_three_strategy, self).OnReseted()
        self._daily_high = 0.0
        self._daily_low = 0.0
        self._daily_mid = 0.0
        self._daily_range = 0.0
        self._current_day = None
        self._previous_close = 0.0
        self._has_previous_close = False
        self._current_volume_bar = 0.0
        self._previous_volume_bar = 0.0
        self._reset_long_state()
        self._reset_short_state()

    def CreateClone(self):
        return twenty_pr_exp_three_strategy()
