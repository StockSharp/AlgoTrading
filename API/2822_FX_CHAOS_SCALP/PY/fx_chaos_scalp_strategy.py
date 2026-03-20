import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class fx_chaos_scalp_strategy(Strategy):
    def __init__(self):
        super(fx_chaos_scalp_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 50.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 50.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._zig_zag_window_size = self.Param("ZigZagWindowSize", 5)

        self._ao = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous = False
        self._entry_price = 0.0
        self._has_entry = False

        self._zz_window = []
        self._zz_last_value = None
        self._zz_direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(fx_chaos_scalp_strategy, self).OnStarted(time)

        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = 5
        self._ao.LongMa.Length = 34

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_zigzag(candle)

        ao_result = self._ao.Process(candle)

        if not self._has_previous:
            self._update_previous_levels(candle)
            return

        if ao_result.IsEmpty or not self._ao.IsFormed:
            self._update_previous_levels(candle)
            return

        ao = float(ao_result)
        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)

        long_signal = open_price < self._previous_high and close_price > self._previous_high and ao < 0.0
        short_signal = open_price > self._previous_low and close_price < self._previous_low and ao > 0.0

        if long_signal and self.Position == 0:
            self.BuyMarket()
            self._entry_price = close_price
            self._has_entry = True
        elif short_signal and self.Position == 0:
            self.SellMarket()
            self._entry_price = close_price
            self._has_entry = True

        if self.Position != 0 and self._has_entry:
            if self._manage_risk(candle):
                pass

        if self.Position == 0:
            self._has_entry = False
            self._entry_price = 0.0

        self._update_previous_levels(candle)

    def _manage_risk(self, candle):
        if self.Position == 0:
            self._has_entry = False
            self._entry_price = 0.0
            return False

        if not self._has_entry:
            return False

        step = self._get_price_step()

        if self.Position > 0:
            stop = self._entry_price - self._stop_loss_points.Value * step if self._stop_loss_points.Value > 0 else None
            take = self._entry_price + self._take_profit_points.Value * step if self._take_profit_points.Value > 0 else None

            if stop is not None and float(candle.LowPrice) <= stop:
                self.SellMarket(self.Position)
                self._has_entry = False
                self._entry_price = 0.0
                return True
            if take is not None and float(candle.HighPrice) >= take:
                self.SellMarket(self.Position)
                self._has_entry = False
                self._entry_price = 0.0
                return True
        elif self.Position < 0:
            stop = self._entry_price + self._stop_loss_points.Value * step if self._stop_loss_points.Value > 0 else None
            take = self._entry_price - self._take_profit_points.Value * step if self._take_profit_points.Value > 0 else None

            if stop is not None and float(candle.HighPrice) >= stop:
                self.BuyMarket(abs(self.Position))
                self._has_entry = False
                self._entry_price = 0.0
                return True
            if take is not None and float(candle.LowPrice) <= take:
                self.BuyMarket(abs(self.Position))
                self._has_entry = False
                self._entry_price = 0.0
                return True

        return False

    def _update_zigzag(self, candle):
        ws = self._zig_zag_window_size.Value
        if ws < 3:
            ws = 3
        if ws % 2 == 0:
            ws += 1

        self._zz_window.append((float(candle.HighPrice), float(candle.LowPrice)))
        if len(self._zz_window) > ws:
            self._zz_window = self._zz_window[len(self._zz_window) - ws:]

        if len(self._zz_window) < ws:
            return

        center = ws // 2
        ch, cl = self._zz_window[center]
        is_up = True
        is_down = True

        for i in range(ws):
            if i == center:
                continue
            h, l = self._zz_window[i]
            if i < center:
                if ch <= h:
                    is_up = False
                if cl >= l:
                    is_down = False
            else:
                if ch < h:
                    is_up = False
                if cl > l:
                    is_down = False
            if not is_up and not is_down:
                break

        if is_up:
            if self._zz_direction == 1:
                if self._zz_last_value is None or ch > self._zz_last_value:
                    self._zz_last_value = ch
            else:
                self._zz_last_value = ch
                self._zz_direction = 1
        elif is_down:
            if self._zz_direction == -1:
                if self._zz_last_value is None or cl < self._zz_last_value:
                    self._zz_last_value = cl
            else:
                self._zz_last_value = cl
                self._zz_direction = -1

    def _update_previous_levels(self, candle):
        self._previous_high = float(candle.HighPrice)
        self._previous_low = float(candle.LowPrice)
        self._has_previous = True

    def _get_price_step(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        return step if step > 0 else 1.0

    def OnReseted(self):
        super(fx_chaos_scalp_strategy, self).OnReseted()
        self._ao = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._has_previous = False
        self._entry_price = 0.0
        self._has_entry = False
        self._zz_window = []
        self._zz_last_value = None
        self._zz_direction = 0

    def CreateClone(self):
        return fx_chaos_scalp_strategy()
