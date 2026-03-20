import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class zs1_forex_instruments_strategy(Strategy):
    def __init__(self):
        super(zs1_forex_instruments_strategy, self).__init__()

        self._orders_space_pips = self.Param("OrdersSpacePips", 50) \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")
        self._pk_pips = self.Param("PkPips", 10) \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")
        self._volume_tolerance = self.Param("VolumeTolerance", 0.0000001) \
            .SetDisplay("Orders Space (pips)", "Distance between successive grid levels.", "Trading")

        self._order_intents = new()
        self._long_entries = new()
        self._short_entries = new()
        self._pip_value = 0.0
        self._first_price = 0.0
        self._zone = 0.0
        self._last_zone = 0.0
        self._zone_changed = False
        self._first_stage = 0.0
        self._first_order_direction = None
        self._last_order_direction = None
        self._is_closing_all = False
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._has_best_bid = False
        self._has_best_ask = False

    def OnReseted(self):
        super(zs1_forex_instruments_strategy, self).OnReseted()
        self._order_intents = new()
        self._long_entries = new()
        self._short_entries = new()
        self._pip_value = 0.0
        self._first_price = 0.0
        self._zone = 0.0
        self._last_zone = 0.0
        self._zone_changed = False
        self._first_stage = 0.0
        self._first_order_direction = None
        self._last_order_direction = None
        self._is_closing_all = False
        self._best_bid = 0.0
        self._best_ask = 0.0
        self._has_best_bid = False
        self._has_best_ask = False

    def OnStarted(self, time):
        super(zs1_forex_instruments_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return zs1_forex_instruments_strategy()
