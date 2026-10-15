import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

VOL_TICK = 0
VOL_REAL = 1
VOL_NONE = 2


class three_candles_reversal_strategy(Strategy):
    def __init__(self):
        super(three_candles_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._signal_bar = self.Param("SignalBar", 1)
        self._max_bar_size = self.Param("MaxBarSize", 300)
        self._volume_filter = self.Param("VolumeFilter", VOL_TICK)
        self._allow_buy_entry = self.Param("AllowBuyEntry", True)
        self._allow_sell_entry = self.Param("AllowSellEntry", True)
        self._allow_buy_exit = self.Param("AllowBuyExit", True)
        self._allow_sell_exit = self.Param("AllowSellExit", True)
        self._stop_loss_pips = self.Param("StopLossPips", 1000.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 2000.0)

        self._candles = []
        self._last_bullish_signal_time = None
        self._last_bearish_signal_time = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def MaxBarSize(self):
        return self._max_bar_size.Value

    @MaxBarSize.setter
    def MaxBarSize(self, value):
        self._max_bar_size.Value = value

    @property
    def VolumeFilter(self):
        return self._volume_filter.Value

    @VolumeFilter.setter
    def VolumeFilter(self, value):
        self._volume_filter.Value = value

    @property
    def AllowBuyEntry(self):
        return self._allow_buy_entry.Value

    @AllowBuyEntry.setter
    def AllowBuyEntry(self, value):
        self._allow_buy_entry.Value = value

    @property
    def AllowSellEntry(self):
        return self._allow_sell_entry.Value

    @AllowSellEntry.setter
    def AllowSellEntry(self, value):
        self._allow_sell_entry.Value = value

    @property
    def AllowBuyExit(self):
        return self._allow_buy_exit.Value

    @AllowBuyExit.setter
    def AllowBuyExit(self, value):
        self._allow_buy_exit.Value = value

    @property
    def AllowSellExit(self):
        return self._allow_sell_exit.Value

    @AllowSellExit.setter
    def AllowSellExit(self, value):
        self._allow_sell_exit.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    def OnStarted2(self, time):
        super(three_candles_reversal_strategy, self).OnStarted2(time)

        self._candles = []
        self._last_bullish_signal_time = None
        self._last_bearish_signal_time = None
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        close_time = candle.CloseTime if candle.CloseTime is not None else open_time
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)

        self._candles.append((open_time, close_time, open_price, high, low, close, volume))

        sb = int(self.SignalBar)
        required = sb + 5
        while len(self._candles) > required:
            self._candles.pop(0)

        if len(self._candles) < required:
            return

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if price_step <= 0.0:
            price_step = 1.0

        if self._check_risk_management(candle, price_step):
            return

        buffer = list(self._candles)
        bullish_signal = self._is_bullish_signal(buffer, price_step, sb)
        bearish_signal = self._is_bearish_signal(buffer, price_step, sb)

        if bullish_signal:
            signal_candle = self._get_series(buffer, sb)
            self._handle_bullish(signal_candle)

        if bearish_signal:
            signal_candle = self._get_series(buffer, sb)
            self._handle_bearish(signal_candle)

    def _check_risk_management(self, candle, price_step):
        if self.Position == 0 or self._entry_price == 0.0:
            return False

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        stop_distance = float(self.StopLossPips) * price_step if float(self.StopLossPips) > 0.0 else 0.0
        take_distance = float(self.TakeProfitPips) * price_step if float(self.TakeProfitPips) > 0.0 else 0.0

        if self.Position > 0:
            stop_triggered = stop_distance > 0.0 and low <= self._entry_price - stop_distance
            take_triggered = take_distance > 0.0 and high >= self._entry_price + take_distance
            if stop_triggered or take_triggered:
                self.SellMarket()
                self._entry_price = 0.0
                return True
        elif self.Position < 0:
            stop_triggered = stop_distance > 0.0 and high >= self._entry_price + stop_distance
            take_triggered = take_distance > 0.0 and low <= self._entry_price - take_distance
            if stop_triggered or take_triggered:
                self.BuyMarket()
                self._entry_price = 0.0
                return True

        return False

    def _handle_bullish(self, signal_candle):
        signal_time = signal_candle[1]
        if self._last_bullish_signal_time == signal_time:
            return

        if self.AllowSellExit and self.Position < 0:
            self.BuyMarket()
            self._entry_price = 0.0

        if self.AllowBuyEntry and self.Position == 0:
            self.BuyMarket()
            self._entry_price = signal_candle[5]

        self._last_bullish_signal_time = signal_time

    def _handle_bearish(self, signal_candle):
        signal_time = signal_candle[1]
        if self._last_bearish_signal_time == signal_time:
            return

        if self.AllowBuyExit and self.Position > 0:
            self.SellMarket()
            self._entry_price = 0.0

        if self.AllowSellEntry and self.Position == 0:
            self.SellMarket()
            self._entry_price = signal_candle[5]

        self._last_bearish_signal_time = signal_time

    def _is_bullish_signal(self, candles, price_step, sb):
        last = self._get_series(candles, sb + 1)
        middle = self._get_series(candles, sb + 2)
        oldest = self._get_series(candles, sb + 3)

        if not (oldest[2] > oldest[5] and
                middle[2] > middle[5] and
                middle[5] > oldest[4] and
                last[2] < last[5] and
                last[5] > middle[2]):
            return False

        if not self._should_apply_volume_filter(oldest, price_step):
            return True

        vol_oldest = oldest[6]
        vol_middle = middle[6]
        vol_last = last[6]

        return vol_oldest < vol_middle or vol_last > vol_middle or vol_last > vol_oldest

    def _is_bearish_signal(self, candles, price_step, sb):
        last = self._get_series(candles, sb + 1)
        middle = self._get_series(candles, sb + 2)
        oldest = self._get_series(candles, sb + 3)

        if not (oldest[2] < oldest[5] and
                middle[2] < middle[5] and
                middle[5] < oldest[3] and
                last[2] > last[5] and
                last[5] < middle[2]):
            return False

        if not self._should_apply_volume_filter(oldest, price_step):
            return True

        vol_oldest = oldest[6]
        vol_middle = middle[6]
        vol_last = last[6]

        return vol_oldest < vol_middle or vol_last > vol_middle or vol_last > vol_oldest

    def _should_apply_volume_filter(self, oldest, price_step):
        if int(self.VolumeFilter) == VOL_NONE:
            return False
        if int(self.MaxBarSize) <= 0:
            return False
        bar_range = oldest[3] - oldest[4]
        threshold = int(self.MaxBarSize) * price_step
        if bar_range > threshold:
            return False
        return True

    def _get_series(self, candles, index):
        idx = len(candles) - 1 - index
        return candles[idx]

    def OnReseted(self):
        super(three_candles_reversal_strategy, self).OnReseted()
        self._candles = []
        self._last_bullish_signal_time = None
        self._last_bearish_signal_time = None
        self._entry_price = 0.0

    def CreateClone(self):
        return three_candles_reversal_strategy()
