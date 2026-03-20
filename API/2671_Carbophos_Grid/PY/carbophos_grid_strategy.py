import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class carbophos_grid_strategy(Strategy):
    """Grid strategy: symmetric grid levels with profit/loss management on aggregated position."""

    def __init__(self):
        super(carbophos_grid_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit Target", "Floating profit target in money", "Risk")
        self._max_loss = self.Param("MaxLoss", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Loss", "Maximum floating loss before closing", "Risk")
        self._step_pips = self.Param("StepPips", 2000) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (pips)", "Distance between grid levels in pips", "Grid")
        self._orders_per_side = self.Param("OrdersPerSide", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Orders Per Side", "Number of pending orders on each side", "Grid")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume for each pending order", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._entry_price = None
        self._grid_center_price = 0.0
        self._grid_placed = False
        self._cooldown_remaining = 0
        self._buy_levels = []
        self._sell_levels = []

    @property
    def ProfitTarget(self):
        return float(self._profit_target.Value)
    @property
    def MaxLoss(self):
        return float(self._max_loss.Value)
    @property
    def StepPips(self):
        return int(self._step_pips.Value)
    @property
    def OrdersPerSide(self):
        return int(self._orders_per_side.Value)
    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_grid_step(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.01
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 2
        multiplier = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        return self.StepPips * step * multiplier

    def OnStarted(self, time):
        super(carbophos_grid_strategy, self).OnStarted(time)

        self._entry_price = None
        self._grid_center_price = 0.0
        self._grid_placed = False
        self._cooldown_remaining = 0
        self._buy_levels = []
        self._sell_levels = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        current_price = float(candle.ClosePrice)

        # Check if any grid levels were hit by this candle
        self._check_grid_fills(candle)

        # Check profit/loss on position
        if self.Position != 0 and self._entry_price is not None:
            floating_pnl = (current_price - self._entry_price) * self.Position

            if floating_pnl >= self.ProfitTarget:
                self._close_all()
                return

            if floating_pnl <= -self.MaxLoss:
                self._close_all()
                return

        # Cooldown after closing
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        # Place grid if none is active
        if not self._grid_placed or (self.Position == 0 and len(self._buy_levels) == 0 and len(self._sell_levels) == 0):
            self._place_grid(current_price)

    def _place_grid(self, center_price):
        self._buy_levels = []
        self._sell_levels = []

        step_size = self._get_grid_step()
        if step_size <= 0 or center_price <= 0:
            return

        for i in range(1, self.OrdersPerSide + 1):
            offset = step_size * i
            buy_price = center_price - offset
            sell_price = center_price + offset

            if buy_price > 0:
                self._buy_levels.append(buy_price)

            self._sell_levels.append(sell_price)

        self._grid_center_price = center_price
        self._grid_placed = True

    def _check_grid_fills(self, candle):
        lo = float(candle.LowPrice)
        h = float(candle.HighPrice)

        # Check buy levels (price goes down to the level)
        i = len(self._buy_levels) - 1
        while i >= 0:
            if i < len(self._buy_levels) and lo <= self._buy_levels[i]:
                level = self._buy_levels[i]
                self.BuyMarket()
                self._update_entry_price(level, self.OrderVolume, True)
                try:
                    self._buy_levels.pop(i)
                except:
                    pass
            i -= 1

        # Check sell levels (price goes up to the level)
        i = len(self._sell_levels) - 1
        while i >= 0:
            if i < len(self._sell_levels) and h >= self._sell_levels[i]:
                level = self._sell_levels[i]
                self.SellMarket()
                self._update_entry_price(level, self.OrderVolume, False)
                try:
                    self._sell_levels.pop(i)
                except:
                    pass
            i -= 1

    def _update_entry_price(self, fill_price, volume, is_buy):
        if self._entry_price is None or self.Position == 0:
            self._entry_price = fill_price
            return

        existing_entry = self._entry_price
        existing_pos = self.Position
        new_pos = existing_pos + volume if is_buy else existing_pos - volume

        if new_pos == 0:
            self._entry_price = None
            return

        # Only update if adding to position in same direction
        if (is_buy and existing_pos > 0) or (not is_buy and existing_pos < 0):
            total_volume = abs(existing_pos) + volume
            self._entry_price = (existing_entry * abs(existing_pos) + fill_price * volume) / total_volume
        else:
            # Reducing position - keep same entry price
            if abs(new_pos) > 0:
                self._entry_price = existing_entry
            else:
                self._entry_price = None

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._buy_levels = []
        self._sell_levels = []
        self._grid_placed = False
        self._entry_price = None
        self._cooldown_remaining = 10

    def OnReseted(self):
        super(carbophos_grid_strategy, self).OnReseted()
        self._entry_price = None
        self._grid_center_price = 0.0
        self._grid_placed = False
        self._cooldown_remaining = 0
        self._buy_levels = []
        self._sell_levels = []

    def CreateClone(self):
        return carbophos_grid_strategy()
