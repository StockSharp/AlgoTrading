import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ntk07_strategy(Strategy):
    def __init__(self):
        super(ntk07_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._grid_multiplier = self.Param("GridMultiplier", 1.5) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

        self._reference_price = 0.0
        self._entry_price = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ntk07_strategy, self).OnReseted()
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(ntk07_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length
        self._ema = ExponentialMovingAverage()
        self._ema.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ntk07_strategy()
