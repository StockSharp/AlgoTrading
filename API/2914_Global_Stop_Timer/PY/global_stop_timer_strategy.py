import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class global_stop_timer_strategy(Strategy):
    def __init__(self):
        super(global_stop_timer_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._momentum_length = self.Param("MomentumLength", 10) \
            .SetDisplay("Momentum Length", "Momentum period", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")

        self._prev_momentum = 100.0
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
        super(global_stop_timer_strategy, self).OnReseted()
        self._prev_momentum = 100.0
        self._has_prev = False

    def OnStarted(self, time):
        super(global_stop_timer_strategy, self).OnStarted(time)
        self._prev_momentum = 100.0
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
            self._prev_momentum = mv
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_momentum = mv
            return
        close = float(candle.ClosePrice)
        ev = float(ema_value)
        bullish_cross = self._prev_momentum <= 100 and mv > 100
        bearish_cross = self._prev_momentum >= 100 and mv < 100
        if bullish_cross and close > ev and self.Position <= 0:
            self.BuyMarket()
        elif bearish_cross and close < ev and self.Position >= 0:
            self.SellMarket()
        self._prev_momentum = mv

    def CreateClone(self):
        return global_stop_timer_strategy()
