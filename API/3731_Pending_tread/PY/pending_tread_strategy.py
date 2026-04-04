import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from System.Collections.Generic import List
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class pending_tread_strategy(Strategy):
    """
    Pending grid strategy converted from the MetaTrader 4 expert advisor 'Pending_tread'.
    Maintains two independent ladders of limit orders above and below the market with configurable direction and spacing.
    When price reaches a grid level, a market order is placed in the configured direction.
    """

    def __init__(self):
        super(pending_tread_strategy, self).__init__()

        self._pip_step = self.Param("PipStep", 200000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid step (pips)", "Distance between adjacent pending orders expressed in pips", "Trading")

        self._take_profit_pips = self.Param("TakeProfitPips", 150000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take profit (pips)", "Individual take-profit distance assigned to every pending order", "Trading")

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order volume", "Volume sent with each pending order", "Trading")

        self._orders_per_side = self.Param("OrdersPerSide", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Orders per side", "Maximum number of grid levels maintained above and below the anchor", "Trading")

        self._above_market_side = self.Param("AboveMarketSide", Sides.Buy) \
            .SetDisplay("Above market side", "Type of orders triggered above the current price", "Orders")

        self._below_market_side = self.Param("BelowMarketSide", Sides.Sell) \
            .SetDisplay("Below market side", "Type of orders triggered below the current price", "Orders")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle timeframe", "General")

        self._pip_size = 0.0
        self._anchor_price = 0.0
        self._initialized = False
        self._triggered_levels_above = []
        self._triggered_levels_below = []
        self._entry_price = 0.0

    @property
    def PipStep(self):
        return self._pip_step.Value

    @PipStep.setter
    def PipStep(self, value):
        self._pip_step.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def OrdersPerSide(self):
        return self._orders_per_side.Value

    @OrdersPerSide.setter
    def OrdersPerSide(self, value):
        self._orders_per_side.Value = value

    @property
    def AboveMarketSide(self):
        return self._above_market_side.Value

    @AboveMarketSide.setter
    def AboveMarketSide(self, value):
        self._above_market_side.Value = value

    @property
    def BelowMarketSide(self):
        return self._below_market_side.Value

    @BelowMarketSide.setter
    def BelowMarketSide(self, value):
        self._below_market_side.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(pending_tread_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._anchor_price = 0.0
        self._initialized = False
        self._triggered_levels_above = []
        self._triggered_levels_below = []
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(pending_tread_strategy, self).OnStarted2(time)

        self._pip_size = self._get_pip_size()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if not self._initialized:
            self._anchor_price = close
            self._initialized = True
            return

        distance = float(self.PipStep) * self._pip_size
        if distance <= 0:
            return

        tp_offset = float(self.TakeProfitPips) * self._pip_size

        # Check above-market grid levels
        for i in range(1, int(self.OrdersPerSide) + 1):
            level = self._anchor_price + distance * i

            if level in self._triggered_levels_above:
                continue

            if close >= level:
                self._triggered_levels_above.append(level)
                self._execute_grid_order(self.AboveMarketSide, close, tp_offset)
                return  # one order per candle

        # Check below-market grid levels
        for i in range(1, int(self.OrdersPerSide) + 1):
            level = self._anchor_price - distance * i

            if level in self._triggered_levels_below:
                continue

            if close <= level:
                self._triggered_levels_below.append(level)
                self._execute_grid_order(self.BelowMarketSide, close, tp_offset)
                return  # one order per candle

        # Check take-profit for existing position
        self._check_take_profit(close, tp_offset)

    def _execute_grid_order(self, side, price, tp_offset):
        # Close existing opposite position first
        if self.Position != 0:
            if (self.Position > 0 and side == Sides.Sell) or (self.Position < 0 and side == Sides.Buy):
                self._close_position(side)

        vol = float(self.OrderVolume)

        if side == Sides.Buy:
            self.BuyMarket(vol)
            self._entry_price = price
        else:
            self.SellMarket(vol)
            self._entry_price = price

    def _close_position(self, new_side):
        abs_pos = abs(float(self.Position))
        if abs_pos <= 0:
            return

        if self.Position > 0:
            self.SellMarket(abs_pos)
        else:
            self.BuyMarket(abs_pos)

    def _check_take_profit(self, close, tp_offset):
        if self.Position == 0 or self._entry_price == 0 or tp_offset <= 0:
            return

        if self.Position > 0 and close >= self._entry_price + tp_offset:
            self.SellMarket(abs(float(self.Position)))
            self._entry_price = 0

            # Reset grid to re-establish levels around current price
            self._reset_grid(close)
        elif self.Position < 0 and close <= self._entry_price - tp_offset:
            self.BuyMarket(abs(float(self.Position)))
            self._entry_price = 0

            self._reset_grid(close)

    def _reset_grid(self, new_anchor):
        self._anchor_price = new_anchor
        self._triggered_levels_above = []
        self._triggered_levels_below = []

    def _get_pip_size(self):
        security = self.Security
        if security is None:
            return 0.01

        step = security.PriceStep
        if step is not None:
            step = float(step)
            if step > 0:
                return step

        return 0.01

    def CreateClone(self):
        return pending_tread_strategy()
