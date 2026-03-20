import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class boll_trade_bollinger_reversion_strategy(Strategy):
    def __init__(self):
        super(boll_trade_bollinger_reversion_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 0.5) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(boll_trade_bollinger_reversion_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(boll_trade_bollinger_reversion_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = self.bollinger_period
        self._bb.Width = self.bollinger_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return boll_trade_bollinger_reversion_strategy()
