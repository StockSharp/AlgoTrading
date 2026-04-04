import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides
from StockSharp.Algo.Strategies import Strategy


class build_your_grid_strategy(Strategy):
    """Grid strategy converted from the MetaTrader expert 'BuildYourGridEA'.
    It maintains layered long and short positions, optionally increases volume geometrically
    and supports profit/loss group exits together with hedge rebalancing."""

    # OrderPlacementModes
    OP_BOTH = 0
    OP_LONG_ONLY = 1
    OP_SHORT_ONLY = 2

    # GridDirectionModes
    GD_WITH_TREND = 0
    GD_AGAINST_TREND = 1

    # StepProgressionModes
    SP_STATIC = 0
    SP_GEOMETRIC = 1
    SP_EXPONENTIAL = 2

    # CloseTargetModes
    CT_PIPS = 0
    CT_CURRENCY = 1

    # LossCloseModes
    LC_DO_NOTHING = 0
    LC_CLOSE_FIRST = 1
    LC_CLOSE_ALL = 2

    # LotProgressionModes
    LP_STATIC = 0
    LP_GEOMETRIC = 1
    LP_EXPONENTIAL = 2

    def __init__(self):
        super(build_your_grid_strategy, self).__init__()

        self._order_placement = self.Param("OrderPlacement", self.OP_LONG_ONLY) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._grid_direction = self.Param("GridDirection", self.GD_AGAINST_TREND) \
            .SetDisplay("Grid Direction", "Whether layers follow or fade the trend", "Grid")
        self._pips_for_next_order = self.Param("PipsForNextOrder", 500000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid Step (pips)", "Base spacing between grid levels", "Grid")
        self._step_progression = self.Param("StepProgression", self.SP_STATIC) \
            .SetDisplay("Step Progression", "How the distance grows with each layer", "Grid")
        self._close_target_mode = self.Param("CloseTarget", self.CT_PIPS) \
            .SetDisplay("Close Target", "Profit target type", "Risk")
        self._pips_close_in_profit = self.Param("PipsCloseInProfit", 500000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Target (pips)", "Basket profit target in pips", "Risk")
        self._currency_close_in_profit = self.Param("CurrencyCloseInProfit", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Target (currency)", "Basket profit target in account currency", "Risk")
        self._loss_close_mode = self.Param("LossMode", self.LC_DO_NOTHING) \
            .SetDisplay("Loss Handling", "Action when loss threshold is hit", "Risk")
        self._pips_for_close_in_loss = self.Param("PipsForCloseInLoss", 200000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Loss (pips)", "Allowed drawdown before protective close", "Risk")
        self._place_hedge_order = self.Param("PlaceHedgeOrder", False) \
            .SetDisplay("Use Hedge", "Enable hedge rebalancing", "Risk")
        self._hedge_loss_threshold = self.Param("HedgeLossThreshold", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Hedge Threshold (%)", "Loss percentage that triggers hedging", "Risk")
        self._hedge_volume_multiplier = self.Param("HedgeVolumeMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Hedge Multiplier", "Multiplier applied to imbalance volume", "Risk")
        self._auto_lot_size = self.Param("AutoLotSize", False) \
            .SetDisplay("Auto Volume", "Use balance driven order size", "Volume")
        self._risk_factor = self.Param("RiskFactor", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Factor", "Risk factor for automatic sizing", "Volume")
        self._manual_lot_size = self.Param("ManualLotSize", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Manual Volume", "Order size when auto sizing is disabled", "Volume")
        self._lot_progression = self.Param("LotProgression", self.LP_STATIC) \
            .SetDisplay("Lot Progression", "How volumes scale with each layer", "Volume")
        self._max_multiplier_lot = self.Param("MaxMultiplierLot", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Multiplier", "Cap for lot growth relative to first entry", "Volume")
        self._max_orders = self.Param("MaxOrders", 2) \
            .SetDisplay("Max Orders", "Maximum simultaneous positions (0 = unlimited)", "General")
        self._max_spread = self.Param("MaxSpread", 0.0) \
            .SetDisplay("Max Spread", "Maximum allowed spread in pips (0 = ignore)", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series used for price data", "General")

        self._long_entries = []   # list of [price, volume]
        self._short_entries = []  # list of [price, volume]

        self._current_price = 0.0
        self._point_size = 0.0
        self._price_step = 0.0
        self._step_price = 0.0

        self._total_orders = 0
        self._buy_orders = 0
        self._sell_orders = 0
        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_profit = 0.0
        self._sell_profit = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None
        self._first_buy_volume = 0.0
        self._first_sell_volume = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0
        self._is_hedged = False
        self._cooldown_bars = 0

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
    def PlaceHedgeOrder(self):
        return self._place_hedge_order.Value

    @property
    def HedgeLossThreshold(self):
        return self._hedge_loss_threshold.Value

    @property
    def HedgeVolumeMultiplier(self):
        return self._hedge_volume_multiplier.Value

    @property
    def AutoLotSize(self):
        return self._auto_lot_size.Value

    @property
    def RiskFactor(self):
        return self._risk_factor.Value

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

    @property
    def MaxSpread(self):
        return self._max_spread.Value

    def OnReseted(self):
        super(build_your_grid_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._current_price = 0.0
        self._point_size = 0.0
        self._price_step = 0.0
        self._step_price = 0.0
        self._total_orders = 0
        self._buy_orders = 0
        self._sell_orders = 0
        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_profit = 0.0
        self._sell_profit = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None
        self._first_buy_volume = 0.0
        self._first_sell_volume = 0.0
        self._last_buy_volume = 0.0
        self._last_sell_volume = 0.0
        self._is_hedged = False
        self._cooldown_bars = 0

    def OnStarted2(self, time):
        super(build_your_grid_strategy, self).OnStarted2(time)

        self._point_size = self._calculate_point_size()
        ps = self.Security.PriceStep if self.Security is not None else None
        self._price_step = float(ps) if ps is not None else 0.0

        sp = self.GetSecurityValue[object](Level1Fields.StepPrice)
        self._step_price = float(sp) if sp is not None else 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._current_price = float(candle.ClosePrice)
        if self._current_price <= 0.0:
            return

        self._process_prices()

    def _process_prices(self):
        if self._cooldown_bars > 0:
            self._cooldown_bars -= 1
            return

        self._update_aggregates()

        if self._total_orders > 0:
            if self._should_close_in_profit():
                if self._close_all_positions():
                    self._cooldown_bars = 200
                    return

            if self.LossMode != self.LC_DO_NOTHING and self._should_close_in_loss():
                if self.LossMode == self.LC_CLOSE_FIRST:
                    closed = self._close_first_positions()
                else:
                    closed = self._close_all_positions()
                if closed:
                    self._cooldown_bars = 200
                    return

            if self._should_hedge():
                if self._execute_hedge_order():
                    return

        if self._try_open_initial_orders():
            return

        self._try_open_next_orders()

    def _update_aggregates(self):
        self._total_orders = len(self._long_entries) + len(self._short_entries)
        self._buy_orders = len(self._long_entries)
        self._sell_orders = len(self._short_entries)

        self._total_long_volume = 0.0
        self._total_short_volume = 0.0
        self._buy_profit = 0.0
        self._sell_profit = 0.0
        self._buy_pips = 0.0
        self._sell_pips = 0.0
        self._last_buy_price = None
        self._last_sell_price = None

        for entry in self._long_entries:
            self._total_long_volume += entry[1]
            self._last_buy_price = entry[0]
            diff = self._current_price - entry[0]
            self._buy_profit += self._calculate_profit(diff, entry[1])
            self._buy_pips += self._calculate_pips(diff)

        for entry in self._short_entries:
            self._total_short_volume += entry[1]
            self._last_sell_price = entry[0]
            diff = entry[0] - self._current_price
            self._sell_profit += self._calculate_profit(diff, entry[1])
            self._sell_pips += self._calculate_pips(diff)

        self._first_buy_volume = self._long_entries[0][1] if len(self._long_entries) > 0 else 0.0
        self._first_sell_volume = self._short_entries[0][1] if len(self._short_entries) > 0 else 0.0
        self._last_buy_volume = self._long_entries[-1][1] if len(self._long_entries) > 0 else 0.0
        self._last_sell_volume = self._short_entries[-1][1] if len(self._short_entries) > 0 else 0.0

        self._is_hedged = (self._buy_orders > 1 and self._sell_orders > 1
                           and self._total_long_volume == self._total_short_volume
                           and self._total_long_volume > 0.0)

    def _calculate_profit(self, diff, volume):
        if self._price_step > 0.0 and self._step_price > 0.0:
            return diff / self._price_step * self._step_price * volume
        return diff * volume

    def _calculate_pips(self, diff):
        if self._point_size > 0.0:
            return diff / self._point_size
        return diff

    def _should_close_in_profit(self):
        entries = self._buy_orders + self._sell_orders
        if entries <= 0:
            return False

        if self.CloseTarget == self.CT_PIPS:
            return (self._buy_pips + self._sell_pips) / float(entries) >= float(self.PipsCloseInProfit)
        elif self.CloseTarget == self.CT_CURRENCY:
            return (self._buy_profit + self._sell_profit) >= float(self.CurrencyCloseInProfit)
        return False

    def _should_close_in_loss(self):
        entries = self._buy_orders + self._sell_orders
        if entries <= 0:
            return False
        return (self._buy_pips + self._sell_pips) / float(entries) <= -float(self.PipsForCloseInLoss)

    def _close_all_positions(self):
        closed = False

        if self._buy_orders > 0:
            volume = self._total_long_volume
            self._long_entries = []
            if volume > 0.0:
                self.SellMarket(volume)
                closed = True

        if self._sell_orders > 0:
            volume = self._total_short_volume
            self._short_entries = []
            if volume > 0.0:
                self.BuyMarket(volume)
                closed = True

        return closed

    def _close_first_positions(self):
        closed = False

        if self._buy_orders > 0:
            volume = self._long_entries[0][1]
            self._long_entries.pop(0)
            if volume > 0.0:
                self.SellMarket(volume)
                closed = True

        if self._sell_orders > 0:
            volume = self._short_entries[0][1]
            self._short_entries.pop(0)
            if volume > 0.0:
                self.BuyMarket(volume)
                closed = True

        return closed

    def _should_hedge(self):
        if not self.PlaceHedgeOrder or float(self.HedgeLossThreshold) <= 0.0:
            return False

        portfolio = self.Portfolio
        balance = 0.0
        if portfolio is not None:
            cv = portfolio.CurrentValue
            bv = portfolio.BeginValue
            if cv is not None:
                balance = float(cv)
            elif bv is not None:
                balance = float(bv)

        if balance <= 0.0:
            return False

        floating = self._buy_profit + self._sell_profit
        if floating >= 0.0:
            return False

        loss_percent = abs(floating) * 100.0 / balance
        return loss_percent >= float(self.HedgeLossThreshold) and not self._is_hedged

    def _execute_hedge_order(self):
        imbalance = self._total_long_volume - self._total_short_volume
        if imbalance == 0.0:
            return False

        if imbalance < 0.0:
            volume = self._normalize_volume(abs(imbalance) * float(self.HedgeVolumeMultiplier))
            if volume <= 0.0:
                return False
            self._long_entries.append([self._current_price, volume])
            self.BuyMarket(volume)
            return True

        sell_volume = self._normalize_volume(imbalance * float(self.HedgeVolumeMultiplier))
        if sell_volume <= 0.0:
            return False
        self._short_entries.append([self._current_price, sell_volume])
        self.SellMarket(sell_volume)
        return True

    def _can_open_more_orders(self):
        if self.MaxOrders <= 0:
            return True
        return self._total_orders < self.MaxOrders

    def _try_open_initial_orders(self):
        if not self._can_open_more_orders():
            return False

        if self._buy_orders == 0 and (self.OrderPlacement == self.OP_BOTH or self.OrderPlacement == self.OP_LONG_ONLY):
            volume = self._get_order_volume(Sides.Buy)
            if self._send_market_order(Sides.Buy, volume):
                return True

        if self._sell_orders == 0 and (self.OrderPlacement == self.OP_BOTH or self.OrderPlacement == self.OP_SHORT_ONLY):
            volume = self._get_order_volume(Sides.Sell)
            if self._send_market_order(Sides.Sell, volume):
                return True

        return False

    def _try_open_next_orders(self):
        if not self._can_open_more_orders():
            return

        allow_buy = self.OrderPlacement != self.OP_SHORT_ONLY
        allow_sell = self.OrderPlacement != self.OP_LONG_ONLY

        if not allow_buy and not allow_sell:
            return

        has_buys = self._buy_orders > 0 or self.OrderPlacement == self.OP_SHORT_ONLY
        has_sells = self._sell_orders > 0 or self.OrderPlacement == self.OP_LONG_ONLY

        if not (has_buys and has_sells):
            return

        buy_distance = self._get_next_distance(Sides.Buy) if allow_buy else 0.0
        sell_distance = self._get_next_distance(Sides.Sell) if allow_sell else 0.0

        if self.GridDirection == self.GD_WITH_TREND:
            if allow_buy and self._last_buy_price is not None and buy_distance > 0.0:
                trigger = self._last_buy_price + buy_distance
                if self._current_price >= trigger:
                    volume = self._get_order_volume(Sides.Buy)
                    if self._send_market_order(Sides.Buy, volume):
                        return

            if allow_sell and self._last_sell_price is not None and sell_distance > 0.0:
                trigger = self._last_sell_price - sell_distance
                if self._current_price <= trigger:
                    volume = self._get_order_volume(Sides.Sell)
                    if self._send_market_order(Sides.Sell, volume):
                        return
        else:
            if allow_buy and self._last_buy_price is not None and buy_distance > 0.0:
                trigger = self._last_buy_price - buy_distance
                if self._current_price <= trigger:
                    volume = self._get_order_volume(Sides.Buy)
                    if self._send_market_order(Sides.Buy, volume):
                        return

            if allow_sell and self._last_sell_price is not None and sell_distance > 0.0:
                trigger = self._last_sell_price + sell_distance
                if self._current_price >= trigger:
                    volume = self._get_order_volume(Sides.Sell)
                    if self._send_market_order(Sides.Sell, volume):
                        return

    def _send_market_order(self, side, volume):
        if volume <= 0.0:
            return False

        if side == Sides.Buy:
            self._long_entries.append([self._current_price, volume])
            self.BuyMarket(volume)
        else:
            self._short_entries.append([self._current_price, volume])
            self.SellMarket(volume)

        return True

    def _get_next_distance(self, side):
        base_distance = float(self.PipsForNextOrder)
        count = self._buy_orders if side == Sides.Buy else self._sell_orders

        prog = self.StepProgression
        if prog == self.SP_STATIC:
            multiplier = 1.0
        elif prog == self.SP_GEOMETRIC:
            multiplier = float(max(1, count))
        elif prog == self.SP_EXPONENTIAL:
            if count <= 0:
                multiplier = 1.0
            else:
                multiplier = float(max(1, int(Math.Pow(2, count - 1))))
        else:
            multiplier = 1.0

        return base_distance * multiplier * self._point_size

    def _get_order_volume(self, side):
        base_volume = self._get_base_volume()
        is_buy = side == Sides.Buy
        first_volume = self._first_buy_volume if is_buy else self._first_sell_volume
        last_volume = self._last_buy_volume if is_buy else self._last_sell_volume
        orders = self._buy_orders if is_buy else self._sell_orders

        prog = self.LotProgression
        if prog == self.LP_STATIC:
            if orders == 0:
                result = base_volume
            else:
                result = first_volume if first_volume > 0.0 else base_volume
        elif prog == self.LP_GEOMETRIC:
            if orders == 0:
                result = base_volume
            elif orders == 1:
                result = last_volume * 2.0
            else:
                result = last_volume + (first_volume if first_volume > 0.0 else base_volume)
        elif prog == self.LP_EXPONENTIAL:
            if orders == 0:
                result = base_volume
            else:
                result = last_volume * 2.0
        else:
            result = base_volume

        max_mult = float(self.MaxMultiplierLot)
        if max_mult > 0.0 and orders > 0 and first_volume > 0.0:
            cap = first_volume * max_mult
            if result > cap:
                result = cap

        return self._normalize_volume(result)

    def _get_base_volume(self):
        if self.AutoLotSize:
            portfolio = self.Portfolio
            balance = 0.0
            if portfolio is not None:
                cv = portfolio.CurrentValue
                bv = portfolio.BeginValue
                if cv is not None:
                    balance = float(cv)
                elif bv is not None:
                    balance = float(bv)
            if balance > 0.0:
                volume = balance * float(self.RiskFactor) / 100000.0
            else:
                volume = float(self.ManualLotSize)
        else:
            volume = float(self.ManualLotSize)

        return self._normalize_volume(volume)

    def _normalize_volume(self, volume):
        if volume <= 0.0:
            return 0.0

        sec = self.Security
        min_vol = float(sec.MinVolume) if sec is not None and sec.MinVolume is not None else 0.0
        max_vol = float(sec.MaxVolume) if sec is not None and sec.MaxVolume is not None else 0.0
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0.0

        if step > 0.0:
            volume = round(volume / step) * step

        if min_vol > 0.0 and volume < min_vol:
            volume = min_vol

        if max_vol > 0.0 and volume > max_vol:
            volume = max_vol

        return volume

    def _calculate_point_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0.0:
                return step
        return 0.0001

    def CreateClone(self):
        return build_your_grid_strategy()
