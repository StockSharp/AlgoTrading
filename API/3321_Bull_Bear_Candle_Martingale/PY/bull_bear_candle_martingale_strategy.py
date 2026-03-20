import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bull_bear_candle_martingale_strategy(Strategy):
    def __init__(self):
        super(bull_bear_candle_martingale_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 30) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")

        self._ema = None
        self._prev_close = None
        self._prev_ema = None

    @property
    def ema_period(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(bull_bear_candle_martingale_strategy, self).OnReseted()
        self._ema = None
        self._prev_close = None
        self._prev_ema = None

    def OnStarted(self, time):
        super(bull_bear_candle_martingale_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        subscription.Bind(self._ema, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ema_val = float(ema_value)

        bullish = close > open_p
        bearish = close < open_p
        body_size = abs(close - open_p)
        range_size = high - low

        strong_candle = range_size > 0 and body_size / range_size > 0.5

        if self._prev_close is not None and self._prev_ema is not None and strong_candle:
            cross_up = self._prev_close <= self._prev_ema and close > ema_val
            cross_down = self._prev_close >= self._prev_ema and close < ema_val

            if bullish and cross_up and self.Position <= 0:
                self.BuyMarket()
            elif bearish and cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return bull_bear_candle_martingale_strategy()
