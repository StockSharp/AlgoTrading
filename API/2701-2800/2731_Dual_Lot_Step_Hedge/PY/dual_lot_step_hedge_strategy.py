import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class dual_lot_step_hedge_strategy(Strategy):
    def __init__(self):
        super(dual_lot_step_hedge_strategy, self).__init__()

        self._lot_multiplier = self.Param("LotMultiplier", 10)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 150.0)
        self._min_profit = self.Param("MinProfit", 27.0)
        self._scaling_mode = self.Param("ScalingMode", 0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._volume_step = 0.0
        self._max_volume = 0.0
        self._current_volume = 0.0
        self._pip_value = 0.0
        self._initial_equity = 0.0

        self._long_volume = 0.0
        self._short_volume = 0.0
        self._long_avg_price = 0.0
        self._short_avg_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LotMultiplier(self):
        return self._lot_multiplier.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def MinProfit(self):
        return self._min_profit.Value

    @property
    def ScalingMode(self):
        return self._scaling_mode.Value

    def OnStarted2(self, time):
        super(dual_lot_step_hedge_strategy, self).OnStarted2(time)

        sec = self.Security
        vs = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0.0
        if vs <= 0:
            vs = 1.0
        self._volume_step = vs

        self._max_volume = self._lot_check(vs * self.LotMultiplier)
        if self._max_volume <= 0:
            self._max_volume = vs

        self._current_volume = self._max_volume if self.ScalingMode == 0 else vs
        self._pip_value = self._calculate_pip_value()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        if self._volume_step <= 0:
            return

        if self._initial_equity <= 0:
            pf = self.Portfolio
            if pf is not None and pf.CurrentValue is not None:
                self._initial_equity = float(pf.CurrentValue)

        self._check_protective_levels(price)

        if self._check_profit_target():
            return

        self._reset_current_volume_if_needed()

        buy_count = 1 if self._long_volume > 0 else 0
        sell_count = 1 if self._short_volume > 0 else 0

        if buy_count > 1 or sell_count > 1:
            self._close_all()
            return

        if buy_count == 0 and sell_count == 0:
            self._try_open_hedge(price)
        elif buy_count == 1 and sell_count == 0:
            self._open_short_if_needed(price)
        elif buy_count == 0 and sell_count == 1:
            self._open_long_if_needed(price)

    def _check_profit_target(self):
        if self._initial_equity <= 0 or self.MinProfit <= 0:
            return False
        pf = self.Portfolio
        if pf is None or pf.CurrentValue is None:
            return False
        current = float(pf.CurrentValue)
        if current - self._initial_equity >= self.MinProfit:
            self._close_all()
            return True
        return False

    def _try_open_hedge(self, price):
        vol = self._lot_check(self._current_volume)
        if vol <= 0:
            return
        self.BuyMarket()
        self._apply_long_open(vol, price)
        self.SellMarket()
        self._apply_short_open(vol, price)
        self._adjust_volume_after_entry()

    def _open_long_if_needed(self, price):
        vol = self._lot_check(self._current_volume)
        if vol <= 0:
            return
        self.BuyMarket()
        self._apply_long_open(vol, price)
        self._adjust_volume_after_entry()

    def _open_short_if_needed(self, price):
        vol = self._lot_check(self._current_volume)
        if vol <= 0:
            return
        self.SellMarket()
        self._apply_short_open(vol, price)
        self._adjust_volume_after_entry()

    def _adjust_volume_after_entry(self):
        if self.ScalingMode == 0:
            self._current_volume = self._lot_check(self._current_volume - self._volume_step)
        else:
            self._current_volume = self._lot_check(self._current_volume + self._volume_step)

    def _close_all(self):
        if self._long_volume > 0:
            self.SellMarket()
        if self._short_volume > 0:
            self.BuyMarket()
        self._long_volume = 0.0
        self._short_volume = 0.0
        self._long_avg_price = 0.0
        self._short_avg_price = 0.0
        self._initial_equity = 0.0
        if self.ScalingMode == 0:
            self._current_volume = 0.0
        else:
            self._current_volume = self._volume_step

    def _check_protective_levels(self, price):
        if self._pip_value <= 0:
            return

        if self._long_volume > 0:
            if self.StopLossPips > 0 and price <= self._long_avg_price - self.StopLossPips * self._pip_value:
                self.SellMarket()
                self._long_volume = 0.0
                self._long_avg_price = 0.0
                return
            if self.TakeProfitPips > 0 and price >= self._long_avg_price + self.TakeProfitPips * self._pip_value:
                self.SellMarket()
                self._long_volume = 0.0
                self._long_avg_price = 0.0
                return

        if self._short_volume > 0:
            if self.StopLossPips > 0 and price >= self._short_avg_price + self.StopLossPips * self._pip_value:
                self.BuyMarket()
                self._short_volume = 0.0
                self._short_avg_price = 0.0
                return
            if self.TakeProfitPips > 0 and price <= self._short_avg_price - self.TakeProfitPips * self._pip_value:
                self.BuyMarket()
                self._short_volume = 0.0
                self._short_avg_price = 0.0
                return

    def _reset_current_volume_if_needed(self):
        if self.ScalingMode == 0:
            if self._current_volume < self._volume_step:
                self._current_volume = self._max_volume
        else:
            if self._current_volume < self._volume_step:
                self._current_volume = self._volume_step
            elif self._current_volume > self._max_volume:
                self._current_volume = self._volume_step

    def _apply_long_open(self, volume, price):
        if volume <= 0:
            return
        total = self._long_volume + volume
        if self._long_volume <= 0:
            self._long_avg_price = price
        else:
            self._long_avg_price = (self._long_avg_price * self._long_volume + price * volume) / total
        self._long_volume = total

    def _apply_short_open(self, volume, price):
        if volume <= 0:
            return
        total = self._short_volume + volume
        if self._short_volume <= 0:
            self._short_avg_price = price
        else:
            self._short_avg_price = (self._short_avg_price * self._short_volume + price * volume) / total
        self._short_volume = total

    def _lot_check(self, volume):
        if volume <= 0:
            return 0.0
        step = self._volume_step
        if step <= 0:
            return 0.0
        ratio = math.floor(volume / step)
        normalized = ratio * step
        if normalized < step:
            normalized = 0.0
        if normalized > self._max_volume:
            normalized = self._max_volume
        return normalized

    def _calculate_pip_value(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step <= 0:
            return 1.0
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnReseted(self):
        super(dual_lot_step_hedge_strategy, self).OnReseted()
        self._volume_step = 0.0
        self._max_volume = 0.0
        self._current_volume = 0.0
        self._pip_value = 0.0
        self._initial_equity = 0.0
        self._long_volume = 0.0
        self._short_volume = 0.0
        self._long_avg_price = 0.0
        self._short_avg_price = 0.0

    def CreateClone(self):
        return dual_lot_step_hedge_strategy()
