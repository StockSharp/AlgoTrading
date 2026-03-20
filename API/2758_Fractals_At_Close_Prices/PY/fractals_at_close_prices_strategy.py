import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class fractals_at_close_prices_strategy(Strategy):
    def __init__(self):
        super(fractals_at_close_prices_strategy, self).__init__()

        self._start_hour = self.Param("StartHour", 0)
        self._end_hour = self.Param("EndHour", 0)
        self._stop_loss_pips = self.Param("StopLossPips", 200)
        self._take_profit_pips = self.Param("TakeProfitPips", 400)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._close_window = []
        self._last_upper_fractal = None
        self._prev_upper_fractal = None
        self._last_lower_fractal = None
        self._prev_lower_fractal = None

        self._pip_value = 0.0
        self._sl_dist = 0.0
        self._tp_dist = 0.0
        self._trail_dist = 0.0
        self._trail_step = 0.0

        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

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

    def OnStarted(self, time):
        super(fractals_at_close_prices_strategy, self).OnStarted(time)

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0

        self._pip_value = price_step
        if decimals == 3 or decimals == 5:
            self._pip_value *= 10.0

        self._sl_dist = self.StopLossPips * self._pip_value if self.StopLossPips != 0 else 0.0
        self._tp_dist = self.TakeProfitPips * self._pip_value if self.TakeProfitPips != 0 else 0.0
        self._trail_dist = self.TrailingStopPips * self._pip_value if self.TrailingStopPips != 0 else 0.0
        self._trail_step = self.TrailingStepPips * self._pip_value if self.TrailingStepPips != 0 else 0.0

        self._close_window = []
        self._last_upper_fractal = None
        self._prev_upper_fractal = None
        self._last_lower_fractal = None
        self._prev_lower_fractal = None
        self._entry_price = None
        self._reset_risk_levels()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_fractals(candle)

        if not self._is_within_trading_hours(candle.OpenTime):
            self._close_all()
            return

        self._apply_risk_management(candle)

        self._execute_entries(candle)

    def _update_fractals(self, candle):
        self._close_window.append(float(candle.ClosePrice))
        while len(self._close_window) > 5:
            self._close_window.pop(0)

        if len(self._close_window) < 5:
            return

        w = self._close_window
        center = w[2]

        is_upper = (center > w[0] and center > w[1] and
                    center >= w[3] and center >= w[4])
        if is_upper:
            self._prev_upper_fractal = self._last_upper_fractal
            self._last_upper_fractal = center

        is_lower = (center < w[0] and center < w[1] and
                    center <= w[3] and center <= w[4])
        if is_lower:
            self._prev_lower_fractal = self._last_lower_fractal
            self._last_lower_fractal = center

    def _is_within_trading_hours(self, time):
        hour = time.Hour
        if self.StartHour == self.EndHour:
            return True
        if self.StartHour < self.EndHour:
            return hour >= self.StartHour and hour < self.EndHour
        return hour >= self.StartHour or hour < self.EndHour

    def _apply_risk_management(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self._long_stop is not None and low <= self._long_stop:
                self.SellMarket()
                self._reset_risk_levels()
                return
            if self._long_take is not None and high >= self._long_take:
                self.SellMarket()
                self._reset_risk_levels()
                return
            self._update_long_trailing(candle)

        elif self.Position < 0:
            if self._short_stop is not None and high >= self._short_stop:
                self.BuyMarket()
                self._reset_risk_levels()
                return
            if self._short_take is not None and low <= self._short_take:
                self.BuyMarket()
                self._reset_risk_levels()
                return
            self._update_short_trailing(candle)

    def _update_long_trailing(self, candle):
        if self._trail_dist <= 0 or self._entry_price is None:
            return
        close = float(candle.ClosePrice)
        profit_dist = close - self._entry_price
        if profit_dist <= self._trail_dist + self._trail_step:
            return
        target_stop = close - self._trail_dist
        if self._long_stop is not None and self._long_stop >= close - (self._trail_dist + self._trail_step):
            return
        self._long_stop = target_stop

    def _update_short_trailing(self, candle):
        if self._trail_dist <= 0 or self._entry_price is None:
            return
        close = float(candle.ClosePrice)
        profit_dist = self._entry_price - close
        if profit_dist <= self._trail_dist + self._trail_step:
            return
        target_stop = close + self._trail_dist
        if self._short_stop is not None and self._short_stop <= close + (self._trail_dist + self._trail_step):
            return
        self._short_stop = target_stop

    def _execute_entries(self, candle):
        if self.Position != 0:
            return

        close = float(candle.ClosePrice)

        bullish_trend = (self._last_lower_fractal is not None and
                         self._prev_lower_fractal is not None and
                         self._prev_lower_fractal < self._last_lower_fractal)

        if bullish_trend:
            self.BuyMarket()
            self._entry_price = close
            self._long_stop = close - self._sl_dist if self._sl_dist > 0 else None
            self._long_take = close + self._tp_dist if self._tp_dist > 0 else None
            self._short_stop = None
            self._short_take = None
            return

        bearish_trend = (self._last_upper_fractal is not None and
                         self._prev_upper_fractal is not None and
                         self._prev_upper_fractal > self._last_upper_fractal)

        if bearish_trend:
            self.SellMarket()
            self._entry_price = close
            self._short_stop = close + self._sl_dist if self._sl_dist > 0 else None
            self._short_take = close - self._tp_dist if self._tp_dist > 0 else None
            self._long_stop = None
            self._long_take = None

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._reset_risk_levels()

    def _reset_risk_levels(self):
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = None

    def OnReseted(self):
        super(fractals_at_close_prices_strategy, self).OnReseted()
        self._close_window = []
        self._last_upper_fractal = None
        self._prev_upper_fractal = None
        self._last_lower_fractal = None
        self._prev_lower_fractal = None
        self._pip_value = 0.0
        self._sl_dist = 0.0
        self._tp_dist = 0.0
        self._trail_dist = 0.0
        self._trail_step = 0.0
        self._entry_price = None
        self._reset_risk_levels()

    def CreateClone(self):
        return fractals_at_close_prices_strategy()
