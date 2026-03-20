import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class very_blondie_system_strategy(Strategy):
    def __init__(self):
        super(very_blondie_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._period_length = self.Param("PeriodLength", 30) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(very_blondie_system_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(very_blondie_system_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.period_length
        self._lowest = Lowest()
        self._lowest.Length = self.period_length
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.period_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return very_blondie_system_strategy()
