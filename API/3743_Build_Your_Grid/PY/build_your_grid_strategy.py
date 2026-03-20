import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class build_your_grid_strategy(Strategy):
    def __init__(self):
        super(build_your_grid_strategy, self).__init__()

        self._order_placement = self.Param("OrderPlacement", OrderPlacementModes.Both) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._grid_direction = self.Param("GridDirection", GridDirectionModes.AgainstTrend) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._pips_for_next_order = self.Param("PipsForNextOrder", 50) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._step_progression = self.Param("StepProgression", StepProgressionModes.Geometric) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._close_target_mode = self.Param("CloseTarget", CloseTargetModes.Pips) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._pips_close_in_profit = self.Param("PipsCloseInProfit", 10) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._currency_close_in_profit = self.Param("CurrencyCloseInProfit", 10) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._loss_close_mode = self.Param("LossMode", LossCloseModes.CloseAll) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._pips_for_close_in_loss = self.Param("PipsForCloseInLoss", 100) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._place_hedge_order = self.Param("PlaceHedgeOrder", False) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._hedge_loss_threshold = self.Param("HedgeLossThreshold", 10) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._hedge_volume_multiplier = self.Param("HedgeVolumeMultiplier", 1) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._auto_lot_size = self.Param("AutoLotSize", False) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._risk_factor = self.Param("RiskFactor", 1) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._manual_lot_size = self.Param("ManualLotSize", 0.01) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._lot_progression = self.Param("LotProgression", LotProgressionModes.Static) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._max_multiplier_lot = self.Param("MaxMultiplierLot", 50) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._max_orders = self.Param("MaxOrders", 0) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._max_spread = self.Param("MaxSpread", 0) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._use_completed_bar = self.Param("UseCompletedBar", False) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Order Placement", "Allowed entry direction", "General")

        self._long_entries = new()
        self._short_entries = new()
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._has_best_bid = False
        self._has_best_ask = False
        self._point_size = 0.0
        self._price_step = 0.0
        self._step_price = 0.0
        self._bar_ready = False
        self._total_orders = 0.0
        self._buy_orders = 0.0
        self._sell_orders = 0.0
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

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(build_your_grid_strategy, self).OnReseted()
        self._long_entries = new()
        self._short_entries = new()
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._has_best_bid = False
        self._has_best_ask = False
        self._point_size = 0.0
        self._price_step = 0.0
        self._step_price = 0.0
        self._bar_ready = False
        self._total_orders = 0.0
        self._buy_orders = 0.0
        self._sell_orders = 0.0
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

    def OnStarted(self, time):
        super(build_your_grid_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return build_your_grid_strategy()
