import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import Highest, Lowest, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class absorption_strategy(Strategy):
    def __init__(self):
        super(absorption_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._max_search = self.Param("MaxSearch", 10)
        self._take_profit_buy = self.Param("TakeProfitBuy", 10.0)
        self._take_profit_sell = self.Param("TakeProfitSell", 10.0)
        self._trailing_stop = self.Param("TrailingStop", 5.0)
        self._trailing_step = self.Param("TrailingStep", 5.0)
        self._indent = self.Param("Indent", 1.0)
        self._order_expiration_hours = self.Param("OrderExpirationHours", 8)
        self._breakeven = self.Param("Breakeven", 1.0)
        self._breakeven_profit = self.Param("BreakevenProfit", 10.0)

        self._highest = None
        self._lowest = None

        self._prev1 = None
        self._prev2 = None

        self._has_active_orders = False
        self._pending_high = 0.0
        self._pending_low = 0.0
        self._pending_buy_price = 0.0
        self._pending_sell_price = 0.0
        self._pending_buy_stop_loss = 0.0
        self._pending_sell_stop_loss = 0.0
        self._pending_buy_take_profit = 0.0
        self._pending_sell_take_profit = 0.0
        self._orders_expiry = None

        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_position = 0.0
        self._exit_request_active = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(absorption_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self._max_search.Value
        self._lowest = Lowest()
        self._lowest.Length = self._max_search.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ_h = CandleIndicatorValue(self._highest, candle)
        civ_h.IsFinal = True
        high_result = self._highest.Process(civ_h)
        civ_l = CandleIndicatorValue(self._lowest, candle)
        civ_l.IsFinal = True
        low_result = self._lowest.Process(civ_l)

        if high_result.IsEmpty or low_result.IsEmpty or not self._highest.IsFormed or not self._lowest.IsFormed:
            self._update_previous_candles(candle)
            self._prev_position = self.Position
            return

        highest_value = float(high_result.Value)
        lowest_value = float(low_result.Value)

        self._manage_active_position(candle)

        if self._has_active_orders:
            if self._orders_expiry is not None and candle.CloseTime >= self._orders_expiry:
                self._clear_pending_orders()
            else:
                self._try_trigger_pending_orders(candle)

        if self.Position == 0 and not self._has_active_orders and self._prev1 is not None and self._prev2 is not None:
            self._try_place_orders(candle, highest_value, lowest_value)

        self._update_previous_candles(candle)

        if self.Position != 0 and self._has_active_orders:
            self._clear_pending_orders()

        self._prev_position = self.Position

    def _try_trigger_pending_orders(self, candle):
        if self.Position != 0:
            return

        if self._pending_buy_price > 0 and float(candle.HighPrice) >= self._pending_buy_price:
            self.BuyMarket(self.Volume)
            self._entry_price = self._pending_buy_price
            self._stop_loss = self._pending_buy_stop_loss
            self._take_profit = self._pending_buy_take_profit
            self._exit_request_active = False
            self._clear_pending_orders()
            return

        if self._pending_sell_price > 0 and float(candle.LowPrice) <= self._pending_sell_price:
            self.SellMarket(self.Volume)
            self._entry_price = self._pending_sell_price
            self._stop_loss = self._pending_sell_stop_loss
            self._take_profit = self._pending_sell_take_profit
            self._exit_request_active = False
            self._clear_pending_orders()

    def _try_place_orders(self, candle, highest_value, lowest_value):
        prev2_high = float(self._prev2.HighPrice)
        prev2_low = float(self._prev2.LowPrice)
        prev1_high = float(self._prev1.HighPrice)
        prev1_low = float(self._prev1.LowPrice)
        cur_high = float(candle.HighPrice)
        cur_low = float(candle.LowPrice)

        prev2_outside = prev2_high > prev1_high and prev2_low < prev1_low
        prev1_outside = prev1_high > prev2_high and prev1_low < prev2_low

        prev2_is_extreme = (self._is_lowest_bar(prev2_low, prev1_low, cur_low, lowest_value) or
                            self._is_highest_bar(prev2_high, prev1_high, cur_high, highest_value))
        prev1_is_extreme = (self._is_lowest_bar(prev1_low, prev2_low, cur_low, lowest_value) or
                            self._is_highest_bar(prev1_high, prev2_high, cur_high, highest_value))

        if prev2_outside and prev2_is_extreme:
            self._place_entry_orders(self._prev2, candle)
        elif prev1_outside and prev1_is_extreme:
            self._place_entry_orders(self._prev1, candle)

    def _place_entry_orders(self, pattern_candle, current_candle):
        volume = float(self.Volume)
        if volume <= 0:
            return

        indent = self._get_price_offset(self._indent.Value)
        step = self._get_price_step()

        buy_price = float(pattern_candle.HighPrice) + indent
        sell_price = float(pattern_candle.LowPrice) - indent
        if sell_price <= 0:
            sell_price = step

        buy_stop_loss = max(float(pattern_candle.LowPrice) - indent, step)
        sell_stop_loss = float(pattern_candle.HighPrice) + indent

        buy_take_offset = self._get_price_offset(self._take_profit_buy.Value)
        sell_take_offset = self._get_price_offset(self._take_profit_sell.Value)

        buy_take_profit = buy_price + buy_take_offset if buy_take_offset > 0 else 0.0
        sell_take_profit = sell_price - sell_take_offset if sell_take_offset > 0 else 0.0

        self._has_active_orders = True
        self._pending_high = float(pattern_candle.HighPrice)
        self._pending_low = float(pattern_candle.LowPrice)
        self._pending_buy_price = buy_price
        self._pending_sell_price = sell_price
        self._pending_buy_stop_loss = buy_stop_loss
        self._pending_sell_stop_loss = sell_stop_loss
        self._pending_buy_take_profit = buy_take_profit
        self._pending_sell_take_profit = sell_take_profit
        self._exit_request_active = False

        if self._order_expiration_hours.Value > 0:
            self._orders_expiry = current_candle.CloseTime + TimeSpan.FromHours(self._order_expiration_hours.Value)
        else:
            self._orders_expiry = None

    def _manage_active_position(self, candle):
        if self._exit_request_active:
            return

        if self.Position > 0:
            self._update_breakeven_long(candle)
            self._update_trailing_long(candle)

            if self._stop_loss > 0 and float(candle.LowPrice) <= self._stop_loss:
                self.SellMarket(abs(self.Position))
                self._exit_request_active = True
                return

            if self._take_profit > 0 and float(candle.HighPrice) >= self._take_profit:
                self.SellMarket(abs(self.Position))
                self._exit_request_active = True

        elif self.Position < 0:
            self._update_breakeven_short(candle)
            self._update_trailing_short(candle)

            if self._stop_loss > 0 and float(candle.HighPrice) >= self._stop_loss:
                self.BuyMarket(abs(self.Position))
                self._exit_request_active = True
                return

            if self._take_profit > 0 and float(candle.LowPrice) <= self._take_profit:
                self.BuyMarket(abs(self.Position))
                self._exit_request_active = True

    def _update_breakeven_long(self, candle):
        if self._breakeven.Value <= 0 or self._breakeven_profit.Value <= 0:
            return
        be_offset = self._get_price_offset(self._breakeven.Value)
        if self._stop_loss >= self._entry_price + be_offset:
            return
        be_profit_offset = self._get_price_offset(self._breakeven_profit.Value)
        if float(candle.HighPrice) - self._entry_price >= be_profit_offset:
            self._stop_loss = self._entry_price + be_offset

    def _update_breakeven_short(self, candle):
        if self._breakeven.Value <= 0 or self._breakeven_profit.Value <= 0:
            return
        be_offset = self._get_price_offset(self._breakeven.Value)
        if self._stop_loss <= self._entry_price - be_offset:
            return
        be_profit_offset = self._get_price_offset(self._breakeven_profit.Value)
        if self._entry_price - float(candle.LowPrice) >= be_profit_offset:
            self._stop_loss = self._entry_price - be_offset

    def _update_trailing_long(self, candle):
        if self._trailing_stop.Value <= 0:
            return
        trailing = self._get_price_offset(self._trailing_stop.Value)
        step = self._get_price_offset(self._trailing_step.Value)
        current = float(candle.HighPrice)

        if current - self._entry_price <= trailing + step:
            return
        if self._stop_loss < current - (trailing + step):
            self._stop_loss = max(self._stop_loss, current - trailing)

    def _update_trailing_short(self, candle):
        if self._trailing_stop.Value <= 0:
            return
        trailing = self._get_price_offset(self._trailing_stop.Value)
        step = self._get_price_offset(self._trailing_step.Value)
        current = float(candle.LowPrice)

        if self._entry_price - current <= trailing + step:
            return
        if self._stop_loss == 0 or self._stop_loss > current + trailing + step:
            self._stop_loss = current + trailing

    def _update_previous_candles(self, candle):
        self._prev2 = self._prev1
        self._prev1 = candle

    def _clear_pending_orders(self):
        self._has_active_orders = False
        self._orders_expiry = None
        self._pending_high = 0.0
        self._pending_low = 0.0
        self._pending_buy_price = 0.0
        self._pending_sell_price = 0.0
        self._pending_buy_stop_loss = 0.0
        self._pending_sell_stop_loss = 0.0
        self._pending_buy_take_profit = 0.0
        self._pending_sell_take_profit = 0.0

    def _is_lowest_bar(self, candidate_low, other_low, current_low, lowest_value):
        if not self._are_close(candidate_low, lowest_value):
            return False
        return candidate_low < other_low and candidate_low < current_low

    def _is_highest_bar(self, candidate_high, other_high, current_high, highest_value):
        if not self._are_close(candidate_high, highest_value):
            return False
        return candidate_high > other_high and candidate_high > current_high

    def _get_price_offset(self, value):
        step = self._get_price_step()
        return value * step

    def _get_price_step(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        return step if step > 0 else 0.0001

    def _are_close(self, first, second):
        tolerance = self._get_price_step() / 2.0
        return abs(first - second) <= tolerance

    def OnReseted(self):
        super(absorption_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._prev1 = None
        self._prev2 = None
        self._has_active_orders = False
        self._orders_expiry = None
        self._pending_high = 0.0
        self._pending_low = 0.0
        self._pending_buy_price = 0.0
        self._pending_sell_price = 0.0
        self._pending_buy_stop_loss = 0.0
        self._pending_sell_stop_loss = 0.0
        self._pending_buy_take_profit = 0.0
        self._pending_sell_take_profit = 0.0
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._prev_position = 0.0
        self._exit_request_active = False

    def CreateClone(self):
        return absorption_strategy()
