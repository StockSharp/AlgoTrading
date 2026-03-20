import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_x_period_candle_x2_strategy(Strategy):
    def __init__(self):
        super(exp_x_period_candle_x2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Trend EMA period", "Indicators")

        self._prev_close = 0.0
        self._prev_open = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnReseted(self):
        super(exp_x_period_candle_x2_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(exp_x_period_candle_x2_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_open = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        if not self._has_prev:
            self._prev_close = close
            self._prev_open = open_p
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            self._prev_open = open_p
            return
        ev = float(ema_value)
        if (self._prev_close > self._prev_open and close > open_p
                and close > ev and self.Position <= 0):
            self.BuyMarket()
        elif (self._prev_close < self._prev_open and close < open_p
              and close < ev and self.Position >= 0):
            self.SellMarket()
        self._prev_close = close
        self._prev_open = open_p

    def CreateClone(self):
        return exp_x_period_candle_x2_strategy()
