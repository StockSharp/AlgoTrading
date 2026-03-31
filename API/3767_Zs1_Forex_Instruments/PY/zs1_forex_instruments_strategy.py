import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


# Direction constants (replacing C# Sides enum and OrderIntents)
SIDE_BUY = 0
SIDE_SELL = 1

TUNNEL_MULTIPLIERS = [1, 3, 6, 12, 24, 48, 96, 192, 384, 768, 1536, 3072]


class zs1_forex_instruments_strategy(Strategy):
    """Hedged grid strategy converted from MetaTrader 'Zs1_www_forex-instruments_info'.
    Opens an initial buy/sell pair, tracks price zones relative to the starting level,
    and adds or closes positions according to tunnel logic. Converted from Level1 to candles."""

    def __init__(self):
        super(zs1_forex_instruments_strategy, self).__init__()

        self._orders_space_pips = self.Param("OrdersSpacePips", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels", "Trading")
        self._pk_pips = self.Param("PkPips", 10) \
            .SetDisplay("Zone Offset (pips)", "Additional offset applied when checking zone boundaries", "Trading")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial Volume", "Base volume for the hedge orders", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe of candles used for price tracking", "General")

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrdersSpacePips(self):
        return self._orders_space_pips.Value

    @property
    def PkPips(self):
        return self._pk_pips.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

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

    def OnStarted2(self, time):
        super(zs1_forex_instruments_strategy, self).OnStarted2(time)

        self._pip_value = self._calculate_pip_value()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _calculate_pip_value(self):
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            return 1.0
        step = float(step)
        scaled = step
        digits = 0
        while scaled < 1.0 and digits < 10:
            scaled *= 10.0
            digits += 1
        adjust = 10.0 if (digits == 3 or digits == 5) else 1.0
        return step * adjust

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._is_closing_all:
            return

        price = float(candle.ClosePrice)
        orders_total = self._get_orders_total()

        if self._first_stage != 0:
            self._check_zone(price)

        if self._first_stage == 0 and orders_total == 0:
            self._open_first(price)
            orders_total = self._get_orders_total()

        if self._zone_changed:
            self._process_zone_change(price)

        if orders_total >= 3 and self._calculate_floating_profit(price) >= 0:
            self._close_all_orders()

    def _get_orders_total(self):
        return len(self._long_entries) + len(self._short_entries)

    def _open_first(self, price):
        """Open the initial hedge pair: one buy + one sell."""
        vol = float(self.InitialVolume)

        self.BuyMarket()
        self._long_entries.append([price, vol])
        self._last_order_direction = SIDE_BUY

        self.SellMarket()
        self._short_entries.append([price, vol])
        self._last_order_direction = SIDE_SELL

        self._first_stage = 1
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_price = price
        self._first_order_direction = None
        self._last_order_direction = None

    def _check_zone(self, price):
        step = float(self.OrdersSpacePips) * self._pip_value
        if step <= 0 or self._first_price <= 0:
            return

        offset = float(self.PkPips) * self._pip_value
        check_price = price

        if self._last_order_direction == SIDE_SELL:
            check_price = price + offset
        elif self._last_order_direction == SIDE_BUY:
            check_price = price - offset

        if check_price >= self._first_price + step * (self._zone + 1):
            self._last_zone = self._zone
            self._zone += 1
            self._zone_changed = True
        elif check_price <= self._first_price + step * (self._zone - 1):
            self._last_zone = self._zone
            self._zone -= 1
            self._zone_changed = True

        if self._zone_changed and self._zone == self._last_zone:
            self._zone_changed = False

    def _process_zone_change(self, price):
        if self._first_stage == 1:
            self._zone_f1(price)
        elif self._first_stage == 2:
            if self._first_order_direction == SIDE_BUY:
                if self._zone == -2:
                    self._zone_minus_two(price)
                elif self._zone == -1:
                    self._zone_minus_one()
                elif self._zone == 0:
                    self._zone_zero()
                elif self._zone == 1 or self._zone == 2:
                    self._zone_plus_one(price)
            elif self._first_order_direction == SIDE_SELL:
                if self._zone == 2:
                    self._zone_minus_two(price)
                elif self._zone == 1:
                    self._zone_minus_one()
                elif self._zone == 0:
                    self._zone_zero()
                elif self._zone == -1 or self._zone == -2:
                    self._zone_plus_one(price)

    def _zone_f1(self, price):
        self._zone_changed = False
        self._close_first_orders(price)

    def _zone_minus_two(self, price):
        self._zone_changed = False
        if self._calculate_floating_profit(price) > 0:
            self._close_all_orders()
        else:
            self._open_another()

    def _zone_minus_one(self):
        self._zone_changed = False
        if self._first_order_direction is None:
            return
        if self._first_order_direction == SIDE_BUY:
            self._open_sell_order()
        else:
            self._open_buy_order()

    def _zone_zero(self):
        self._zone_changed = False
        if self._first_order_direction is None:
            return
        if self._first_order_direction == SIDE_BUY:
            self._open_buy_order()
        else:
            self._open_sell_order()

    def _zone_plus_one(self, price):
        self._zone_changed = False
        if self._calculate_floating_profit(price) > 0:
            self._close_all_orders()
        else:
            self._open_another()

    def _close_first_orders(self, price):
        """Close the winning leg of the initial hedge and determine direction."""
        if len(self._long_entries) > 0 and price > self._long_entries[0][0]:
            # Long is profitable, close it
            self.SellMarket()
            self._long_entries.pop(0)
            self._first_stage = 2
            self._first_order_direction = SIDE_SELL
            self._last_order_direction = SIDE_SELL
            return

        if len(self._short_entries) > 0 and price < self._short_entries[0][0]:
            # Short is profitable, close it
            self.BuyMarket()
            self._short_entries.pop(0)
            self._first_stage = 2
            self._first_order_direction = SIDE_BUY
            self._last_order_direction = SIDE_BUY

    def _open_buy_order(self):
        vol = self._calculate_order_volume()
        if vol <= 0:
            return
        self.BuyMarket()
        close_price = self._first_price  # approximate entry at current reference
        self._long_entries.append([close_price, vol])
        self._last_order_direction = SIDE_BUY

    def _open_sell_order(self):
        vol = self._calculate_order_volume()
        if vol <= 0:
            return
        self.SellMarket()
        close_price = self._first_price
        self._short_entries.append([close_price, vol])
        self._last_order_direction = SIDE_SELL

    def _open_another(self):
        if self._last_order_direction == SIDE_BUY:
            self._open_sell_order()
        elif self._last_order_direction == SIDE_SELL:
            self._open_buy_order()
        elif self._first_order_direction == SIDE_BUY:
            self._open_sell_order()
        elif self._first_order_direction == SIDE_SELL:
            self._open_buy_order()

    def _close_all_orders(self):
        if self._is_closing_all:
            return
        self._zone_changed = False
        self._is_closing_all = True

        # Close all longs
        if len(self._long_entries) > 0:
            self.SellMarket()
            self._long_entries = []

        # Close all shorts
        if len(self._short_entries) > 0:
            self.BuyMarket()
            self._short_entries = []

        self._reset_state()
        self._is_closing_all = False

    def _reset_state(self):
        self._zone = 0
        self._last_zone = 0
        self._zone_changed = False
        self._first_stage = 0
        self._first_order_direction = None
        self._last_order_direction = None
        self._first_price = 0.0

    def _calculate_order_volume(self):
        base_volume = float(self.InitialVolume)
        orders_total = self._get_orders_total()
        multiplier = 1.0

        if (self._zone == 0 or self._zone == -1) and orders_total >= 1 \
                and self._first_order_direction == SIDE_BUY:
            multiplier = self._get_tunnel_multiplier(orders_total)
        elif (self._zone == 0 or self._zone == 1) and orders_total >= 1 \
                and self._first_order_direction == SIDE_SELL:
            multiplier = self._get_tunnel_multiplier(orders_total)

        return base_volume * multiplier

    def _get_tunnel_multiplier(self, orders_total):
        if orders_total < 0:
            return 1.0
        if orders_total >= len(TUNNEL_MULTIPLIERS):
            return float(TUNNEL_MULTIPLIERS[-1])
        return float(TUNNEL_MULTIPLIERS[orders_total])

    def _calculate_floating_profit(self, price):
        profit = 0.0
        for entry in self._long_entries:
            profit += (price - entry[0]) * entry[1]
        for entry in self._short_entries:
            profit += (entry[0] - price) * entry[1]
        return profit

    def CreateClone(self):
        return zs1_forex_instruments_strategy()
