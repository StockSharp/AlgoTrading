import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class stairs_strategy(Strategy):
    def __init__(self):
        super(stairs_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._grid_multiplier = self.Param("GridMultiplier", 1.5) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._max_layers = self.Param("MaxLayers", 5) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._profit_multiplier = self.Param("ProfitMultiplier", 2.0) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._entry_price = 0.0
        self._last_grid_price = 0.0
        self._grid_count = 0.0
        self._prev_ema = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stairs_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._last_grid_price = 0.0
        self._grid_count = 0.0
        self._prev_ema = 0.0

    def OnStarted(self, time):
        super(stairs_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

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
        return stairs_strategy()
