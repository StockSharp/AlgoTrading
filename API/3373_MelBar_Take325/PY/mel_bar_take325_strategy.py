import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mel_bar_take325_strategy(Strategy):
    def __init__(self):
        super(mel_bar_take325_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._sma_period = self.Param("SmaPeriod", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_exit_level = self.Param("RsiExitLevel", 75) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 8) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._has_prev2 = False
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mel_bar_take325_strategy, self).OnReseted()
        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._has_prev2 = False
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(mel_bar_take325_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mel_bar_take325_strategy()
