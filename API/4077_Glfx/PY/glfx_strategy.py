import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class glfx_strategy(Strategy):
    def __init__(self):
        super(glfx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_upper = self.Param("RsiUpper", 65) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_lower = self.Param("RsiLower", 35) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ma_period = self.Param("MaPeriod", 60) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._signals_repeat = self.Param("SignalsRepeat", 2) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._prev_rsi = 0.0
        self._prev_ma = 0.0
        self._buy_count = 0.0
        self._sell_count = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(glfx_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_ma = 0.0
        self._buy_count = 0.0
        self._sell_count = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(glfx_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return glfx_strategy()
