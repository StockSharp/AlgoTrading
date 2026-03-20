import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class morning_evening_stochastic_strategy(Strategy):
    def __init__(self):
        super(morning_evening_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._oversold = self.Param("Oversold", 30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._overbought = self.Param("Overbought", 70) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._candles = new()
        self._prev_k = 0.0
        self._has_prev_k = False
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(morning_evening_stochastic_strategy, self).OnReseted()
        self._candles = new()
        self._prev_k = 0.0
        self._has_prev_k = False
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(morning_evening_stochastic_strategy, self).OnStarted(time)


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
        return morning_evening_stochastic_strategy()
