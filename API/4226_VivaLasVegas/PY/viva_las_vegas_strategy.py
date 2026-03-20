import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class viva_las_vegas_strategy(Strategy):
    def __init__(self):
        super(viva_las_vegas_strategy, self).__init__()

        self._stop_take_pips = self.Param("StopTakePips", 50) \
            .SetDisplay("Stop/take distance", "Protective distance expressed in pips for both stop-loss and take-profit.", "Risk")
        self._base_volume = self.Param("BaseVolume", 1) \
            .SetDisplay("Stop/take distance", "Protective distance expressed in pips for both stop-loss and take-profit.", "Risk")
        self._money_management_mode = self.Param("MoneyManagement", MoneyManagementModes.Martingale) \
            .SetDisplay("Stop/take distance", "Protective distance expressed in pips for both stop-loss and take-profit.", "Risk")
        self._seed = self.Param("Seed", 0) \
            .SetDisplay("Stop/take distance", "Protective distance expressed in pips for both stop-loss and take-profit.", "Risk")

        self._active_seed = 0.0
        self._management = None
        self._previous_position = 0.0
        self._last_realized_pn_l = 0.0
        self._order_in_flight = False
        self._next_volume = 0.0
        self._next_volume = 0.0
        self._series = new()
        self._next_volume = 0.0
        self._current_result = 0.0
        self._index = 0.0
        self._double_up = False

    def OnReseted(self):
        super(viva_las_vegas_strategy, self).OnReseted()
        self._active_seed = 0.0
        self._management = None
        self._previous_position = 0.0
        self._last_realized_pn_l = 0.0
        self._order_in_flight = False
        self._next_volume = 0.0
        self._next_volume = 0.0
        self._series = new()
        self._next_volume = 0.0
        self._current_result = 0.0
        self._index = 0.0
        self._double_up = False

    def OnStarted(self, time):
        super(viva_las_vegas_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return viva_las_vegas_strategy()
