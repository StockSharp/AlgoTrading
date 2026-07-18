import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class brake_exp_channel_strategy(Strategy):
    def __init__(self):
        super(brake_exp_channel_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(brake_exp_channel_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(brake_exp_channel_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ev = float(ema_val)
        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ev
            self._has_prev = True
            return
        cross_up = self._prev_close <= self._prev_ema and close > ev
        cross_down = self._prev_close >= self._prev_ema and close < ev
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_ema = ev

    def CreateClone(self):
        return brake_exp_channel_strategy()
