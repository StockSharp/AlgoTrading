import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ro_nz_auto_sl_ts_tp_strategy(Strategy):
    def __init__(self):
        super(ro_nz_auto_sl_ts_tp_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._take_profit_param = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._stop_loss_param = self.Param("StopLoss", 250.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._lock_after = self.Param("LockProfitAfter", 100.0) \
            .SetDisplay("Lock Profit After", "Profit threshold for locking", "Risk")
        self._profit_lock = self.Param("ProfitLock", 60.0) \
            .SetDisplay("Profit Lock", "Profit to lock after threshold", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 50.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 10.0) \
            .SetDisplay("Trailing Step", "Step for trailing stop", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Risk")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trail_anchor = 0.0
        self._prev_ema10 = 0.0
        self._prev_ema20 = 0.0
        self._profit_locked = False
        self._is_initialized = False
        self._bars_since_exit = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def take_profit(self):
        return self._take_profit_param.Value

    @property
    def stop_loss(self):
        return self._stop_loss_param.Value

    @property
    def lock_profit_after(self):
        return self._lock_after.Value

    @property
    def profit_lock(self):
        return self._profit_lock.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def trailing_step(self):
        return self._trailing_step.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(ro_nz_auto_sl_ts_tp_strategy, self).OnReseted()
        self._reset_protection()
        self._prev_ema10 = 0.0
        self._prev_ema20 = 0.0
        self._is_initialized = False
        self._bars_since_exit = self.cooldown_bars

    def _reset_protection(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._trail_anchor = 0.0
        self._profit_locked = False
        self._bars_since_exit = 0

    def OnStarted(self, time):
        super(ro_nz_auto_sl_ts_tp_strategy, self).OnStarted(time)
        ema10 = ExponentialMovingAverage()
        ema10.Length = 10
        ema20 = ExponentialMovingAverage()
        ema20.Length = 20
        ema100 = ExponentialMovingAverage()
        ema100.Length = 100
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema10, ema20, ema100, self.process_candle).Start()

    def process_candle(self, candle, ema10, ema20, ema100):
        if candle.State != CandleStates.Finished:
            return

        e10 = float(ema10)
        e20 = float(ema20)
        e100 = float(ema100)

        if not self._is_initialized:
            self._prev_ema10 = e10
            self._prev_ema20 = e20
            self._is_initialized = True
            return

        self._bars_since_exit += 1

        bullish_cross = self._prev_ema10 <= self._prev_ema20 and e10 > e20 and e10 > e100 and e20 > e100
        bearish_cross = self._prev_ema10 >= self._prev_ema20 and e10 < e20 and e10 < e100 and e20 < e100

        price = float(candle.ClosePrice)

        if self.Position == 0:
            if self._bars_since_exit >= self.cooldown_bars and bullish_cross:
                self.BuyMarket()
                self._set_initial_protection(price)
            elif self._bars_since_exit >= self.cooldown_bars and bearish_cross:
                self.SellMarket()
                self._set_initial_protection(price)
        else:
            self._manage_protection(price)

        self._prev_ema10 = e10
        self._prev_ema20 = e20

    def _set_initial_protection(self, price):
        self._entry_price = price
        self._profit_locked = False
        self._trail_anchor = price
        tp = float(self.take_profit)
        sl = float(self.stop_loss)

        if self.Position > 0:
            self._stop_price = price - sl if sl > 0 else 0.0
            self._take_price = price + tp if tp > 0 else 0.0
        elif self.Position < 0:
            self._stop_price = price + sl if sl > 0 else 0.0
            self._take_price = price - tp if tp > 0 else 0.0

    def _manage_protection(self, price):
        lock_after = float(self.lock_profit_after)
        p_lock = float(self.profit_lock)
        ts = float(self.trailing_stop)
        t_step = float(self.trailing_step)

        if self.Position > 0:
            if (self._take_price > 0 and price >= self._take_price) or (self._stop_price > 0 and price <= self._stop_price):
                self.SellMarket()
                self._reset_protection()
                return

            profit = price - self._entry_price

            if not self._profit_locked and lock_after > 0 and p_lock > 0 and profit >= lock_after:
                self._stop_price = self._entry_price + p_lock
                self._profit_locked = True

            if ts > 0 and (lock_after == 0 or profit >= lock_after):
                if price - self._trail_anchor >= t_step:
                    new_stop = price - ts
                    if new_stop > self._stop_price:
                        self._stop_price = new_stop
                    self._trail_anchor = price

        elif self.Position < 0:
            if (self._take_price > 0 and price <= self._take_price) or (self._stop_price > 0 and price >= self._stop_price):
                self.BuyMarket()
                self._reset_protection()
                return

            profit = self._entry_price - price

            if not self._profit_locked and lock_after > 0 and p_lock > 0 and profit >= lock_after:
                self._stop_price = self._entry_price - p_lock
                self._profit_locked = True

            if ts > 0 and (lock_after == 0 or profit >= lock_after):
                if self._trail_anchor - price >= t_step:
                    new_stop = price + ts
                    if self._stop_price == 0.0 or new_stop < self._stop_price:
                        self._stop_price = new_stop
                    self._trail_anchor = price

    def CreateClone(self):
        return ro_nz_auto_sl_ts_tp_strategy()
