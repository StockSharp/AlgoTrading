import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class macd_stochastic_trailing_strategy(Strategy):
    def __init__(self):
        super(macd_stochastic_trailing_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_k = None
        self._prev_d = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_stochastic_trailing_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted(self, time):
        super(macd_stochastic_trailing_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, stoch, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_stochastic_trailing_strategy()
