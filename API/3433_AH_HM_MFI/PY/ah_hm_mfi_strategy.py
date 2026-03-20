import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ah_hm_mfi_strategy(Strategy):
    def __init__(self):
        super(ah_hm_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mfi_period = self.Param("MfiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mfi_low = self.Param("MfiLow", 35) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mfi_high = self.Param("MfiHigh", 65) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 8) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ah_hm_mfi_strategy, self).OnReseted()
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(ah_hm_mfi_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.mfi_period

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
        return ah_hm_mfi_strategy()
