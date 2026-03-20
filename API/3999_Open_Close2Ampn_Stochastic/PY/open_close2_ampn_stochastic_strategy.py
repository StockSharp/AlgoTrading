import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class open_close2_ampn_stochastic_strategy(Strategy):
    def __init__(self):
        super(open_close2_ampn_stochastic_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._maximum_risk = self.Param("MaximumRisk", 0.3) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._decrease_factor = self.Param("DecreaseFactor", 100) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._minimum_volume = self.Param("MinimumVolume", 0.1) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._stochastic_length = self.Param("StochasticLength", 9) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._stochastic_k_length = self.Param("StochasticKLength", 3) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._stochastic_d_length = self.Param("StochasticDLength", 3) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Money Management")

        self._stochastic = null!
        self._previous_open = None
        self._previous_close = None
        self._average_entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_direction = 0.0
        self._loss_streak = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(open_close2_ampn_stochastic_strategy, self).OnReseted()
        self._stochastic = null!
        self._previous_open = None
        self._previous_close = None
        self._average_entry_price = 0.0
        self._entry_volume = 0.0
        self._entry_direction = 0.0
        self._loss_streak = 0.0

    def OnStarted(self, time):
        super(open_close2_ampn_stochastic_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return open_close2_ampn_stochastic_strategy()
