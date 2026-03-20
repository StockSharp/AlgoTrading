import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class prophet_strategy(Strategy):
    def __init__(self):
        super(prophet_strategy, self).__init__()

        self._x1 = self.Param("X1", 9) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal")
        self._x2 = self.Param("X2", 29) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal")
        self._x3 = self.Param("X3", 94) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal")
        self._x4 = self.Param("X4", 125) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|.", "Signal")

        self._candle1 = None
        self._candle2 = None
        self._candle3 = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(prophet_strategy, self).OnReseted()
        self._candle1 = None
        self._candle2 = None
        self._candle3 = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(prophet_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return prophet_strategy()
