import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class waddah_attar_win_grid_strategy(Strategy):
    def __init__(self):
        super(waddah_attar_win_grid_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 1500) \
            .SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
        self._first_volume = self.Param("FirstVolume", 0.1) \
            .SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
        self._increment_volume = self.Param("IncrementVolume", 0) \
            .SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
        self._min_profit = self.Param("MinProfit", 450) \
            .SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")

        self._last_buy_grid_price = 0.0
        self._last_sell_grid_price = 0.0
        self._current_buy_volume = 0.0
        self._current_sell_volume = 0.0
        self._reference_balance = 0.0
        self._grid_active = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(waddah_attar_win_grid_strategy, self).OnReseted()
        self._last_buy_grid_price = 0.0
        self._last_sell_grid_price = 0.0
        self._current_buy_volume = 0.0
        self._current_sell_volume = 0.0
        self._reference_balance = 0.0
        self._grid_active = False

    def OnStarted(self, time):
        super(waddah_attar_win_grid_strategy, self).OnStarted(time)


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
        return waddah_attar_win_grid_strategy()
