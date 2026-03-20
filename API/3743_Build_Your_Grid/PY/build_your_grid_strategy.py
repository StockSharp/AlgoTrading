import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class build_your_grid_strategy(Strategy):
    """Grid strategy that maintains layered long and short positions with configurable
    step progression, lot progression, profit targets and loss handling."""

    # Constants for order placement modes
    BOTH = 0
    LONG_ONLY = 1
    SHORT_ONLY = 2

    # Constants for grid direction
    WITH_TREND = 0
    AGAINST_TREND = 1

    # Constants for step progression
    STEP_STATIC = 0
    STEP_GEOMETRIC = 1
    STEP_EXPONENTIAL = 2

    # Constants for close target
    TARGET_PIPS = 0
    TARGET_CURRENCY = 1

    # Constants for loss close modes
    LOSS_DO_NOTHING = 0
    LOSS_CLOSE_FIRST = 1
    LOSS_CLOSE_ALL = 2

    # Constants for lot progression
    LOT_STATIC = 0
    LOT_GEOMETRIC = 1
    LOT_EXPONENTIAL = 2

    def __init__(self):
        super(build_your_grid_strategy, self).__init__()

        self._order_placement = self.Param("OrderPlacement", 0) \
            .SetDisplay("Order Placement", "Allowed entry direction (0=Both, 1=LongOnly, 2=ShortOnly)", "General")
        self._grid_direction = self.Param("GridDirection", 1) \
            .SetDisplay("Grid Direction", "Whether layers follow or fade the trend (0=WithTrend, 1=AgainstTrend)", "Grid")
        self._pips_for_next_order = self.Param("PipsForNextOrder", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid Step (pips)", "Base spacing between grid levels", "Grid")
        self._step_progression = self.Param("StepProgression", 1) \
            .SetDisplay("Step Progression", "How the distance grows with each layer (0=Static, 1=Geometric, 2=Exponential)", "Grid")
        self._close_target_mode = self.Param("CloseTarget", 0) \
            .SetDisplay("Close Target", "Profit target type (0=Pips, 1=Currency)", "Risk")
        self._pips_close_in_profit = self.Param("PipsCloseInProfit", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Target (pips)", "Basket profit target in pips", "Risk")
        self._currency_close_in_profit = self.Param("CurrencyCloseInProfit", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Target (currency)", "Basket profit target in account currency", "Risk")
        self._loss_close_mode = self.Param("LossMode", 2) \
            .SetDisplay("Loss Handling", "Action when loss threshold is hit (0=DoNothing, 1=CloseFirst, 2=CloseAll)", "Risk")
        self._pips_for_close_in_loss = self.Param("PipsForCloseInLoss", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Loss (pips)", "Allowed drawdown before protective close", "Risk")
        self._manual_lot_size = self.Param("ManualLotSize", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Manual Volume", "Order size when auto sizing is disabled", "Volume")
        self._lot_progression = self.Param("LotProgression", 0) \
            .SetDisplay("Lot Progression", "How volumes scale with each layer (0=Static, 1=Geometric, 2=Exponential)", "Volume")
        self._max_multiplier_lot = self.Param("MaxMultiplierLot", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Multiplier", "Cap for lot growth relative to first entry", "Volume")
        self._max_orders = self.Param("MaxOrders", 0) \
            .SetDisplay("Max Orders", "Maximum simultaneous positions (0 = unlimited)", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle series used for price tracking", "General")

        self._long_entries = []
        self._short_entries = []
        self._point_size = 0.0
        self._price_step = 0.0
        self._total_orders = 0
        self._buy_orders = 0
        self._sell_orders = 0
        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None
        self._first_buy_volume = 0.0
        self._first_sell_volume = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrderPlacement(self):
        return self._order_placement.Value

    @property
    def GridDirection(self):
        return self._grid_direction.Value

    @property
    def PipsForNextOrder(self):
        return self._pips_for_next_order.Value

    @property
    def StepProgression(self):
        return self._step_progression.Value

    @property
    def CloseTarget(self):
        return self._close_target_mode.Value

    @property
    def PipsCloseInProfit(self):
        return self._pips_close_in_profit.Value

    @property
    def CurrencyCloseInProfit(self):
        return self._currency_close_in_profit.Value

    @property
    def LossMode(self):
        return self._loss_close_mode.Value

    @property
    def PipsForCloseInLoss(self):
        return self._pips_for_close_in_loss.Value

    @property
    def ManualLotSize(self):
        return self._manual_lot_size.Value

    @property
    def LotProgression(self):
        return self._lot_progression.Value

    @property
    def MaxMultiplierLot(self):
        return self._max_multiplier_lot.Value

    @property
    def MaxOrders(self):
        return self._max_orders.Value

    def OnReseted(self):
        super(build_your_grid_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._point_size = 0.0
        self._price_step = 0.0
        self._total_orders = 0
        self._buy_orders = 0
        self._sell_orders = 0
        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None
        self._first_buy_volume = 0.0
        self._first_sell_volume = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0

    def OnStarted(self, time):
        super(build_your_grid_strategy, self).OnStarted(time)

        self._point_size = self._calculate_point_size()
        if self.Security is not None and self.Security.PriceStep is not None:
            self._price_step = float(self.Security.PriceStep)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        bid = float(candle.ClosePrice)
        ask = float(candle.ClosePrice)

        self._update_aggregates(bid, ask)

        if self._total_orders > 0:
            if self._should_close_in_profit():
                if self._close_all_positions():
                    return
            if self.LossMode != self.LOSS_DO_NOTHING and self._should_close_in_loss():
                if self.LossMode == self.LOSS_CLOSE_FIRST:
                    if self._close_first_positions():
                        return
                else:
                    if self._close_all_positions():
                        return

        if self._try_open_initial_orders(bid, ask):
            return

        self._try_open_next_orders(bid, ask)

    def _update_aggregates(self, bid, ask):
        self._total_orders = len(self._long_entries) + len(self._short_entries)
        self._buy_orders = len(self._long_entries)
        self._sell_orders = len(self._short_entries)

        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None

        for entry in self._long_entries:
            self._total_long_volume += entry[1]
            self._last_buy_price = entry[0]
            diff = bid - entry[0]
            self._buy_pips += self._calculate_pips(diff)

        for entry in self._short_entries:
            self._total_short_volume += entry[1]
            self._last_sell_price = entry[0]
            diff = entry[0] - ask
            self._sell_pips += self._calculate_pips(diff)

        self._first_buy_volume = self._long_entries[0][1] if len(self._long_entries) > 0 else 0.0
        self._first_sell_volume = self._short_entries[0][1] if len(self._short_entries) > 0 else 0.0
        self._last_buy_volume = self._long_entries[-1][1] if len(self._long_entries) > 0 else 0.0
        self._last_sell_volume = self._short_entries[-1][1] if len(self._short_entries) > 0 else 0.0

    def _calculate_pips(self, diff):
        if self._point_size > 0:
            return diff / self._point_size
        return diff

    def _should_close_in_profit(self):
        total_pips = self._buy_pips + self._sell_pips
        if self.CloseTarget == self.TARGET_PIPS:
            return total_pips >= float(self.PipsCloseInProfit)
        return False

    def _should_close_in_loss(self):
        total_pips = self._buy_pips + self._sell_pips
        return total_pips <= -float(self.PipsForCloseInLoss)

    def _close_all_positions(self):
        closed = False
        if self._buy_orders > 0 and self._total_long_volume > 0:
            self.SellMarket(self._total_long_volume)
            closed = True
        if self._sell_orders > 0 and self._total_short_volume > 0:
            self.BuyMarket(self._total_short_volume)
            closed = True
        if closed:
            self._long_entries = []
            self._short_entries = []
        return closed

    def _close_first_positions(self):
        closed = False
        if self._buy_orders > 0:
            volume = self._long_entries[0][1]
            if volume > 0:
                self.SellMarket(volume)
                self._long_entries.pop(0)
                closed = True
        if self._sell_orders > 0:
            volume = self._short_entries[0][1]
            if volume > 0:
                self.BuyMarket(volume)
                self._short_entries.pop(0)
                closed = True
        return closed

    def _can_open_more_orders(self):
        if self.MaxOrders <= 0:
            return True
        return self._total_orders < self.MaxOrders

    def _try_open_initial_orders(self, bid, ask):
        if not self._can_open_more_orders():
            return False

        allow_buy = self.OrderPlacement == self.BOTH or self.OrderPlacement == self.LONG_ONLY
        allow_sell = self.OrderPlacement == self.BOTH or self.OrderPlacement == self.SHORT_ONLY

        if self._buy_orders == 0 and allow_buy:
            volume = self._get_order_volume(True)
            if volume > 0:
                self.BuyMarket(volume)
                self._long_entries.append([ask, volume])
                return True

        if self._sell_orders == 0 and allow_sell:
            volume = self._get_order_volume(False)
            if volume > 0:
                self.SellMarket(volume)
                self._short_entries.append([bid, volume])
                return True

        return False

    def _try_open_next_orders(self, bid, ask):
        if not self._can_open_more_orders():
            return

        allow_buy = self.OrderPlacement != self.SHORT_ONLY
        allow_sell = self.OrderPlacement != self.LONG_ONLY

        if not allow_buy and not allow_sell:
            return

        has_buys = self._buy_orders > 0 or self.OrderPlacement == self.SHORT_ONLY
        has_sells = self._sell_orders > 0 or self.OrderPlacement == self.LONG_ONLY

        if not (has_buys and has_sells):
            return

        buy_distance = self._get_next_distance(True) if allow_buy else 0.0
        sell_distance = self._get_next_distance(False) if allow_sell else 0.0

        if self.GridDirection == self.WITH_TREND:
            if allow_buy and self._last_buy_price is not None and buy_distance > 0:
                trigger = self._last_buy_price + buy_distance
                if ask >= trigger:
                    volume = self._get_order_volume(True)
                    if volume > 0:
                        self.BuyMarket(volume)
                        self._long_entries.append([ask, volume])
                        return

            if allow_sell and self._last_sell_price is not None and sell_distance > 0:
                trigger = self._last_sell_price - sell_distance
                if bid <= trigger:
                    volume = self._get_order_volume(False)
                    if volume > 0:
                        self.SellMarket(volume)
                        self._short_entries.append([bid, volume])
                        return
        else:
            if allow_buy and self._last_buy_price is not None and buy_distance > 0:
                trigger = self._last_buy_price - buy_distance
                if ask <= trigger:
                    volume = self._get_order_volume(True)
                    if volume > 0:
                        self.BuyMarket(volume)
                        self._long_entries.append([ask, volume])
                        return

            if allow_sell and self._last_sell_price is not None and sell_distance > 0:
                trigger = self._last_sell_price + sell_distance
                if bid >= trigger:
                    volume = self._get_order_volume(False)
                    if volume > 0:
                        self.SellMarket(volume)
                        self._short_entries.append([bid, volume])
                        return

    def _get_next_distance(self, is_buy):
        base_distance = float(self.PipsForNextOrder)
        count = self._buy_orders if is_buy else self._sell_orders

        prog = self.StepProgression
        if prog == self.STEP_STATIC:
            multiplier = 1.0
        elif prog == self.STEP_GEOMETRIC:
            multiplier = max(1, count)
        elif prog == self.STEP_EXPONENTIAL:
            multiplier = max(1.0, Math.Pow(2, count - 1)) if count > 0 else 1.0
        else:
            multiplier = 1.0

        return base_distance * multiplier * self._point_size

    def _get_order_volume(self, is_buy):
        base_volume = float(self.ManualLotSize)
        first_volume = self._first_buy_volume if is_buy else self._first_sell_volume
        last_volume = self._last_buy_volume if is_buy else self._last_sell_volume
        orders = self._buy_orders if is_buy else self._sell_orders

        prog = self.LotProgression
        if prog == self.LOT_STATIC:
            result = base_volume if orders == 0 else (first_volume if first_volume > 0 else base_volume)
        elif prog == self.LOT_GEOMETRIC:
            if orders == 0:
                result = base_volume
            elif orders == 1:
                result = last_volume * 2.0
            else:
                result = last_volume + (first_volume if first_volume > 0 else base_volume)
        elif prog == self.LOT_EXPONENTIAL:
            result = base_volume if orders == 0 else last_volume * 2.0
        else:
            result = base_volume

        max_mult = float(self.MaxMultiplierLot)
        if max_mult > 0 and orders > 0 and first_volume > 0:
            cap = first_volume * max_mult
            if result > cap:
                result = cap

        if result <= 0:
            result = base_volume

        return result

    def _calculate_point_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0:
                return step
        return 0.0001

    def CreateClone(self):
        return build_your_grid_strategy()
