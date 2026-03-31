import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class precipice_martin_strategy(Strategy):
    """Precipice Martin: grid strategy with alternating long/short entries and martingale sizing."""

    def __init__(self):
        super(precipice_martin_strategy, self).__init__()

        self._use_buy = self.Param("UseBuy", True) \
            .SetDisplay("Use Buy", "Enable opening long positions", "Trading")
        self._buy_step_pips = self.Param("BuyStepPips", 89) \
            .SetDisplay("Buy SL/TP (pips)", "Stop loss and take profit distance for longs", "Trading")
        self._use_sell = self.Param("UseSell", True) \
            .SetDisplay("Use Sell", "Enable opening short positions", "Trading")
        self._sell_step_pips = self.Param("SellStepPips", 89) \
            .SetDisplay("Sell SL/TP (pips)", "Stop loss and take profit distance for shorts", "Trading")
        self._use_martingale = self.Param("UseMartingale", True) \
            .SetDisplay("Use Martingale", "Increase volume after losing trades", "Position sizing")
        self._martingale_coefficient = self.Param("MartingaleCoefficient", 1.6) \
            .SetGreaterThanZero() \
            .SetDisplay("Martingale Coefficient", "Multiplier applied after losses", "Position sizing")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used to generate trading bars", "General")

        self._pip_size = 0.0
        self._martingale_multiplier = 1.0
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._last_long_volume = 0.0
        self._last_short_volume = 0.0
        self._prefer_long_entry = True

    @property
    def UseBuy(self):
        return self._use_buy.Value
    @property
    def BuyStepPips(self):
        return int(self._buy_step_pips.Value)
    @property
    def UseSell(self):
        return self._use_sell.Value
    @property
    def SellStepPips(self):
        return int(self._sell_step_pips.Value)
    @property
    def UseMartingale(self):
        return self._use_martingale.Value
    @property
    def MartingaleCoefficient(self):
        return float(self._martingale_coefficient.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _adjust_volume(self, volume):
        sec = self.Security
        if sec is not None and sec.VolumeStep is not None:
            step = float(sec.VolumeStep)
            if step > 0:
                volume = math.floor(volume / step) * step

        if sec is not None and sec.MinVolume is not None:
            min_v = float(sec.MinVolume)
            if min_v > 0 and volume < min_v:
                volume = 0.0

        if sec is not None and sec.MaxVolume is not None:
            max_v = float(sec.MaxVolume)
            if max_v > 0 and volume > max_v:
                volume = max_v

        return volume

    def _calculate_order_volume(self):
        sec = self.Security
        min_volume = float(sec.MinVolume) if sec is not None and sec.MinVolume is not None else self.Volume
        if min_volume <= 0:
            min_volume = 1.0

        multiplier = self._martingale_multiplier if self.UseMartingale else 1.0
        volume = min_volume * multiplier
        return self._adjust_volume(volume)

    def _update_martingale(self, realized_pnl):
        if not self.UseMartingale:
            self._martingale_multiplier = 1.0
            return

        if realized_pnl > 0:
            self._martingale_multiplier = 1.0
        else:
            self._martingale_multiplier *= self.MartingaleCoefficient

    def OnStarted2(self, time):
        super(precipice_martin_strategy, self).OnStarted2(time)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        self._pip_size = step * 10.0
        if self._pip_size <= 0:
            self._pip_size = step if step > 0 else 1.0

        self._martingale_multiplier = 1.0
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._last_long_volume = 0.0
        self._last_short_volume = 0.0
        self._prefer_long_entry = True

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        closed_long = self._try_close_long(candle)
        closed_short = self._try_close_short(candle)

        if self.Position != 0:
            return

        if closed_long or closed_short:
            return

        if self._long_entry_price is not None or self._short_entry_price is not None:
            return

        if self.UseBuy and self.UseSell:
            if self._prefer_long_entry:
                if self._try_enter_long(candle):
                    self._prefer_long_entry = False
                    return
                if self._try_enter_short(candle):
                    self._prefer_long_entry = False
            else:
                if self._try_enter_short(candle):
                    self._prefer_long_entry = True
                    return
                if self._try_enter_long(candle):
                    self._prefer_long_entry = True
        else:
            if self.UseBuy:
                self._try_enter_long(candle)
            if self.UseSell:
                self._try_enter_short(candle)

    def _try_enter_long(self, candle):
        if self._long_entry_price is not None:
            return False
        if self.Position != 0:
            return False

        volume = self._calculate_order_volume()
        if volume <= 0:
            return False

        entry_price = float(candle.ClosePrice)
        self.BuyMarket()

        self._long_entry_price = entry_price
        self._last_long_volume = volume

        if self.BuyStepPips > 0:
            offset = self.BuyStepPips * self._pip_size
            self._long_stop_price = entry_price - offset
            self._long_take_price = entry_price + offset
        else:
            self._long_stop_price = None
            self._long_take_price = None

        return True

    def _try_enter_short(self, candle):
        if self._short_entry_price is not None:
            return False
        if self.Position != 0:
            return False

        volume = self._calculate_order_volume()
        if volume <= 0:
            return False

        entry_price = float(candle.ClosePrice)
        self.SellMarket()

        self._short_entry_price = entry_price
        self._last_short_volume = volume

        if self.SellStepPips > 0:
            offset = self.SellStepPips * self._pip_size
            self._short_stop_price = entry_price + offset
            self._short_take_price = entry_price - offset
        else:
            self._short_stop_price = None
            self._short_take_price = None

        return True

    def _try_close_long(self, candle):
        if self._long_entry_price is None:
            return False

        volume = self.Position
        if volume <= 0:
            volume = self._last_long_volume
        if volume <= 0:
            return False

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        stop_hit = self._long_stop_price is not None and lo <= self._long_stop_price
        take_hit = self._long_take_price is not None and h >= self._long_take_price

        if not stop_hit and not take_hit:
            return False

        exit_price = self._long_stop_price if stop_hit else self._long_take_price

        self.SellMarket()

        pnl = (exit_price - self._long_entry_price) * volume
        self._update_martingale(pnl)

        self._reset_long_state()
        return True

    def _try_close_short(self, candle):
        if self._short_entry_price is None:
            return False

        volume = abs(self.Position)
        if volume <= 0:
            volume = self._last_short_volume
        if volume <= 0:
            return False

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        stop_hit = self._short_stop_price is not None and h >= self._short_stop_price
        take_hit = self._short_take_price is not None and lo <= self._short_take_price

        if not stop_hit and not take_hit:
            return False

        exit_price = self._short_stop_price if stop_hit else self._short_take_price

        self.BuyMarket()

        pnl = (self._short_entry_price - exit_price) * volume
        self._update_martingale(pnl)

        self._reset_short_state()
        return True

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._last_long_volume = 0.0

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._last_short_volume = 0.0

    def OnReseted(self):
        super(precipice_martin_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._martingale_multiplier = 1.0
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._last_long_volume = 0.0
        self._last_short_volume = 0.0
        self._prefer_long_entry = True

    def CreateClone(self):
        return precipice_martin_strategy()
