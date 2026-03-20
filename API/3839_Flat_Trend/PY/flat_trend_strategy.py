import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class flat_trend_strategy(Strategy):
    def __init__(self):
        super(flat_trend_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR volatility period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_atr = 0.0
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(flat_trend_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(flat_trend_strategy, self).OnStarted(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.process_candle).Start()

    def process_candle(self, candle, ema, atr):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ema_val = float(ema)
        atr_val = float(atr)
        if not self._has_prev:
            self._prev_atr = atr_val
            self._prev_close = close
            self._prev_ema = ema_val
            self._has_prev = True
            return
        atr_expanding = atr_val > self._prev_atr
        if atr_expanding and self._prev_close <= self._prev_ema and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif atr_expanding and self._prev_close >= self._prev_ema and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_atr = atr_val
        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return flat_trend_strategy()
