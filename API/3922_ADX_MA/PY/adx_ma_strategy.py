import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_ma_strategy(Strategy):
    """
    ADX MA strategy: EMA crossover with price for trend-following entries.
    """

    def __init__(self):
        super(adx_ma_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")

        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA trend filter", "Indicators")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(adx_ma_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(adx_ma_strategy, self).OnStarted(time)

        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ema
            self._has_prev = True
            return

        if self._prev_close <= self._prev_ema and close > ema and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        elif self._prev_close >= self._prev_ema and close < ema and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_ma_strategy()
