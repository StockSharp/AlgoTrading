import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from collections import deque


class total_power_indicator_x_strategy(Strategy):
    """Total Power Indicator strategy: bull/bear strength with EMA-based crossover."""

    def __init__(self):
        super(total_power_indicator_x_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._power_period = self.Param("PowerPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Power Period", "EMA length used by Total Power", "Indicator")

        self._lookback_period = self.Param("LookbackPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Samples counted for bull/bear strength", "Indicator")

        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow buying when bulls dominate", "Trading")

        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow selling when bears dominate", "Trading")

        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Close longs on bearish crossover", "Trading")

        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Close shorts on bullish crossover", "Trading")

        self._use_trading_hours = self.Param("UseTradingHours", True) \
            .SetDisplay("Use Trading Hours", "Restrict trading to session window", "Schedule")

        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Session start hour", "Schedule")

        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Session start minute", "Schedule")

        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Session end hour", "Schedule")

        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Session end minute", "Schedule")

        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk")

        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk")

        self._prev_diff = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._bull_history = deque()
        self._bear_history = deque()
        self._bull_count = 0
        self._bear_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def PowerPeriod(self):
        return self._power_period.Value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value

    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value

    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value

    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value

    @property
    def UseTradingHours(self):
        return self._use_trading_hours.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def StartMinute(self):
        return self._start_minute.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def EndMinute(self):
        return self._end_minute.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(total_power_indicator_x_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = max(1, self.PowerPeriod)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        ema_out = self._ema.Process(DecimalIndicatorValue(self._ema, candle.ClosePrice, candle.CloseTime))
        if not self._ema.IsFormed:
            return

        ema_val = float(ema_out)

        bull_contrib = 1 if high > ema_val else 0
        bear_contrib = 1 if low < ema_val else 0

        self._update_counter(self._bull_history, bull_contrib, True)
        self._update_counter(self._bear_history, bear_contrib, False)

        lb = self.LookbackPeriod
        if len(self._bull_history) < lb or len(self._bear_history) < lb:
            return

        bull_pct = self._bull_count * 100.0 / lb
        bear_pct = self._bear_count * 100.0 / lb

        bulls = max(0.0, min(100.0, (bull_pct - 50.0) * 2.0))
        bears = max(0.0, min(100.0, (bear_pct - 50.0) * 2.0))

        diff = bulls - bears
        prev = self._prev_diff if self._prev_diff is not None else diff
        self._prev_diff = diff

        if self._handle_stops(candle):
            return

        cross_up = diff > 0 and prev <= 0
        cross_down = diff < 0 and prev >= 0

        in_window = (not self.UseTradingHours) or self._in_trading_window(candle.OpenTime)

        if self.UseTradingHours and not in_window:
            self._close_all()
            return

        if self.EnableLongExit and cross_down and self.Position > 0:
            self.SellMarket()
            self._reset_long_targets()

        if self.EnableShortExit and cross_up and self.Position < 0:
            self.BuyMarket()
            self._reset_short_targets()

        if not in_window:
            return

        if self.EnableLongEntry and cross_up and self.Position == 0:
            self.BuyMarket()
            self._setup_long(close)
        elif self.EnableShortEntry and cross_down and self.Position == 0:
            self.SellMarket()
            self._setup_short(close)

    def _update_counter(self, history, value, is_bull):
        history.append(value)
        if is_bull:
            self._bull_count += value
        else:
            self._bear_count += value

        lb = self.LookbackPeriod
        while len(history) > lb:
            removed = history.popleft()
            if is_bull:
                self._bull_count -= removed
            else:
                self._bear_count -= removed

    def _handle_stops(self, candle):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return False

        if self.Position > 0:
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket()
                self._reset_long_targets()
                return True
            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket()
                self._reset_long_targets()
                return True
        elif self.Position < 0:
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket()
                self._reset_short_targets()
                return True
            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket()
                self._reset_short_targets()
                return True
        return False

    def _in_trading_window(self, time):
        start = TimeSpan(self.StartHour, self.StartMinute, 0)
        end = TimeSpan(self.EndHour, self.EndMinute, 0)
        current = time.TimeOfDay
        if start == end:
            return current >= start and current < end
        if start < end:
            return current >= start and current < end
        return current >= start or current < end

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
            self._reset_long_targets()
        elif self.Position < 0:
            self.BuyMarket()
            self._reset_short_targets()

    def _setup_long(self, entry):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if step <= 0:
            self._reset_long_targets()
            return
        self._long_stop = entry - self.StopLossPoints * step if self.StopLossPoints > 0 else None
        self._long_take = entry + self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else None
        self._reset_short_targets()

    def _setup_short(self, entry):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if step <= 0:
            self._reset_short_targets()
            return
        self._short_stop = entry + self.StopLossPoints * step if self.StopLossPoints > 0 else None
        self._short_take = entry - self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else None
        self._reset_long_targets()

    def _reset_long_targets(self):
        self._long_stop = None
        self._long_take = None

    def _reset_short_targets(self):
        self._short_stop = None
        self._short_take = None

    def OnReseted(self):
        super(total_power_indicator_x_strategy, self).OnReseted()
        self._prev_diff = None
        self._reset_long_targets()
        self._reset_short_targets()
        self._bull_history.clear()
        self._bear_history.clear()
        self._bull_count = 0
        self._bear_count = 0

    def CreateClone(self):
        return total_power_indicator_x_strategy()
