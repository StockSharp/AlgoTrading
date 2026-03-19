import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from collections import deque
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class farhad_crab1_strategy(Strategy):
    """
    FarhadCrab1: Trend-following on EMA pullbacks with trailing stop,
    daily EMA crossover filter for exits, and pip-based SL/TP.
    """

    def __init__(self):
        super(farhad_crab1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Execution timeframe", "General")
        self._ma_length = self.Param("MaLength", 15) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._ma_shift = self.Param("MaShift", 0) \
            .SetDisplay("EMA Shift", "Shift EMA backwards by N candles", "Indicators")
        self._daily_ma_length = self.Param("DailyMaLength", 15) \
            .SetDisplay("Daily EMA Length", "EMA period on daily candles", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Protection")
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Protection")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Protection")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Extra gain before updating trailing stop", "Protection")

        self._daily_candle_type = DataType.TimeFrame(TimeSpan.FromHours(1))

        self._ma_values = deque()
        self._stop_loss_price = None
        self._take_profit_price = None
        self._prev_daily_close = None
        self._prev_daily_ma = None
        self._prev_prev_daily_close = None
        self._prev_prev_daily_ma = None
        self._previous_candle = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(farhad_crab1_strategy, self).OnReseted()
        self._ma_values.clear()
        self._stop_loss_price = None
        self._take_profit_price = None
        self._prev_daily_close = None
        self._prev_daily_ma = None
        self._prev_prev_daily_close = None
        self._prev_prev_daily_ma = None
        self._previous_candle = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(farhad_crab1_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_working_candle).Start()

        daily_ema = ExponentialMovingAverage()
        daily_ema.Length = self._daily_ma_length.Value
        daily_sub = self.SubscribeCandles(self._daily_candle_type)
        daily_sub.Bind(daily_ema, self._process_daily_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_daily_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        self._prev_prev_daily_close = self._prev_daily_close
        self._prev_prev_daily_ma = self._prev_daily_ma
        self._prev_daily_close = float(candle.ClosePrice)
        self._prev_daily_ma = float(ema_val)

    def _process_working_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_val)
        self._update_ma_buffer(ema_val)
        shifted_ma = self._get_shifted_ma()

        if shifted_ma is None:
            self._previous_candle = candle
            return

        if self._previous_candle is None:
            self._previous_candle = candle
            return

        if self._try_close_by_daily_filter():
            self._previous_candle = candle
            return

        pip_size = self._get_pip_size()
        close = float(candle.ClosePrice)

        if self._check_stops(candle):
            self._previous_candle = candle
            return

        self._apply_trailing(candle, pip_size)

        prev_low = float(self._previous_candle.LowPrice)
        prev_high = float(self._previous_candle.HighPrice)

        if self.Position <= 0 and prev_low > shifted_ma:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._set_risk_levels(close, pip_size, True)
            self._previous_candle = candle
            return

        if self.Position >= 0 and prev_high < shifted_ma:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._set_risk_levels(close, pip_size, False)
            self._previous_candle = candle
            return

        self._previous_candle = candle

    def _try_close_by_daily_filter(self):
        if (self._prev_daily_close is None or self._prev_daily_ma is None or
                self._prev_prev_daily_close is None or self._prev_prev_daily_ma is None):
            return False

        if (self.Position > 0 and
                self._prev_daily_ma > self._prev_daily_close and
                self._prev_prev_daily_ma < self._prev_prev_daily_close):
            self.SellMarket()
            self._reset_risk()
            return True

        if (self.Position < 0 and
                self._prev_daily_ma < self._prev_daily_close and
                self._prev_prev_daily_ma > self._prev_prev_daily_close):
            self.BuyMarket()
            self._reset_risk()
            return True

        return False

    def _check_stops(self, candle):
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        if self.Position > 0:
            if self._stop_loss_price is not None and low <= self._stop_loss_price:
                self.SellMarket()
                self._reset_risk()
                return True
            if self._take_profit_price is not None and high >= self._take_profit_price:
                self.SellMarket()
                self._reset_risk()
                return True
        elif self.Position < 0:
            if self._stop_loss_price is not None and high >= self._stop_loss_price:
                self.BuyMarket()
                self._reset_risk()
                return True
            if self._take_profit_price is not None and low <= self._take_profit_price:
                self.BuyMarket()
                self._reset_risk()
                return True
        elif self._stop_loss_price is not None or self._take_profit_price is not None:
            self._reset_risk()

        return False

    def _apply_trailing(self, candle, pip_size):
        ts_pips = float(self._trailing_stop_pips.Value)
        if ts_pips <= 0:
            return

        step_pips = float(self._trailing_step_pips.Value)
        threshold = (ts_pips + step_pips) * pip_size
        close = float(candle.ClosePrice)

        if self.Position > 0:
            profit = close - self._entry_price
            if profit > threshold:
                candidate = close - ts_pips * pip_size
                min_stop = close - threshold
                if self._stop_loss_price is None or self._stop_loss_price < min_stop:
                    self._stop_loss_price = candidate
        elif self.Position < 0:
            profit = self._entry_price - close
            if profit > threshold:
                candidate = close + ts_pips * pip_size
                max_stop = close + threshold
                if self._stop_loss_price is None or self._stop_loss_price > max_stop:
                    self._stop_loss_price = candidate

    def _set_risk_levels(self, price, pip_size, is_long):
        self._entry_price = price
        sl_pips = float(self._stop_loss_pips.Value)
        tp_pips = float(self._take_profit_pips.Value)

        if sl_pips > 0 and pip_size > 0:
            self._stop_loss_price = price - sl_pips * pip_size if is_long else price + sl_pips * pip_size
        else:
            self._stop_loss_price = None

        if tp_pips > 0 and pip_size > 0:
            self._take_profit_price = price + tp_pips * pip_size if is_long else price - tp_pips * pip_size
        else:
            self._take_profit_price = None

    def _reset_risk(self):
        self._stop_loss_price = None
        self._take_profit_price = None

    def _get_pip_size(self):
        if self.Security is None:
            return 0.0001
        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 0.0001
        decimals = self.Security.Decimals
        if decimals == 3 or decimals == 5:
            return step * 10
        return step

    def _update_ma_buffer(self, ema_val):
        self._ma_values.append(ema_val)
        max_count = max(self._ma_shift.Value + 1, 1)
        while len(self._ma_values) > max_count:
            self._ma_values.popleft()

    def _get_shifted_ma(self):
        count = len(self._ma_values)
        target = count - self._ma_shift.Value - 1
        if target < 0:
            return None
        idx = 0
        for v in self._ma_values:
            if idx == target:
                return v
            idx += 1
        return None

    def CreateClone(self):
        return farhad_crab1_strategy()
