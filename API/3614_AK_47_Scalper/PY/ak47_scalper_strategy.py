import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class ak47_scalper_strategy(Strategy):
    def __init__(self):
        super(ak47_scalper_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._lookback_period = self.Param("LookbackPeriod", 5) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_stop_multiplier = self.Param("AtrStopMultiplier", 1.5) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._atr = None
        self._highest_high = 0.0
        self._lowest_low = 0.0
        self._bars_collected = 0.0
        self._entry_price = None
        self._entry_side = None
        self._stop_distance = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ak47_scalper_strategy, self).OnReseted()
        self._atr = None
        self._highest_high = 0.0
        self._lowest_low = 0.0
        self._bars_collected = 0.0
        self._entry_price = None
        self._entry_side = None
        self._stop_distance = 0.0

    def OnStarted(self, time):
        super(ak47_scalper_strategy, self).OnStarted(time)

        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ak47_scalper_strategy()
