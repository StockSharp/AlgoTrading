import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class stochastic_martingale_grid_strategy(Strategy):
    def __init__(self):
        super(stochastic_martingale_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 20) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._max_orders = self.Param("MaxOrders", 2) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._step_pips = self.Param("StepPips", 7) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._slowing = self.Param("Slowing", 3) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._zone_buy = self.Param("ZoneBuy", 50) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._zone_sell = self.Param("ZoneSell", 50) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")

        self._entries = None
        self._stochastic = None
        self._previous_main = None
        self._previous_signal = None
        self._pip_size = 0.0
        self._last_entry_volume = 0.0
        self._last_entry_price = 0.0
        self._current_side = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_martingale_grid_strategy, self).OnReseted()
        self._entries = None
        self._stochastic = None
        self._previous_main = None
        self._previous_signal = None
        self._pip_size = 0.0
        self._last_entry_volume = 0.0
        self._last_entry_price = 0.0
        self._current_side = None

    def OnStarted(self, time):
        super(stochastic_martingale_grid_strategy, self).OnStarted(time)


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
        return stochastic_martingale_grid_strategy()
