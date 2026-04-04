import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class Entry(object):
    def __init__(self, price, volume):
        self.Price = price
        self.Volume = volume


class zs1_forex_instruments_strategy(Strategy):
    """Hedged grid strategy converted from MetaTrader 'Zs1_www_forex-instruments_info'.
    Opens an initial buy/sell pair, tracks price zones relative to the starting level,
    and adds or closes positions according to tunnel logic."""

    def __init__(self):
        super(zs1_forex_instruments_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle timeframe for price sampling.", "General")

        self._orders_space_pips = self.Param("OrdersSpacePips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")

        self._pk_pips = self.Param("PkPips", 10) \
            .SetDisplay("Zone Offset (pips)", "Additional offset applied when checking zone boundaries.", "Trading")

        self._long_entries = []
        self._short_entries = []
        self._pip_value = 0.0
        self._first_price = 0.0
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_stage = 0
        self._first_order_direction = None
        self._last_order_direction = None
        self._is_closing_all = False
        self._current_price = 0.0
        self._has_price_data = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrdersSpacePips(self):
        return self._orders_space_pips.Value

    @OrdersSpacePips.setter
    def OrdersSpacePips(self, value):
        self._orders_space_pips.Value = value

    @property
    def PkPips(self):
        return self._pk_pips.Value

    @PkPips.setter
    def PkPips(self, value):
        self._pk_pips.Value = value

    def OnReseted(self):
        super(zs1_forex_instruments_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._pip_value = 0.0
        self._first_price = 0.0
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_stage = 0
        self._first_order_direction = None
        self._last_order_direction = None
        self._is_closing_all = False
        self._current_price = 0.0
        self._has_price_data = False

    def OnStarted2(self, time):
        super(zs1_forex_instruments_strategy, self).OnStarted2(time)

        self._pip_value = self._calculate_pip_value()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._current_price = float(candle.ClosePrice)
        self._has_price_data = True

        if not self._has_price_data or self._current_price <= 0.0:
            return

        if self._is_closing_all:
            return

        orders_total = self._get_orders_total()

        if self._first_stage != 0:
            self._check_zone()

        if self._first_stage == 0 and orders_total == 0:
            self._open_first()
            orders_total = self._get_orders_total()

        if self._zone_changed:
            self._process_zone_change()

        if orders_total >= 3 and self._calculate_floating_profit() >= 0.0:
            self._close_all_orders()

    def OnOwnTradeReceived(self, trade):
        super(zs1_forex_instruments_strategy, self).OnOwnTradeReceived(trade)

        if trade is None or trade.Trade is None:
            return

        volume = float(trade.Trade.Volume)
        price = float(trade.Trade.Price)
        side = trade.Order.Side if trade.Order is not None else None

        if side is None:
            return

        if side == Sides.Buy:
            if self.Position < 0 or self._is_closing_all:
                # Closing short
                self._reduce_entries(self._short_entries, volume)
            else:
                # Opening long
                self._long_entries.append(Entry(price, volume))
                self._last_order_direction = Sides.Buy
        else:
            if self.Position > 0 or self._is_closing_all:
                # Closing long
                self._reduce_entries(self._long_entries, volume)
            else:
                # Opening short
                self._short_entries.append(Entry(price, volume))
                self._last_order_direction = Sides.Sell

        if self._first_stage == 1 and self._first_price == 0.0 and len(self._long_entries) > 0 and len(self._short_entries) > 0:
            long_price = self._long_entries[0].Price
            short_price = self._short_entries[0].Price
            self._first_price = (long_price + short_price) / 2.0

        if len(self._long_entries) == 0 and len(self._short_entries) == 0 and self._is_closing_all:
            self._reset_state()
            self._is_closing_all = False

    def _process_zone_change(self):
        if self._first_stage == 1:
            self._zone_f1()
        elif self._first_stage == 2:
            if self._first_order_direction == Sides.Buy:
                if self._zone == -2:
                    self._zone_minus_two()
                elif self._zone == -1:
                    self._zone_minus_one()
                elif self._zone == 0:
                    self._zone_zero()
                elif self._zone == 1 or self._zone == 2:
                    self._zone_plus_one()
            elif self._first_order_direction == Sides.Sell:
                if self._zone == 2:
                    self._zone_minus_two()
                elif self._zone == 1:
                    self._zone_minus_one()
                elif self._zone == 0:
                    self._zone_zero()
                elif self._zone == -1 or self._zone == -2:
                    self._zone_plus_one()

    def _zone_f1(self):
        self._zone_changed = False
        self._close_first_orders()

    def _zone_minus_two(self):
        self._zone_changed = False
        if self._calculate_floating_profit() > 0.0:
            self._close_all_orders()
        else:
            self._open_another()

    def _zone_minus_one(self):
        self._zone_changed = False
        if self._first_order_direction is None:
            return
        if self._first_order_direction == Sides.Buy:
            self._open_sell_order()
        else:
            self._open_buy_order()

    def _zone_zero(self):
        self._zone_changed = False
        if self._first_order_direction is None:
            return
        if self._first_order_direction == Sides.Buy:
            self._open_buy_order()
        else:
            self._open_sell_order()

    def _zone_plus_one(self):
        self._zone_changed = False
        if self._calculate_floating_profit() > 0.0:
            self._close_all_orders()
        else:
            self._open_another()

    def _open_first(self):
        self.BuyMarket()
        self.SellMarket()

        self._first_stage = 1
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_price = self._current_price
        self._first_order_direction = None
        self._last_order_direction = None

    def _close_first_orders(self):
        if len(self._long_entries) > 0 and self._current_price > self._long_entries[0].Price:
            # Long is profitable, close it, keep short
            self.SellMarket()
            self._first_stage = 2
            self._first_order_direction = Sides.Sell
            self._last_order_direction = Sides.Sell
            return

        if len(self._short_entries) > 0 and self._current_price < self._short_entries[0].Price:
            # Short is profitable, close it, keep long
            self.BuyMarket()
            self._first_stage = 2
            self._first_order_direction = Sides.Buy
            self._last_order_direction = Sides.Buy

    def _open_buy_order(self):
        self.BuyMarket()

    def _open_sell_order(self):
        self.SellMarket()

    def _open_another(self):
        if self._last_order_direction == Sides.Buy:
            self._open_sell_order()
        elif self._last_order_direction == Sides.Sell:
            self._open_buy_order()
        elif self._first_order_direction == Sides.Buy:
            self._open_sell_order()
        elif self._first_order_direction == Sides.Sell:
            self._open_buy_order()

    def _close_all_orders(self):
        if self._is_closing_all:
            return

        self._zone_changed = False
        self._is_closing_all = True

        # Close by selling longs and buying back shorts
        if len(self._long_entries) > 0:
            total_long = 0.0
            for e in self._long_entries:
                total_long += e.Volume
            if total_long > 0.0:
                self.SellMarket(total_long)

        if len(self._short_entries) > 0:
            total_short = 0.0
            for e in self._short_entries:
                total_short += e.Volume
            if total_short > 0.0:
                self.BuyMarket(total_short)

        if len(self._long_entries) == 0 and len(self._short_entries) == 0:
            self._reset_state()
            self._is_closing_all = False

    def _reset_state(self):
        self._long_entries = []
        self._short_entries = []
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_stage = 0
        self._first_order_direction = None
        self._last_order_direction = None
        self._first_price = 0.0

    def _check_zone(self):
        step = float(self.OrdersSpacePips) * self._pip_value
        if step <= 0.0 or self._first_price <= 0.0:
            return

        offset = float(self.PkPips) * self._pip_value
        price = self._current_price + offset

        if price >= self._first_price + step * (self._zone + 1):
            self._last_zone = self._zone
            self._zone += 1
            self._zone_changed = True
        elif price <= self._first_price - step * (1 - self._zone):
            self._last_zone = self._zone
            self._zone -= 1
            self._zone_changed = True

        if self._zone_changed and self._zone == self._last_zone:
            self._zone_changed = False

    def _get_orders_total(self):
        return len(self._long_entries) + len(self._short_entries)

    def _calculate_floating_profit(self):
        if not self._has_price_data:
            return 0.0

        profit = 0.0
        for entry in self._long_entries:
            profit += (self._current_price - entry.Price) * entry.Volume
        for entry in self._short_entries:
            profit += (entry.Price - self._current_price) * entry.Volume
        return profit

    def _reduce_entries(self, entries, volume):
        remaining = volume
        while remaining > 0.0 and len(entries) > 0:
            current = entries[0]
            if current.Volume <= remaining + 0.0001:
                remaining -= current.Volume
                entries.pop(0)
            else:
                current.Volume -= remaining
                remaining = 0.0

    def _calculate_pip_value(self):
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0.0:
            return 1.0
        return float(step)

    def CreateClone(self):
        return zs1_forex_instruments_strategy()
