import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class gandalf_pro_projection_strategy(Strategy):
    def __init__(self):
        super(gandalf_pro_projection_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._filter_length = self.Param("FilterLength", 24) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._price_factor = self.Param("PriceFactor", 0.18) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._trend_factor = self.Param("TrendFactor", 0.18) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._close_buffer = new()
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gandalf_pro_projection_strategy, self).OnReseted()
        self._close_buffer = new()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(gandalf_pro_projection_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return gandalf_pro_projection_strategy()
