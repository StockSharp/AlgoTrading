import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class carbophos_grid_strategy(Strategy):
    def __init__(self):
        super(carbophos_grid_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 500.0)
        self._max_loss = self.Param("MaxLoss", 100.0)
        self._step_pips = self.Param("StepPips", 2000)
        self._orders_per_side = self.Param("OrdersPerSide", 1)
        self._order_volume = self.Param("OrderVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._entry_price = None
        self._grid_center_price = 0.0
        self._grid_placed = False
        self._cooldown_remaining = 0
        self._buy_levels = []
        self._sell_levels = []

    @property
    def ProfitTarget(self):
        return self._profit_target.Value

    @property
    def MaxLoss(self):
        return self._max_loss.Value

    @property
    def StepPips(self):
        return self._step_pips.Value

    @property
    def OrdersPerSide(self):
        return self._orders_per_side.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(carbophos_grid_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        current_price = float(candle.ClosePrice)
        self._check_grid_fills(candle)

        if self.Position != 0 and self._entry_price is not None:
            floating_pnl = (current_price - self._entry_price) * float(self.Position)
            if floating_pnl >= self.ProfitTarget:
                self._close_all("Profit target reached.")
                return
            if floating_pnl <= -self.MaxLoss:
                self._close_all("Maximum loss reached.")
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

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
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        i = len(self._buy_levels) - 1
        while i >= 0 and i < len(self._buy_levels):
            if low <= self._buy_levels[i]:
                level = self._buy_levels[i]
                self.BuyMarket()
                self._update_entry_price(level, self.OrderVolume, True)
                self._buy_levels.pop(i)
            i -= 1

        i = len(self._sell_levels) - 1
        while i >= 0 and i < len(self._sell_levels):
            if high >= self._sell_levels[i]:
                level = self._sell_levels[i]
                self.SellMarket()
                self._update_entry_price(level, self.OrderVolume, False)
                self._sell_levels.pop(i)
            i -= 1

    def _update_entry_price(self, fill_price, volume, is_buy):
        if self._entry_price is None or self.Position == 0:
            self._entry_price = fill_price
            return

        existing_entry = self._entry_price
        existing_pos = float(self.Position)
        new_pos = existing_pos + volume if is_buy else existing_pos - volume

        if new_pos == 0:
            self._entry_price = None
            return

        if (is_buy and existing_pos > 0) or (not is_buy and existing_pos < 0):
            total_volume = abs(existing_pos) + volume
            self._entry_price = (existing_entry * abs(existing_pos) + fill_price * volume) / total_volume
        else:
            if abs(new_pos) > 0:
                self._entry_price = existing_entry
            else:
                self._entry_price = None

    def _close_all(self, reason):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._buy_levels = []
        self._sell_levels = []
        self._grid_placed = False
        self._entry_price = None
        self._cooldown_remaining = 10
        self.LogInfo(reason)

    def _get_grid_step(self):
        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if price_step <= 0:
            price_step = 0.01
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 2
        multiplier = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        return self.StepPips * price_step * multiplier

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
