import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class exp_martin_v2_strategy(Strategy):
    """
    Exponential martingale strategy.
    Opens positions and doubles volume on loss, resets on profit.
    """

    def __init__(self):
        super(exp_martin_v2_strategy, self).__init__()
        self._start_volume = self.Param("StartVolume", 1.0) \
            .SetDisplay("Start Volume", "Initial order volume", "General")
        self._factor = self.Param("Factor", 2.0) \
            .SetDisplay("Factor", "Volume multiplier", "General")
        self._limit = self.Param("Limit", 5) \
            .SetDisplay("Limit", "Max multiplication count", "General")
        self._stop_loss = self.Param("StopLoss", 100) \
            .SetDisplay("Stop Loss", "Loss limit in points", "Risk")
        self._take_profit = self.Param("TakeProfit", 100) \
            .SetDisplay("Take Profit", "Profit target in points", "Risk")
        self._start_type = self.Param("StartType", 0) \
            .SetDisplay("Start Type", "0-Buy, 1-Sell", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CycleCooldownBars", 2) \
            .SetDisplay("Cycle Cooldown Bars", "Bars to wait before next cycle", "General")

        self._current_volume = 0.0
        self._max_volume = 0.0
        self._need_open = True
        self._direction = 0
        self._entry_price = 0.0
        self._long_take = 0.0
        self._long_stop = 0.0
        self._short_take = 0.0
        self._short_stop = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_martin_v2_strategy, self).OnReseted()
        self._current_volume = 0.0
        self._max_volume = 0.0
        self._need_open = True
        self._direction = 0
        self._entry_price = 0.0
        self._long_take = 0.0
        self._long_stop = 0.0
        self._short_take = 0.0
        self._short_stop = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(exp_martin_v2_strategy, self).OnStarted(time)

        sv = float(self._start_volume.Value)
        self._current_volume = sv
        self._direction = 1 if self._start_type.Value == 0 else -1

        self._max_volume = sv
        f = float(self._factor.Value)
        for i in range(self._limit.Value):
            self._max_volume = self._round_volume(self._max_volume * f)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if high >= self._long_take:
                self.SellMarket(self.Position)
                self._prepare_next(True)
            elif low <= self._long_stop:
                self.SellMarket(self.Position)
                self._prepare_next(False)
        elif self.Position < 0:
            if low <= self._short_take:
                self.BuyMarket(-self.Position)
                self._prepare_next(True)
            elif high >= self._short_stop:
                self.BuyMarket(-self.Position)
                self._prepare_next(False)

        if self._need_open and self.Position == 0 and self._cooldown_remaining == 0:
            self._entry_price = close
            tp = self._take_profit.Value
            sl = self._stop_loss.Value
            if self._direction == 1:
                self.BuyMarket(self._current_volume)
                self._long_take = self._entry_price + tp * step
                self._long_stop = self._entry_price - sl * step
            else:
                self.SellMarket(self._current_volume)
                self._short_take = self._entry_price - tp * step
                self._short_stop = self._entry_price + sl * step
            self._need_open = False

    def _prepare_next(self, was_profit):
        if was_profit:
            self._current_volume = float(self._start_volume.Value)
        else:
            self._direction = -self._direction
            self._current_volume = self._round_volume(self._current_volume * float(self._factor.Value))
            if self._current_volume > self._max_volume:
                self._current_volume = float(self._start_volume.Value)

        self._need_open = True
        self._cooldown_remaining = self._cooldown_bars.Value

    def _round_volume(self, volume):
        step = 1.0
        if self.Security is not None and self.Security.VolumeStep is not None:
            step = float(self.Security.VolumeStep)
        if step <= 0:
            step = 1.0
        return math.ceil(volume / step) * step

    def CreateClone(self):
        return exp_martin_v2_strategy()
