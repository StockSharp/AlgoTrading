import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class rapid_doji_strategy(Strategy):
    def __init__(self):
        super(rapid_doji_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._doji_threshold = self.Param("DojiThreshold", 0.15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rapid_doji_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(rapid_doji_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rapid_doji_strategy()
