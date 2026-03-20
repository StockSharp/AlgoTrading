import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class com_fracti_fractal_rsi_strategy(Strategy):
    def __init__(self):
        super(com_fracti_fractal_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._entry_price = 0.0
        self._prev_high5 = 0.0
        self._prev_low5 = 0.0
        self._bar_count = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(com_fracti_fractal_rsi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_high5 = 0.0
        self._prev_low5 = 0.0
        self._bar_count = 0.0

    def OnStarted(self, time):
        super(com_fracti_fractal_rsi_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return com_fracti_fractal_rsi_strategy()
