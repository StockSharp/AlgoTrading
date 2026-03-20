import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy


class abh_bh_mfi_strategy(Strategy):
    def __init__(self):
        super(abh_bh_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._mfi_period = self.Param("MfiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._oversold = self.Param("Oversold", 40) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._overbought = self.Param("Overbought", 60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._candles = new()
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(abh_bh_mfi_strategy, self).OnReseted()
        self._candles = new()
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(abh_bh_mfi_strategy, self).OnStarted(time)

        self._mfi = MoneyFlowIndex()
        self._mfi.Length = self.mfi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._mfi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return abh_bh_mfi_strategy()
