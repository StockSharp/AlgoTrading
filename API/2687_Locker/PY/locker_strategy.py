import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class locker_strategy(Strategy):
    """Hedging grid locker: opens initial position, hedges on drawdown, closes at profit target."""

    def __init__(self):
        super(locker_strategy, self).__init__()

        self._profit_target_percent = self.Param("ProfitTargetPercent", 0.001) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit %", "Target profit percent of balance", "General")
        self._start_volume = self.Param("StartVolume", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Start Volume", "Initial trade volume", "General")
        self._step_volume = self.Param("StepVolume", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("Step Volume", "Volume for subsequent trades", "General")
        self._step_points = self.Param("StepPoints", 15000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Step Points", "Number of price steps between new trades", "General")
        self._enable_automation = self.Param("EnableAutomation", True) \
            .SetDisplay("Enable Automation", "Allow the strategy to place trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "Data")
        self._max_open_positions = self.Param("MaxOpenPositions", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Open Positions", "Maximum number of hedged legs allowed", "Risk")

        # entries: list of (side, price, volume) tuples; side='buy' or 'sell'
        self._entries = []
        self._realized_pnl = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None
        self._cooldown = 0

    @property
    def ProfitTargetPercent(self):
        return float(self._profit_target_percent.Value)
    @property
    def StartVolume(self):
        return float(self._start_volume.Value)
    @property
    def StepVolume(self):
        return float(self._step_volume.Value)
    @property
    def StepPoints(self):
        return float(self._step_points.Value)
    @property
    def EnableAutomation(self):
        return self._enable_automation.Value
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def MaxOpenPositions(self):
        return int(self._max_open_positions.Value)

    def OnStarted(self, time):
        super(locker_strategy, self).OnStarted(time)

        self._entries = []
        self._realized_pnl = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.EnableAutomation:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close_price = float(candle.ClosePrice)
        bid = close_price
        ask = close_price

        current_profit = self._realized_pnl + self._calc_unrealized(bid, ask)
        open_count = len(self._entries)

        if open_count == 0:
            self._open_position('buy', self.StartVolume, ask)
            return

        if open_count >= self.MaxOpenPositions and self._try_close_pair(bid, ask):
            return

        portfolio_value = 1000000.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            pv = float(self.Portfolio.CurrentValue)
            if pv > 0:
                portfolio_value = pv

        target_profit = portfolio_value * self.ProfitTargetPercent

        if target_profit > 0 and current_profit >= target_profit:
            self._close_all(bid, ask)
            self._cooldown = 20
            return

        if target_profit <= 0:
            return

        if current_profit <= -target_profit:
            last_price = self._last_entry_price
            if last_price == 0:
                return

            step_distance = self._get_step_distance()
            if step_distance <= 0:
                return

            if ask > last_price + step_distance:
                self._open_position('sell', self.StepVolume, ask)
            elif bid < last_price - step_distance:
                self._open_position('buy', self.StepVolume, bid)

    def _calc_unrealized(self, bid, ask):
        profit = 0.0
        for side, price, volume in self._entries:
            exit_price = bid if side == 'buy' else ask
            direction = 1.0 if side == 'buy' else -1.0
            profit += (exit_price - price) * direction * volume
        return profit

    def _try_close_pair(self, bid, ask):
        buy_index = -1
        sell_index = -1

        for i in range(len(self._entries)):
            side = self._entries[i][0]
            if side == 'buy' and buy_index == -1:
                buy_index = i
            elif side == 'sell' and sell_index == -1:
                sell_index = i
            if buy_index != -1 and sell_index != -1:
                break

        if buy_index == -1 or sell_index == -1:
            return False

        if buy_index > sell_index:
            self._close_entry(buy_index, bid, ask)
            self._close_entry(sell_index, bid, ask)
        else:
            self._close_entry(sell_index, bid, ask)
            self._close_entry(buy_index, bid, ask)

        self._update_last_entry()
        return True

    def _close_all(self, bid, ask):
        while len(self._entries) > 0:
            self._close_entry(len(self._entries) - 1, bid, ask)
        self._update_last_entry()

    def _close_entry(self, index, bid, ask):
        if index < 0 or index >= len(self._entries):
            return

        side, price, volume = self._entries[index]
        exit_price = bid if side == 'buy' else ask

        if side == 'buy':
            self.SellMarket()
        else:
            self.BuyMarket()

        direction = 1.0 if side == 'buy' else -1.0
        pnl = (exit_price - price) * direction * volume
        self._realized_pnl += pnl

        self._entries.pop(index)

    def _open_position(self, side, volume, price):
        if volume <= 0:
            return

        if side == 'buy':
            self.BuyMarket()
        else:
            self.SellMarket()

        self._entries.append((side, price, volume))
        self._last_entry_price = price
        self._last_entry_side = side

    def _get_step_distance(self):
        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if price_step > 0:
            return self.StepPoints * price_step
        return self.StepPoints

    def _update_last_entry(self):
        if len(self._entries) == 0:
            self._last_entry_price = 0.0
            self._last_entry_side = None
            return
        side, price, volume = self._entries[-1]
        self._last_entry_price = price
        self._last_entry_side = side

    def OnReseted(self):
        super(locker_strategy, self).OnReseted()
        self._entries = []
        self._realized_pnl = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None
        self._cooldown = 0

    def CreateClone(self):
        return locker_strategy()
