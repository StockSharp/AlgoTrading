import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class billy_expert_reversal_strategy(Strategy):
    def __init__(self):
        super(billy_expert_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

        self._bar_count = 0.0
        self._prev_rsi = 0.0
        self._has_prev_rsi = False
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(billy_expert_reversal_strategy, self).OnReseted()
        self._bar_count = 0.0
        self._prev_rsi = 0.0
        self._has_prev_rsi = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(billy_expert_reversal_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

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
        return billy_expert_reversal_strategy()
