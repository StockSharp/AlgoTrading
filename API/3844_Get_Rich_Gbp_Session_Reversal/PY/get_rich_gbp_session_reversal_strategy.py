import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class get_rich_gbp_session_reversal_strategy(Strategy):
    def __init__(self):
        super(get_rich_gbp_session_reversal_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._overbought = self.Param("Overbought", 70) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._oversold = self.Param("Oversold", 30) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")

        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(get_rich_gbp_session_reversal_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(get_rich_gbp_session_reversal_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return get_rich_gbp_session_reversal_strategy()
