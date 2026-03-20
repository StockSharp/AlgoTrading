import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class simplest_de_marker_strategy(Strategy):
    def __init__(self):
        super(simplest_de_marker_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._oversold = self.Param("Oversold", 0.2) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._overbought = self.Param("Overbought", 0.8) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_value = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simplest_de_marker_strategy, self).OnReseted()
        self._prev_value = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(simplest_de_marker_strategy, self).OnStarted(time)

        self._demarker = DeMarker()
        self._demarker.Length = self.demarker_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._demarker, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return simplest_de_marker_strategy()
