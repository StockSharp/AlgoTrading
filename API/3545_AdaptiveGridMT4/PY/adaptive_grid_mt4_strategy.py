import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_grid_mt4_strategy(Strategy):
    def __init__(self):
        super(adaptive_grid_mt4_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 20) \
            .SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators")
        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.5) \
            .SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators")

        self._atr = None
        self._prev_close = None
        self._prev_atr = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_grid_mt4_strategy, self).OnReseted()
        self._atr = None
        self._prev_close = None
        self._prev_atr = None
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(adaptive_grid_mt4_strategy, self).OnStarted(time)

        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return adaptive_grid_mt4_strategy()
