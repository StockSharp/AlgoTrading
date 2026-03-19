import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class tp_sl_trailing_strategy(Strategy):
    """EMA price crossover for entries."""
    def __init__(self):
        super(tp_sl_trailing_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tp_sl_trailing_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_ema = 0

    def OnStarted(self, time):
        super(tp_sl_trailing_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._prev_ema = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close == 0 or self._prev_ema == 0:
            self._prev_close = close
            self._prev_ema = float(ema_val)
            return

        ema_f = float(ema_val)

        if self._prev_close < self._prev_ema and close >= ema_f and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close > self._prev_ema and close <= ema_f and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_f

    def CreateClone(self):
        return tp_sl_trailing_strategy()
