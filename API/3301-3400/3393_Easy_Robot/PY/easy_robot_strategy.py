import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class easy_robot_strategy(Strategy):
    def __init__(self):
        super(easy_robot_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._ema = None
        self._rsi = None
        self._was_bullish = False
        self._has_prev_signal = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(easy_robot_strategy, self).OnReseted()
        self._ema = None
        self._rsi = None
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted2(self, time):
        super(easy_robot_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._has_prev_signal = False

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._ema, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._rsi.IsFormed:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        rsi_val = float(rsi_value)
        is_bullish = close > ema_val and rsi_val > 50.0

        if self._has_prev_signal and is_bullish != self._was_bullish:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
            elif not is_bullish and close < ema_val and rsi_val < 50.0 and self.Position >= 0:
                self.SellMarket()

        self._was_bullish = is_bullish
        self._has_prev_signal = True

    def CreateClone(self):
        return easy_robot_strategy()
