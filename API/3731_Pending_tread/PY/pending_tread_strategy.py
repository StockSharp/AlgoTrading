import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class pending_tread_strategy(Strategy):
    """Grid strategy that places market orders at regular pip intervals above and below market price."""

    def __init__(self):
        super(pending_tread_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for price tracking", "General")
        self._pip_step = self.Param("PipStep", 12.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid step (pips)", "Distance between adjacent orders expressed in pips", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take profit (pips)", "Take-profit distance assigned to every order", "Trading")
        self._order_volume = self.Param("OrderVolume", 0.01) \
            .SetGreaterThanZero() \
            .SetDisplay("Order volume", "Volume sent with each order", "Trading")
        self._orders_per_side = self.Param("OrdersPerSide", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Orders per side", "Maximum number of orders maintained above and below the market", "Trading")

        self._pip_size = 0.0
        self._point_value = 0.0
        self._last_grid_price = None
        self._buy_entries = []
        self._sell_entries = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PipStep(self):
        return self._pip_step.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def OrdersPerSide(self):
        return self._orders_per_side.Value

    def OnReseted(self):
        super(pending_tread_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._point_value = 0.0
        self._last_grid_price = None
        self._buy_entries = []
        self._sell_entries = []

    def OnStarted(self, time):
        super(pending_tread_strategy, self).OnStarted(time)

        self._point_value = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._point_value = ps

        self._pip_size = self._get_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _get_pip_size(self):
        if self.Security is None:
            return 0.0001

        step = self._point_value
        decimals = self.Security.Decimals if self.Security.Decimals is not None else 0

        if decimals >= 4:
            return step * 10.0

        return step if step > 0 else 0.0001

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        self._check_take_profits(candle)

        if self._last_grid_price is None:
            self._last_grid_price = close
            return

        distance = float(self.PipStep) * self._pip_size
        if distance <= 0:
            return

        price_move = close - self._last_grid_price

        if abs(price_move) >= distance:
            volume = float(self.OrderVolume)
            if volume <= 0:
                return

            tp_offset = float(self.TakeProfitPips) * self._pip_size

            if price_move > 0:
                self.BuyMarket(volume)
                tp = close + tp_offset if tp_offset > 0 else None
                self._buy_entries.append([close, volume, tp])
            else:
                self.SellMarket(volume)
                tp = close - tp_offset if tp_offset > 0 else None
                self._sell_entries.append([close, volume, tp])

            self._last_grid_price = close

    def _check_take_profits(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        new_buys = []
        for entry in self._buy_entries:
            if entry[2] is not None and high >= entry[2]:
                self.SellMarket(entry[1])
            else:
                new_buys.append(entry)
        self._buy_entries = new_buys

        new_sells = []
        for entry in self._sell_entries:
            if entry[2] is not None and low <= entry[2]:
                self.BuyMarket(entry[1])
            else:
                new_sells.append(entry)
        self._sell_entries = new_sells

    def CreateClone(self):
        return pending_tread_strategy()
