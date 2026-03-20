import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class martin_for_small_deposits_strategy(Strategy):
    def __init__(self):
        super(martin_for_small_deposits_strategy, self).__init__()

        self._initial_volume = self.Param("InitialVolume", 0.01)
        self._take_profit_pips = self.Param("TakeProfitPips", 200)
        self._step_pips = self.Param("StepPips", 100)
        self._bars_to_skip = self.Param("BarsToSkip", 100)
        self._increase_factor = self.Param("IncreaseFactor", 1.7)
        self._max_volume = self.Param("MaxVolume", 6.0)
        self._min_profit = self.Param("MinProfit", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._position_volume = 0.0
        self._avg_price = 0.0
        self._extreme_price = 0.0
        self._last_entry_price = 0.0
        self._current_trade_count = 0
        self._current_direction = 0
        self._bars_since_last_entry = 0
        self._pip_size = 0.0
        self._close_history = [0.0] * 15
        self._close_history_count = 0
        self._latest_index = -1

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StepPips(self):
        return self._step_pips.Value

    @property
    def BarsToSkip(self):
        return self._bars_to_skip.Value

    @property
    def IncreaseFactor(self):
        return self._increase_factor.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def MinProfit(self):
        return self._min_profit.Value

    def OnStarted(self, time):
        super(martin_for_small_deposits_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_close_history(float(candle.ClosePrice))

        pip_size = self._ensure_pip_size()
        if pip_size <= 0:
            return

        step_dist = self.StepPips * pip_size if self.StepPips > 0 else 0.0
        tp_dist = self.TakeProfitPips * pip_size if self.TakeProfitPips > 0 else 0.0

        has_position = (self._position_volume > 0 or self.Position != 0 or
                        self._current_direction != 0)

        if not has_position:
            if not self._is_history_ready():
                return
            ref = self._get_reference_close()
            price = float(candle.ClosePrice)
            if price < ref:
                self._try_open_buy(price)
            elif price > ref:
                self._try_open_sell(price)
            return

        if self._position_volume <= 0 or self._current_direction == 0:
            return

        self._bars_since_last_entry += 1

        price = float(candle.ClosePrice)
        pnl = self._calculate_open_profit(price)

        if pnl > self.MinProfit:
            self._close_all()
            return

        if self._current_direction > 0:
            if tp_dist > 0 and price >= self._last_entry_price + tp_dist:
                self._close_all()
                return
            if self._bars_since_last_entry <= self.BarsToSkip:
                return
            if step_dist > 0 and self._extreme_price - price > step_dist:
                self._try_open_buy(price)
        elif self._current_direction < 0:
            if tp_dist > 0 and price <= self._last_entry_price - tp_dist:
                self._close_all()
                return
            if self._bars_since_last_entry <= self.BarsToSkip:
                return
            if step_dist > 0 and price - self._extreme_price > step_dist:
                self._try_open_sell(price)

    def _try_open_buy(self, price):
        vol = self._get_next_volume(1)
        if vol <= 0:
            return
        self.BuyMarket()
        self._apply_long_open(price, vol)

    def _try_open_sell(self, price):
        vol = self._get_next_volume(-1)
        if vol <= 0:
            return
        self.SellMarket()
        self._apply_short_open(price, vol)

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._reset_position_state()

    def _apply_long_open(self, price, volume):
        prev = self._position_volume
        self._position_volume += volume
        if prev == 0:
            self._avg_price = price
            self._extreme_price = price
        else:
            self._avg_price = (self._avg_price * prev + price * volume) / self._position_volume
            self._extreme_price = min(self._extreme_price, price)
        self._last_entry_price = price
        self._current_direction = 1
        self._current_trade_count += 1
        self._bars_since_last_entry = 0

    def _apply_short_open(self, price, volume):
        prev = self._position_volume
        self._position_volume += volume
        if prev == 0:
            self._avg_price = price
            self._extreme_price = price
        else:
            self._avg_price = (self._avg_price * prev + price * volume) / self._position_volume
            self._extreme_price = max(self._extreme_price, price)
        self._last_entry_price = price
        self._current_direction = -1
        self._current_trade_count += 1
        self._bars_since_last_entry = 0

    def _calculate_open_profit(self, price):
        if self._current_direction > 0:
            return (price - self._avg_price) * self._position_volume
        elif self._current_direction < 0:
            return (self._avg_price - price) * self._position_volume
        return 0.0

    def _get_next_volume(self, direction):
        base = self.InitialVolume
        if base <= 0:
            return 0.0
        depth = self._current_trade_count if self._current_direction == direction else 0
        if self.IncreaseFactor <= 0 or depth == 0:
            factor = 1.0
        else:
            raw = math.pow(self.IncreaseFactor, depth)
            if math.isinf(raw) or math.isnan(raw):
                return 0.0
            factor = raw
        vol = base * factor
        if self.MaxVolume > 0 and vol > self.MaxVolume:
            vol = self.MaxVolume
        return vol

    def _reset_position_state(self):
        self._position_volume = 0.0
        self._avg_price = 0.0
        self._extreme_price = 0.0
        self._last_entry_price = 0.0
        self._current_trade_count = 0
        self._current_direction = 0
        self._bars_since_last_entry = 0

    def _ensure_pip_size(self):
        if self._pip_size > 0:
            return self._pip_size
        sec = self.Security
        if sec is None:
            return 0.0
        step = float(sec.PriceStep) if sec.PriceStep is not None else 0.0
        if step == 0:
            d = sec.Decimals if sec.Decimals is not None else 0
            if d > 0:
                step = math.pow(10, -d)
        if step == 0:
            step = 0.01
        d_count = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        self._pip_size = step * 10.0 if (d_count == 3 or d_count == 5) else step
        if self._pip_size == 0:
            self._pip_size = step if step > 0 else 0.01
        return self._pip_size

    def _update_close_history(self, close_price):
        length = len(self._close_history)
        if length == 0:
            return
        self._latest_index = (self._latest_index + 1) % length
        self._close_history[self._latest_index] = close_price
        if self._close_history_count < length:
            self._close_history_count += 1

    def _is_history_ready(self):
        return self._close_history_count >= len(self._close_history)

    def _get_reference_close(self):
        index = (self._latest_index + 1) % len(self._close_history)
        return self._close_history[index]

    def OnReseted(self):
        super(martin_for_small_deposits_strategy, self).OnReseted()
        self._position_volume = 0.0
        self._avg_price = 0.0
        self._extreme_price = 0.0
        self._last_entry_price = 0.0
        self._current_trade_count = 0
        self._current_direction = 0
        self._bars_since_last_entry = 0
        self._pip_size = 0.0
        self._close_history = [0.0] * 15
        self._close_history_count = 0
        self._latest_index = -1

    def CreateClone(self):
        return martin_for_small_deposits_strategy()
