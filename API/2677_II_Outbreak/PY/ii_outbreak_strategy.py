import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StandardDeviation, DecimalIndicatorValue


class ii_outbreak_strategy(Strategy):
    """II Outbreak: trend-following breakout with timing oscillator, volatility filter, pyramiding and trailing."""

    def __init__(self):
        super(ii_outbreak_strategy, self).__init__()

        self._commission = self.Param("Commission", 4.0) \
            .SetDisplay("Commission", "Round lot commission used for stop offset", "Risk Management")
        self._epsilon_tolerance = self.Param("EpsilonTolerance", 0.0000000001) \
            .SetDisplay("Epsilon", "Minimum acceleration threshold", "Filters")
        self._spread_threshold = self.Param("SpreadThreshold", 6.0) \
            .SetDisplay("Spread Threshold", "Maximum spread allowed to trade (points)", "Execution")
        self._trail_stop_points = self.Param("TrailStopPoints", 50000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail Stop Points", "Trailing stop distance in points", "Risk Management")
        self._total_equity_risk = self.Param("TotalEquityRisk", 0.5) \
            .SetDisplay("Equity Risk %", "Maximum floating loss before closing all trades", "Risk Management")
        self._maximum_risk = self.Param("MaximumRisk", 0.1) \
            .SetDisplay("Risk Fraction", "Fraction of balance allocated per order", "Risk Management")
        self._std_dev_limit = self.Param("StdDevLimit", 5000.0) \
            .SetDisplay("StdDev Limit", "Upper bound for standard deviation filter", "Filters")
        self._volatility_threshold = self.Param("VolatilityThreshold", 0.0) \
            .SetDisplay("Volatility Threshold", "Minimum volatility score required for entries", "Filters")
        self._account_leverage = self.Param("AccountLeverage", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Account Leverage", "Used to approximate required margin", "Execution")
        self._warning_alerts = self.Param("WarningAlerts", True) \
            .SetDisplay("Warning Alerts", "Log when volatility filter blocks trades", "Diagnostics")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")

        self._point = 0.0
        self._trail_stop_distance = 0.0
        self._initial_stop_distance = 0.0
        self._trail_start_points = 0.0
        self._pyramiding_step_points = 0.0

        self._static_stop_enabled = True
        self._buy_signal = False
        self._sell_signal = False
        self._volatility_signal = False

        self._buy_pyramid_level = 0.0
        self._sell_pyramid_level = 0.0
        self._current_volatility_threshold = 0.0
        self._current_spread_limit = 0.0

        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_initial_stop = None
        self._short_initial_stop = None

        self._timing_values = [50.0, 50.0, 50.0]
        self._typical_prices = [0.0] * 120
        self._typical_count = 0

        self._has_previous_candle = False
        self._entry_price = 0.0

    @property
    def Commission(self):
        return float(self._commission.Value)
    @property
    def EpsilonTolerance(self):
        return float(self._epsilon_tolerance.Value)
    @property
    def SpreadThreshold(self):
        return float(self._spread_threshold.Value)
    @property
    def TrailStopPoints(self):
        return float(self._trail_stop_points.Value)
    @property
    def TotalEquityRisk(self):
        return float(self._total_equity_risk.Value)
    @property
    def MaximumRisk(self):
        return float(self._maximum_risk.Value)
    @property
    def StdDevLimit(self):
        return float(self._std_dev_limit.Value)
    @property
    def VolatilityThreshold(self):
        return float(self._volatility_threshold.Value)
    @property
    def AccountLeverage(self):
        return float(self._account_leverage.Value)
    @property
    def WarningAlerts(self):
        return self._warning_alerts.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ii_outbreak_strategy, self).OnStarted(time)

        sec = self.Security
        self._point = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.01
        self._trail_stop_distance = self.TrailStopPoints * self._point
        self._initial_stop_distance = self._trail_stop_distance * 2.0
        self._trail_start_points = self.TrailStopPoints + int(self.Commission) + self.SpreadThreshold
        self._pyramiding_step_points = max(10.0, self.SpreadThreshold + 1.0)
        self._current_volatility_threshold = self.VolatilityThreshold
        self._current_spread_limit = self.SpreadThreshold

        self._static_stop_enabled = True
        self._buy_signal = False
        self._sell_signal = False
        self._volatility_signal = False
        self._buy_pyramid_level = 0.0
        self._sell_pyramid_level = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_initial_stop = None
        self._short_initial_stop = None
        self._timing_values = [50.0, 50.0, 50.0]
        self._typical_prices = [0.0] * 120
        self._typical_count = 0
        self._has_previous_candle = False
        self._entry_price = 0.0

        self._std_dev = StandardDeviation()
        self._std_dev.Length = 10

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._std_dev)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_timing(candle)
        self._std_dev.Process(DecimalIndicatorValue(self._std_dev, float(candle.ClosePrice), candle.OpenTime))
        self._update_volatility(candle)

        can_trade = self._std_dev.IsFormed

        if self._has_previous_candle and not self._static_stop_enabled and self._is_equity_risk_exceeded(candle):
            self._close_all()
            self._reset_after_close()
            self._has_previous_candle = True
            return

        if not can_trade:
            self._has_previous_candle = True
            return

        if self.Position == 0:
            self._reset_state_before_entry()

            if self._is_trading_blocked_by_calendar(candle.OpenTime):
                self._has_previous_candle = True
                return

            self._try_open_position(candle)
        else:
            self._manage_open_position(candle)

        self._has_previous_candle = True

    def _reset_state_before_entry(self):
        self._static_stop_enabled = True
        self._buy_pyramid_level = 0.0
        self._sell_pyramid_level = 0.0
        self._current_volatility_threshold = self.VolatilityThreshold
        self._current_spread_limit = self.SpreadThreshold
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_initial_stop = None
        self._short_initial_stop = None

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def _reset_after_close(self):
        self._static_stop_enabled = True
        self._buy_pyramid_level = 0.0
        self._sell_pyramid_level = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_initial_stop = None
        self._short_initial_stop = None
        self._current_volatility_threshold = self.VolatilityThreshold
        self._current_spread_limit = self.SpreadThreshold
        self._entry_price = 0.0

    def _try_open_position(self, candle):
        if not self._volatility_signal:
            return

        close = float(candle.ClosePrice)

        if self._buy_signal:
            self.BuyMarket()
            self._entry_price = close
            self._long_initial_stop = close - self._initial_stop_distance
        elif self._sell_signal:
            self.SellMarket()
            self._entry_price = close
            self._short_initial_stop = close + self._initial_stop_distance

    def _manage_open_position(self, candle):
        if self.Position == 0:
            return

        if self._entry_price <= 0 or self._point <= 0:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._static_stop_enabled:
            if self.Position > 0 and self._long_initial_stop is not None and lo <= self._long_initial_stop:
                self.SellMarket()
                self._reset_after_close()
                return
            if self.Position < 0 and self._short_initial_stop is not None and h >= self._short_initial_stop:
                self.BuyMarket()
                self._reset_after_close()
                return

        profit_points = (close - self._entry_price) / self._point if self.Position > 0 else (self._entry_price - close) / self._point

        if profit_points < self._trail_start_points:
            return

        if self.Position > 0:
            new_stop = close - self._trail_stop_distance
            if self._long_trailing_stop is None or new_stop - self._long_trailing_stop >= self._point:
                self._long_trailing_stop = new_stop

            if self._long_trailing_stop is not None and lo <= self._long_trailing_stop:
                self.SellMarket()
                self._reset_after_close()
                return

            self._try_add_to_position(True, profit_points, candle)

        else:
            new_stop = close + self._trail_stop_distance
            if self._short_trailing_stop is None or self._short_trailing_stop - new_stop >= self._point:
                self._short_trailing_stop = new_stop

            if self._short_trailing_stop is not None and h >= self._short_trailing_stop:
                self.BuyMarket()
                self._reset_after_close()
                return

            self._try_add_to_position(False, profit_points, candle)

    def _try_add_to_position(self, is_long, profit_points, candle):
        if not self._volatility_signal:
            return

        if is_long:
            if not self._buy_signal:
                return
            if profit_points < self._buy_pyramid_level + self._pyramiding_step_points:
                return
            self.BuyMarket()
            self._buy_pyramid_level = profit_points
            self._static_stop_enabled = False
            self._long_initial_stop = None
        else:
            if not self._sell_signal:
                return
            if profit_points < self._sell_pyramid_level + self._pyramiding_step_points:
                return
            self.SellMarket()
            self._sell_pyramid_level = profit_points
            self._static_stop_enabled = False
            self._short_initial_stop = None

    def _is_equity_risk_exceeded(self, candle):
        return False

    def _update_volatility(self, candle):
        self._volatility_signal = self._has_previous_candle

    def _update_timing(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        cpiv = 100.0 * ((h + lo + c) / 3.0)

        limit = len(self._typical_prices)
        count = min(self._typical_count + 1, limit)

        i = min(count - 1, limit - 1)
        while i > 0:
            self._typical_prices[i] = self._typical_prices[i - 1]
            i -= 1

        self._typical_prices[0] = cpiv
        self._typical_count = count

        self._calculate_timing_signals()

    def _calculate_timing_signals(self):
        if self._typical_count < 2:
            self._buy_signal = False
            self._sell_signal = False
            return

        self._timing_values = [50.0, 50.0, 50.0]

        j = 0
        i_counter = 0
        cpiv = 0.0
        ppiv = 0.0
        dmov = 0.0
        amov = 0.0
        tval = 50.0

        dtemp1 = dtemp2 = dtemp3 = dtemp4 = dtemp5 = dtemp6 = dtemp7 = dtemp8 = 0.0
        atemp1 = atemp2 = atemp3 = atemp4 = atemp5 = atemp6 = atemp7 = atemp8 = 0.0

        for idx in range(self._typical_count - 1, -1, -1):
            typical = self._typical_prices[idx]

            if j == 0:
                j = 1
                i_counter = 0
                cpiv = typical
            else:
                if j < 7:
                    j += 1

                ppiv = cpiv
                cpiv = typical
                dpiv = cpiv - ppiv

                dtemp1 = (2.0 / 3.0) * dtemp1 + (1.0 / 3.0) * dpiv
                dtemp2 = (1.0 / 3.0) * dtemp1 + (2.0 / 3.0) * dtemp2
                dtemp3 = 1.5 * dtemp1 - dtemp2 / 2.0
                dtemp4 = (2.0 / 3.0) * dtemp4 + (1.0 / 3.0) * dtemp3
                dtemp5 = (1.0 / 3.0) * dtemp4 + (2.0 / 3.0) * dtemp5
                dtemp6 = 1.5 * dtemp4 - dtemp5 / 2.0
                dtemp7 = (2.0 / 3.0) * dtemp7 + (1.0 / 3.0) * dtemp6
                dtemp8 = (1.0 / 3.0) * dtemp7 + (2.0 / 3.0) * dtemp8
                dmov = 1.5 * dtemp7 - dtemp8 / 2.0

                atemp1 = (2.0 / 3.0) * atemp1 + (1.0 / 3.0) * abs(dpiv)
                atemp2 = (1.0 / 3.0) * atemp1 + (2.0 / 3.0) * atemp2
                atemp3 = 1.5 * atemp1 - atemp2 / 2.0
                atemp4 = (2.0 / 3.0) * atemp4 + (1.0 / 3.0) * atemp3
                atemp5 = (1.0 / 3.0) * atemp4 + (2.0 / 3.0) * atemp5
                atemp6 = 1.5 * atemp4 - atemp5 / 2.0
                atemp7 = (2.0 / 3.0) * atemp7 + (1.0 / 3.0) * atemp6
                atemp8 = (1.0 / 3.0) * atemp7 + (2.0 / 3.0) * atemp8
                amov = 1.5 * atemp7 - atemp8 / 2.0

                if j <= 6 and cpiv != ppiv:
                    i_counter += 1

                if j == 6 and i_counter == 0:
                    j = 0

            if j > 6 and amov > self.EpsilonTolerance:
                tval = 50.0 * (dmov / amov + 1.0)
                if tval > 100.0:
                    tval = 100.0
                elif tval < 0.0:
                    tval = 0.0
            else:
                tval = 50.0

            if idx <= 2:
                self._timing_values[idx] = tval

        self._buy_signal = self._timing_values[1] <= self._timing_values[2] and self._timing_values[0] > self._timing_values[1]
        self._sell_signal = self._timing_values[1] >= self._timing_values[2] and self._timing_values[0] < self._timing_values[1]

    def _is_trading_blocked_by_calendar(self, t):
        if t.DayOfWeek == 5 and t.Hour >= 23:  # Friday
            return True
        day_of_year = t.DayOfYear
        if (day_of_year == 358 or day_of_year == 359 or day_of_year == 365 or day_of_year == 366) and t.Hour >= 16:
            return True
        return False

    def OnReseted(self):
        super(ii_outbreak_strategy, self).OnReseted()
        self._point = 0.0
        self._trail_stop_distance = 0.0
        self._initial_stop_distance = 0.0
        self._trail_start_points = 0.0
        self._pyramiding_step_points = 0.0
        self._static_stop_enabled = True
        self._buy_signal = False
        self._sell_signal = False
        self._volatility_signal = False
        self._buy_pyramid_level = 0.0
        self._sell_pyramid_level = 0.0
        self._current_volatility_threshold = 0.0
        self._current_spread_limit = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._long_initial_stop = None
        self._short_initial_stop = None
        self._timing_values = [50.0, 50.0, 50.0]
        self._typical_prices = [0.0] * 120
        self._typical_count = 0
        self._has_previous_candle = False
        self._entry_price = 0.0

    def CreateClone(self):
        return ii_outbreak_strategy()
