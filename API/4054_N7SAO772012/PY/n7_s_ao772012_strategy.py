import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class n7_s_ao772012_strategy(Strategy):
    def __init__(self):
        super(n7_s_ao772012_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._ao_period = self.Param("AoPeriod", 5) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._lookback = self.Param("Lookback", 3) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

        self._ao_history = new()
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(n7_s_ao772012_strategy, self).OnReseted()
        self._ao_history = new()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(n7_s_ao772012_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return n7_s_ao772012_strategy()
