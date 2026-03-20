import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_session_reversal_strategy(Strategy):
    def __init__(self):
        super(bollinger_bands_session_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Candle Type", "Primary candle series", "General")
        self._bollinger_width = self.Param("BollingerWidth", 2.0) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_bands_session_reversal_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(bollinger_bands_session_reversal_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_length
        self._bollinger.Width = self.bollinger_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bollinger_bands_session_reversal_strategy()
