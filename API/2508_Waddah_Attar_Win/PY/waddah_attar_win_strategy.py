import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class waddah_attar_win_strategy(Strategy):
    """Grid strategy that places symmetric market orders around grid levels and takes profit when equity target is reached."""

    def __init__(self):
        super(waddah_attar_win_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 500) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (Points)", "Distance between grid levels in price steps", "General")

        self._first_volume = self.Param("FirstVolume", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("First Volume", "Volume for grid orders", "General")

        self._increment_volume = self.Param("IncrementVolume", 0) \
            .SetDisplay("Increment Volume", "Additional volume on subsequent grid entries", "General")

        self._min_profit = self.Param("MinProfit", 200) \
            .SetNotNegative() \
            .SetDisplay("Min Profit", "Price movement profit target to close position", "Risk")

        self._grid_origin = 0.0
        self._last_grid_index = 0
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._initialized = False
        self._total_orders = 0

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

    def OnStarted2(self, time):
        super(waddah_attar_win_strategy, self).OnStarted2(time)

        tf = DataType.TimeFrame(TimeSpan.FromMinutes(5))

        self.SubscribeCandles(tf) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        # cap total orders to avoid exceeding limits
        if self._total_orders >= 80:
            return

        close = float(candle.ClosePrice)

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        if price_step <= 0:
            price_step = 0.01

        step_offset = float(self.StepPoints) * price_step
        if step_offset <= 0:
            return

        # initialize grid origin on first candle
        if not self._initialized:
            self._grid_origin = close
            self._last_grid_index = 0
            self._current_volume = float(self.FirstVolume)
            self._initialized = True
            return

        # calculate which grid index the price is at
        grid_index = int(math.floor((close - self._grid_origin) / step_offset))

        # check profit target: close position if in profit
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl = close - self._entry_price
            else:
                pnl = self._entry_price - close

            if pnl >= float(self.MinProfit) * price_step:
                if self.Position > 0:
                    self.SellMarket()
                    self._total_orders += 1
                else:
                    self.BuyMarket()
                    self._total_orders += 1

                # reset grid around current price
                self._grid_origin = close
                self._last_grid_index = 0
                self._current_volume = float(self.FirstVolume)
                self._entry_price = 0.0
                return

        # price crossed to a new grid level
        if grid_index != self._last_grid_index:
            if grid_index > self._last_grid_index:
                # price moved up - buy
                if self.Position < 0:
                    # close short first
                    self.BuyMarket()
                    self._total_orders += 1
                    self._entry_price = close
                    self._grid_origin = close
                    self._last_grid_index = 0
                    self._current_volume = float(self.FirstVolume)
                else:
                    self.BuyMarket()
                    self._total_orders += 1
                    if self.Position <= 0:
                        self._entry_price = close
                    self._current_volume = self._current_volume + float(self.IncrementVolume)
            else:
                # price moved down - sell
                if self.Position > 0:
                    # close long first
                    self.SellMarket()
                    self._total_orders += 1
                    self._entry_price = close
                    self._grid_origin = close
                    self._last_grid_index = 0
                    self._current_volume = float(self.FirstVolume)
                else:
                    self.SellMarket()
                    self._total_orders += 1
                    if self.Position >= 0:
                        self._entry_price = close
                    self._current_volume = self._current_volume + float(self.IncrementVolume)

            self._last_grid_index = grid_index

    def OnReseted(self):
        super(waddah_attar_win_strategy, self).OnReseted()
        self._grid_origin = 0.0
        self._last_grid_index = 0
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._initialized = False
        self._total_orders = 0

    def CreateClone(self):
        return waddah_attar_win_strategy()
