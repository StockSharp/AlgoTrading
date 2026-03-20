import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_ma_on_rsi_filling_step_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_on_rsi_filling_step_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe used for RSI calculations.", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe used for RSI calculations.", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe used for RSI calculations.", "General")

        self._rsi = None
        self._rsi_history = new()
        self._previous_rsi = None
        self._previous_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_ma_on_rsi_filling_step_strategy, self).OnReseted()
        self._rsi = None
        self._rsi_history = new()
        self._previous_rsi = None
        self._previous_signal = None

    def OnStarted(self, time):
        super(rsi_ma_on_rsi_filling_step_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_ma_on_rsi_filling_step_strategy()
