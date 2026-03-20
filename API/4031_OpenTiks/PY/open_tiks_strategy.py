import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class open_tiks_strategy(Strategy):
    def __init__(self):
        super(open_tiks_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume of each market entry in lots", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 0.0) \
            .SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 30.0) \
            .SetDisplay("Trailing Stop (points)", "Trailing distance expressed in price points", "Risk")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Max Orders", "Maximum number of simultaneously open entries", "Trading")
        self._use_partial_close = self.Param("UsePartialClose", True) \
            .SetDisplay("Use Partial Close", "Close half of the position whenever the trailing stop advances", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe used for pattern detection", "General")

        self._price_step = 1.0
        self._volume_step = 0.0
        self._min_volume_limit = 0.0
        self._max_volume_limit = 0.0

        self._high1 = None
        self._high2 = None
        self._high3 = None
        self._open1 = None
        self._open2 = None
        self._open3 = None

        self._long_entry_price = None
        self._short_entry_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def MaxOrders(self):
        return self._max_orders.Value

    @property
    def UsePartialClose(self):
        return self._use_partial_close.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _normalize_entry_volume(self, volume):
        if volume <= 0:
            return 0.0
        if self._volume_step > 0:
            steps = round(volume / self._volume_step)
            if steps <= 0:
                steps = 1
            volume = steps * self._volume_step
        if self._min_volume_limit > 0 and volume < self._min_volume_limit:
            volume = self._min_volume_limit
        if self._max_volume_limit > 0 and volume > self._max_volume_limit:
            volume = self._max_volume_limit
        return volume

    def _normalize_exit_volume(self, desired, current_position):
        if desired <= 0 or current_position <= 0:
            return 0.0
        volume = desired
        if self._volume_step > 0:
            steps = round(volume / self._volume_step)
            if steps <= 0:
                steps = 1
            volume = steps * self._volume_step
        if volume > current_position:
            volume = current_position
        return volume

    def OnStarted(self, time):
        super(open_tiks_strategy, self).OnStarted(time)

        self._price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._price_step = ps

        self._volume_step = 0.0
        if self.Security is not None and self.Security.VolumeStep is not None:
            vs = float(self.Security.VolumeStep)
            if vs > 0:
                self._volume_step = vs

        self._min_volume_limit = 0.0
        if self.Security is not None and self.Security.MinVolume is not None:
            mv = float(self.Security.MinVolume)
            if mv > 0:
                self._min_volume_limit = mv

        self._max_volume_limit = 0.0
        if self.Security is not None and self.Security.MaxVolume is not None:
            mv = float(self.Security.MaxVolume)
            if mv > 0:
                self._max_volume_limit = mv

        self.Volume = self._normalize_entry_volume(float(self.OrderVolume))

        self._dummy_sma = SimpleMovingAverage()
        self._dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._dummy_sma, self.ProcessCandle).Start()

    def OnOwnTradeReceived(self, trade):
        super(open_tiks_strategy, self).OnOwnTradeReceived(trade)

        if trade.Trade is not None and trade.Trade.Price is not None:
            self._last_trade_price = float(trade.Trade.Price)
        elif trade.Order is not None and trade.Order.Price is not None:
            self._last_trade_price = float(trade.Order.Price)

    def OnPositionReceived(self, position):
        super(open_tiks_strategy, self).OnPositionReceived(position)

        delta = self.Position - self._previous_position

        if self.Position > 0:
            if self._previous_position <= 0:
                self._long_entry_price = self._last_trade_price
                self._long_trailing_stop = None
                self._short_entry_price = None
                self._short_trailing_stop = None
            elif delta > 0 and self._last_trade_price is not None:
                prev_vol = max(0.0, self._previous_position)
                cur_vol = max(0.0, self.Position)
                if cur_vol > 0:
                    current_entry = self._long_entry_price if self._long_entry_price is not None else self._last_trade_price
                    self._long_entry_price = (current_entry * prev_vol + self._last_trade_price * delta) / cur_vol
        elif self.Position < 0:
            if self._previous_position >= 0:
                self._short_entry_price = self._last_trade_price
                self._short_trailing_stop = None
                self._long_entry_price = None
                self._long_trailing_stop = None
            elif delta < 0 and self._last_trade_price is not None:
                prev_vol = max(0.0, abs(self._previous_position))
                cur_vol = max(0.0, abs(self.Position))
                if cur_vol > 0:
                    current_entry = self._short_entry_price if self._short_entry_price is not None else self._last_trade_price
                    self._short_entry_price = (current_entry * prev_vol + self._last_trade_price * abs(delta)) / cur_vol
        else:
            self._long_entry_price = None
            self._short_entry_price = None
            self._long_trailing_stop = None
            self._short_trailing_stop = None

        self._previous_position = self.Position

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_trailing(candle)

        buy_signal = False
        sell_signal = False

        if (self._high1 is not None and self._high2 is not None and self._high3 is not None and
                self._open1 is not None and self._open2 is not None and self._open3 is not None):
            high = float(candle.HighPrice)
            open_p = float(candle.OpenPrice)

            buy_signal = (high > self._high1 and self._high1 > self._high2 and self._high2 > self._high3 and
                          open_p > self._open1 and self._open1 > self._open2 and self._open2 > self._open3)

            sell_signal = (high < self._high1 and self._high1 < self._high2 and self._high2 < self._high3 and
                           open_p < self._open1 and self._open1 < self._open2 and self._open2 < self._open3)

        self._high3 = self._high2
        self._high2 = self._high1
        self._high1 = float(candle.HighPrice)

        self._open3 = self._open2
        self._open2 = self._open1
        self._open1 = float(candle.OpenPrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if buy_signal:
            self._try_enter_long()

        if sell_signal:
            self._try_enter_short()

    def _try_enter_long(self):
        max_ord = self.MaxOrders
        if max_ord > 0 and self._estimate_orders_count(self.Position) >= max_ord:
            return

        volume = self._normalize_entry_volume(float(self.OrderVolume))
        if volume <= 0:
            return

        self.BuyMarket(volume)

    def _try_enter_short(self):
        max_ord = self.MaxOrders
        if max_ord > 0 and self._estimate_orders_count(self.Position) >= max_ord:
            return

        volume = self._normalize_entry_volume(float(self.OrderVolume))
        if volume <= 0:
            return

        self.SellMarket(volume)

    def _estimate_orders_count(self, position_volume):
        base_volume = self._normalize_entry_volume(float(self.OrderVolume))
        if base_volume <= 0:
            return 1 if position_volume != 0 else 0
        ratio = abs(position_volume) / base_volume
        if ratio <= 0:
            return 0
        return int(math.ceil(ratio))

    def _update_trailing(self, candle):
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        stop_distance = float(self.StopLossPoints) * self._price_step
        trailing_distance = float(self.TrailingStopPoints) * self._price_step

        if self.Position > 0 and self._long_entry_price is not None:
            entry_long = self._long_entry_price

            if stop_distance > 0 and low <= entry_long - stop_distance:
                self.SellMarket(abs(self.Position))
                return

            if trailing_distance > 0 and close - entry_long >= trailing_distance:
                desired_stop = close - trailing_distance
                if self._long_trailing_stop is None or desired_stop > self._long_trailing_stop:
                    self._long_trailing_stop = desired_stop
                    self._try_reduce_long_position()

                if self._long_trailing_stop is not None and low <= self._long_trailing_stop:
                    self.SellMarket(abs(self.Position))

        elif self.Position < 0 and self._short_entry_price is not None:
            entry_short = self._short_entry_price
            position_volume = abs(self.Position)

            if stop_distance > 0 and high >= entry_short + stop_distance:
                self.BuyMarket(position_volume)
                return

            if trailing_distance > 0 and entry_short - close >= trailing_distance:
                desired_stop = close + trailing_distance
                if self._short_trailing_stop is None or desired_stop < self._short_trailing_stop:
                    self._short_trailing_stop = desired_stop
                    self._try_reduce_short_position()

                if self._short_trailing_stop is not None and high >= self._short_trailing_stop:
                    self.BuyMarket(position_volume)

    def _try_reduce_long_position(self):
        if not self.UsePartialClose:
            return
        if self.Position <= 0:
            return

        position_volume = abs(self.Position)
        half = position_volume / 2.0
        normalized_half = self._normalize_exit_volume(half, position_volume)

        if self._min_volume_limit > 0 and normalized_half < self._min_volume_limit:
            self.SellMarket(position_volume)
            return

        if normalized_half > 0:
            self.SellMarket(normalized_half)

    def _try_reduce_short_position(self):
        if not self.UsePartialClose:
            return
        if self.Position >= 0:
            return

        position_volume = abs(self.Position)
        half = position_volume / 2.0
        normalized_half = self._normalize_exit_volume(half, position_volume)

        if self._min_volume_limit > 0 and normalized_half < self._min_volume_limit:
            self.BuyMarket(position_volume)
            return

        if normalized_half > 0:
            self.BuyMarket(normalized_half)

    def OnReseted(self):
        super(open_tiks_strategy, self).OnReseted()
        self._price_step = 1.0
        self._volume_step = 0.0
        self._min_volume_limit = 0.0
        self._max_volume_limit = 0.0
        self._high1 = None
        self._high2 = None
        self._high3 = None
        self._open1 = None
        self._open2 = None
        self._open3 = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None

    def CreateClone(self):
        return open_tiks_strategy()
