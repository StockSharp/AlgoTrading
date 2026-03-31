import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class pendulum_swing_strategy(Strategy):
    def __init__(self):
        super(pendulum_swing_strategy, self).__init__()
        self._base_volume = self.Param("BaseVolume", 0.1).SetGreaterThanZero().SetDisplay("Base volume", "Initial lot", "Risk")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Volume multiplier", "Progression factor", "Risk")
        self._max_levels = self.Param("MaxLevels", 8).SetGreaterThanZero().SetDisplay("Maximum levels", "Max fills per direction", "Risk")
        self._manual_step_pips = self.Param("ManualStepPips", 50).SetGreaterThanZero().SetDisplay("Manual step (pips)", "Fallback distance", "Entry")
        self._tp_pips = self.Param("TakeProfitPips", 10).SetDisplay("Take profit (pips)", "Local profit target", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))).SetDisplay("Trading candle", "Primary timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pendulum_swing_strategy, self).OnReseted()
        self._pip_size = 0
        self._pending_buy_price = 0
        self._pending_sell_price = 0
        self._pending_buy_vol = 0
        self._pending_sell_vol = 0
        self._long_level = 0
        self._short_level = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(pendulum_swing_strategy, self).OnStarted2(time)
        self._pip_size = self._get_pip_size()
        self._pending_buy_price = 0
        self._pending_sell_price = 0
        self._pending_buy_vol = 0
        self._pending_sell_vol = 0
        self._long_level = 0
        self._short_level = 0
        self._entry_price = 0
        self.Volume = self._base_volume.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def _get_pip_size(self):
        step = 0.01
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        return step

    def _get_next_volume(self, level):
        base = self._base_volume.Value
        if base <= 0:
            return 0
        if level >= self._max_levels.Value:
            return 0
        mult = self._volume_multiplier.Value if self._volume_multiplier.Value > 0 else 1.0
        return base * (mult ** level)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_tp(candle)
        self._ensure_pendulum(candle)

    def _manage_tp(self, candle):
        if self._tp_pips.Value <= 0 or self.Position == 0 or self._pip_size <= 0 or self._entry_price <= 0:
            return
        diff = float(candle.ClosePrice) - self._entry_price
        threshold = self._tp_pips.Value * self._pip_size
        if self.Position > 0 and diff >= threshold:
            self.SellMarket()
            self._long_level = 0
        elif self.Position < 0 and -diff >= threshold:
            self.BuyMarket()
            self._short_level = 0

    def _ensure_pendulum(self, candle):
        step = self._manual_step_pips.Value * self._pip_size if self._manual_step_pips.Value > 0 and self._pip_size > 0 else 0
        if step <= 0:
            return

        if self._pending_buy_price > 0 and self._pending_buy_vol > 0 and candle.HighPrice >= self._pending_buy_price:
            self.BuyMarket(self._pending_buy_vol)
            self._entry_price = float(candle.ClosePrice)
            self._long_level = min(self._long_level + 1, self._max_levels.Value)
            self._pending_buy_price = 0
            self._pending_buy_vol = 0

        if self._pending_sell_price > 0 and self._pending_sell_vol > 0 and candle.LowPrice <= self._pending_sell_price:
            self.SellMarket(self._pending_sell_vol)
            self._entry_price = float(candle.ClosePrice)
            self._short_level = min(self._short_level + 1, self._max_levels.Value)
            self._pending_sell_price = 0
            self._pending_sell_vol = 0

        if self.Position == 0:
            self._long_level = 0
            self._short_level = 0

        self._pending_buy_price = float(candle.ClosePrice) + step
        self._pending_sell_price = float(candle.ClosePrice) - step
        self._pending_buy_vol = self._get_next_volume(self._long_level)
        self._pending_sell_vol = self._get_next_volume(self._short_level)

    def CreateClone(self):
        return pendulum_swing_strategy()
