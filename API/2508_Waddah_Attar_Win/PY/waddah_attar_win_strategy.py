import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType
from StockSharp.Algo.Strategies import Strategy


class waddah_attar_win_strategy(Strategy):
    """Grid strategy placing symmetric limit orders around current price, pyramiding when price approaches."""

    def __init__(self):
        super(waddah_attar_win_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (Points)", "Distance from market price to pending orders in points", "General")

        self._first_volume = self.Param("FirstVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("First Volume", "Volume for the initial pending orders", "General")

        self._increment_volume = self.Param("IncrementVolume", 0.0) \
            .SetDisplay("Increment Volume", "Additional volume applied to subsequent grid orders", "General")

        self._min_profit = self.Param("MinProfit", 910.0) \
            .SetNotNegative() \
            .SetDisplay("Min Profit", "Required equity increase to close all trades", "Risk")

        self._best_bid = None
        self._best_ask = None
        self._last_buy_limit_price = 0.0
        self._last_sell_limit_price = 0.0
        self._last_buy_limit_volume = 0.0
        self._last_sell_limit_volume = 0.0
        self._reference_balance = 0.0
        self._has_initial_orders = False

    @property
    def StepPoints(self):
        return self._step_points.Value

    @property
    def FirstVolume(self):
        return self._first_volume.Value

    @property
    def IncrementVolume(self):
        return self._increment_volume.Value

    @property
    def MinProfit(self):
        return self._min_profit.Value

    def _normalize_price(self, price):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) == 0:
            return price
        step = float(sec.PriceStep)
        steps = round(price / step)
        return steps * step

    def OnStarted(self, time):
        super(waddah_attar_win_strategy, self).OnStarted(time)

        portfolio = self.Portfolio
        self._reference_balance = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0

        self.SubscribeOrderBook() \
            .Bind(self.process_order_book) \
            .Start()

    def process_order_book(self, depth):
        bids = depth.Bids
        if bids is not None and len(bids) > 0:
            self._best_bid = float(bids[0].Price)

        asks = depth.Asks
        if asks is not None and len(asks) > 0:
            self._best_ask = float(asks[0].Price)

        self._process_trading()

    def _process_trading(self):
        if not self.IsFormed:
            return

        if self._best_bid is None or self._best_ask is None:
            return

        bid = self._best_bid
        ask = self._best_ask

        portfolio = self.Portfolio
        equity = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0

        if float(self.MinProfit) > 0 and equity >= self._reference_balance + float(self.MinProfit) and (self._has_initial_orders or self.Position != 0):
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._reset_order_info()
            self._has_initial_orders = False
            self._reference_balance = equity
            return

        if not self._has_initial_orders and self.Position == 0:
            self._reference_balance = equity
            self._place_initial(bid, ask)
            return

        if not self._has_initial_orders:
            return

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0001
        if price_step <= 0:
            return

        step_offset = float(self.StepPoints) * price_step
        if step_offset <= 0:
            return

        proximity = price_step * 5.0

        if self._last_buy_limit_price > 0 and ask - self._last_buy_limit_price <= proximity:
            self._place_add_buy(bid, step_offset)

        if self._last_sell_limit_price > 0 and self._last_sell_limit_price - bid <= proximity:
            self._place_add_sell(ask, step_offset)

    def _place_initial(self, bid, ask):
        if float(self.FirstVolume) <= 0:
            return

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0001
        if price_step <= 0:
            return

        step_offset = float(self.StepPoints) * price_step
        if step_offset <= 0:
            return

        buy_price = self._normalize_price(bid - step_offset)
        sell_price = self._normalize_price(ask + step_offset)

        any_placed = False

        buy_order = self.BuyLimit(buy_price, float(self.FirstVolume))
        if buy_order is not None:
            self._last_buy_limit_price = buy_price
            self._last_buy_limit_volume = float(self.FirstVolume)
            any_placed = True

        sell_order = self.SellLimit(sell_price, float(self.FirstVolume))
        if sell_order is not None:
            self._last_sell_limit_price = sell_price
            self._last_sell_limit_volume = float(self.FirstVolume)
            any_placed = True

        if any_placed:
            self._has_initial_orders = True

    def _place_add_buy(self, bid, step_offset):
        volume = self._last_buy_limit_volume + float(self.IncrementVolume)
        if volume <= 0:
            return
        price = self._normalize_price(bid - step_offset)
        if price <= 0:
            return
        order = self.BuyLimit(price, volume)
        if order is not None:
            self._last_buy_limit_price = price
            self._last_buy_limit_volume = volume

    def _place_add_sell(self, ask, step_offset):
        volume = self._last_buy_limit_volume + float(self.IncrementVolume)
        if volume <= 0:
            return
        price = self._normalize_price(ask + step_offset)
        if price <= 0:
            return
        order = self.SellLimit(price, volume)
        if order is not None:
            self._last_sell_limit_price = price
            self._last_sell_limit_volume = volume

    def _reset_order_info(self):
        self._last_buy_limit_price = 0.0
        self._last_sell_limit_price = 0.0
        self._last_buy_limit_volume = 0.0
        self._last_sell_limit_volume = 0.0

    def OnReseted(self):
        super(waddah_attar_win_strategy, self).OnReseted()
        self._best_bid = None
        self._best_ask = None
        self._reset_order_info()
        self._reference_balance = 0.0
        self._has_initial_orders = False

    def CreateClone(self):
        return waddah_attar_win_strategy()
