import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_rftl_strategy(Strategy):
    def __init__(self):
        super(rsi_rftl_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 44) \
            .SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")
        self._overbought = self.Param("Overbought", 75) \
            .SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")
        self._oversold = self.Param("Oversold", 25) \
            .SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicator")

        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(rsi_rftl_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(rsi_rftl_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period
        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_rftl_strategy()
