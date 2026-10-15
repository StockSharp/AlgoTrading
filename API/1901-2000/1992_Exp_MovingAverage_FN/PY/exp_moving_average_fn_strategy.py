import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_moving_average_fn_strategy(Strategy):

    def __init__(self):
        super(exp_moving_average_fn_strategy, self).__init__()

        self._length = self.Param("Length", 12) \
            .SetDisplay("EMA Length", "EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._count = 0

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(exp_moving_average_fn_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.Length

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        turn_up = self._prev_ema < self._prev_prev_ema and ema_val > self._prev_ema
        turn_down = self._prev_ema > self._prev_prev_ema and ema_val < self._prev_ema

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val

    def OnReseted(self):
        super(exp_moving_average_fn_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._count = 0

    def CreateClone(self):
        return exp_moving_average_fn_strategy()
