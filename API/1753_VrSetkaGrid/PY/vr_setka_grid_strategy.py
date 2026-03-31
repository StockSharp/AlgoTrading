import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vr_setka_grid_strategy(Strategy):
    def __init__(self):
        super(vr_setka_grid_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Base candle series", "General")
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vr_setka_grid_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(vr_setka_grid_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ev = float(ema_value)

        if not self._has_prev:
            self._prev_ema = ev
            self._prev_close = close
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

        self._prev_ema = ev
        self._prev_close = close

    def CreateClone(self):
        return vr_setka_grid_strategy()
