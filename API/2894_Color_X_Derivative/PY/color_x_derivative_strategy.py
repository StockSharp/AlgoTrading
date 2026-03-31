import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_x_derivative_strategy(Strategy):
    def __init__(self):
        super(color_x_derivative_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._momentum_length = self.Param("MomentumLength", 34) \
            .SetDisplay("Momentum Length", "Derivative lookback", "Indicators")
        self._ema_length = self.Param("EmaLength", 7) \
            .SetDisplay("EMA Length", "Smoothing period", "Indicators")

        self._prev_mom = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnReseted(self):
        super(color_x_derivative_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(color_x_derivative_strategy, self).OnStarted2(time)
        self._prev_mom = 0.0
        self._has_prev = False

        momentum = Momentum()
        momentum.Length = self.MomentumLength
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, mom_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(mom_value)
        if not self._has_prev:
            self._prev_mom = mv
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_mom = mv
            return
        close = float(candle.ClosePrice)
        ev = float(ema_value)
        if self._prev_mom <= 0 and mv > 0 and close > ev and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_mom >= 0 and mv < 0 and close < ev and self.Position >= 0:
            self.SellMarket()
        self._prev_mom = mv

    def CreateClone(self):
        return color_x_derivative_strategy()
