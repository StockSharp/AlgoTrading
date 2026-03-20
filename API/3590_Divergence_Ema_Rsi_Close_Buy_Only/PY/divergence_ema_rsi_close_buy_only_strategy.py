import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class divergence_ema_rsi_close_buy_only_strategy(Strategy):
    def __init__(self):
        super(divergence_ema_rsi_close_buy_only_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_entry = self.Param("RsiEntry", 35) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")
        self._rsi_exit = self.Param("RsiExit", 65) \
            .SetDisplay("Candle Type", "Timeframe for signals", "General")

        self._ema = None
        self._rsi = None
        self._prev_rsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(divergence_ema_rsi_close_buy_only_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._prev_rsi = None

    def OnStarted(self, time):
        super(divergence_ema_rsi_close_buy_only_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return divergence_ema_rsi_close_buy_only_strategy()
