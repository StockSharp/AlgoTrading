import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_martingale_strategy(Strategy):
    def __init__(self):
        super(rsi_martingale_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI indicator period", "Indicator")
        self._bars_for_condition = self.Param("BarsForCondition", 10) \
            .SetDisplay("RSI Period", "RSI indicator period", "Indicator")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("RSI Period", "RSI indicator period", "Indicator")

        self._recent_rsi = new()
        self._entry_price = 0.0
        self._direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_martingale_strategy, self).OnReseted()
        self._recent_rsi = new()
        self._entry_price = 0.0
        self._direction = 0.0

    def OnStarted(self, time):
        super(rsi_martingale_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

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
        return rsi_martingale_strategy()
