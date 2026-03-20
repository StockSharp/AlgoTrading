import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class avalanche_strategy(Strategy):
    def __init__(self):
        super(avalanche_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(avalanche_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(avalanche_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return avalanche_strategy()
