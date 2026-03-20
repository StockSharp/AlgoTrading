import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class meeting_lines_stochastic_strategy(Strategy):
    def __init__(self):
        super(meeting_lines_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_low = self.Param("StochLow", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_high = self.Param("StochHigh", 70) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_candle = None
        self._prev_prev_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(meeting_lines_stochastic_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_prev_candle = None

    def OnStarted(self, time):
        super(meeting_lines_stochastic_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return meeting_lines_stochastic_strategy()
