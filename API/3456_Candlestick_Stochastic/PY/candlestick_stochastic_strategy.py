import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class candlestick_stochastic_strategy(Strategy):
    def __init__(self):
        super(candlestick_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_low = self.Param("StochLow", 40) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stoch_high = self.Param("StochHigh", 60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_candle = None
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(candlestick_stochastic_strategy, self).OnReseted()
        self._prev_candle = None
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(candlestick_stochastic_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.stoch_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return candlestick_stochastic_strategy()
