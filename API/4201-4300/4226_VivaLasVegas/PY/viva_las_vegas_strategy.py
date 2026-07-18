import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class viva_las_vegas_strategy(Strategy):
    def __init__(self):
        super(viva_las_vegas_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Timeframe for pacing trades.", "General")
        self._stop_take_pips = self.Param("StopTakePips", 50) \
            .SetDisplay("Stop/take distance", "Protective distance in pips.", "Risk")
        self._base_volume = self.Param("BaseVolume", 1.0) \
            .SetDisplay("Base volume", "Initial lot size.", "Risk")
        self._seed = self.Param("Seed", 0) \
            .SetDisplay("Random seed", "Seed for pseudo-random direction. Zero = time-based.", "General")
        self._active_seed = 0
        self._prev_position = 0.0
        self._last_pnl = 0.0
        self._next_volume = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopTakePips(self):
        return self._stop_take_pips.Value
    @property
    def BaseVolume(self):
        return float(self._base_volume.Value)
    @property
    def SeedValue(self):
        return self._seed.Value

    def OnStarted2(self, time):
        super(viva_las_vegas_strategy, self).OnStarted2(time)
        import time as _time
        self._active_seed = int(_time.time() * 1000) if self.SeedValue == 0 else self.SeedValue
        self._prev_position = 0.0
        self._last_pnl = 0.0
        self._next_volume = self.BaseVolume
        self._entry_price = 0.0
        self._best_price = 0.0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def _get_pip_size(self):
        sec = self.Security
        if sec is not None:
            ps = sec.PriceStep
            if ps is not None and float(ps) > 0:
                return float(ps)
        return 0.0001

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        pip = self._get_pip_size()
        stop_dist = self.StopTakePips * pip
        # manage position exit
        if self.Position > 0:
            if close > self._best_price:
                self._best_price = close
            if stop_dist > 0 and (close <= self._entry_price - stop_dist or close >= self._entry_price + stop_dist):
                won = close >= self._entry_price + stop_dist
                self.SellMarket()
                self._update_volume(won)
                self._entry_price = 0.0
                self._best_price = 0.0
                return
        elif self.Position < 0:
            if close < self._best_price:
                self._best_price = close
            if stop_dist > 0 and (close >= self._entry_price + stop_dist or close <= self._entry_price - stop_dist):
                won = close <= self._entry_price - stop_dist
                self.BuyMarket()
                self._update_volume(won)
                self._entry_price = 0.0
                self._best_price = 0.0
                return
        if self.Position == 0:
            self._active_seed = (self._active_seed * 1103515245 + 12345) & 0x7FFFFFFF
            is_buy = ((self._active_seed >> 16) & 1) == 0
            self._entry_price = close
            self._best_price = close
            if is_buy:
                self.BuyMarket()
            else:
                self.SellMarket()

    def _update_volume(self, won):
        if won:
            self._next_volume = self.BaseVolume
        else:
            self._next_volume = self._next_volume * 2.0

    def OnReseted(self):
        super(viva_las_vegas_strategy, self).OnReseted()
        self._active_seed = 0
        self._prev_position = 0.0
        self._last_pnl = 0.0
        self._next_volume = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0

    def CreateClone(self):
        return viva_las_vegas_strategy()
