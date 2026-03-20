import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class trade_pad_sample_strategy(Strategy):
    def __init__(self):
        super(trade_pad_sample_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._upper_level = self.Param("UpperLevel", 75) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._lower_level = self.Param("LowerLevel", 25) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_k = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trade_pad_sample_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trade_pad_sample_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return trade_pad_sample_strategy()
