import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class trend_signals_with_tp_sl_u_algo_strategy(Strategy):
    def __init__(self):
        super(trend_signals_with_tp_sl_u_algo_strategy, self).__init__()
        self._slow_length = self.Param("SlowLength", 40)             .SetDisplay("Slow Length", "Slow EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))             .SetDisplay("Candle Type", "Candle type", "General")

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(trend_signals_with_tp_sl_u_algo_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = 14
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length

        self._prev_f = 0.0
        self._prev_s = 0.0
        self._init = False
        self._last_signal = DateTime.MinValue
        self._cooldown = TimeSpan.FromMinutes(360)

        subscription = self.SubscribeCandles(self.candle_type)

        def on_candle(candle, f, s):
            if candle.State != CandleStates.Finished:
                return
            if not fast.IsFormed or not slow.IsFormed:
                return
            fv = float(f)
            sv = float(s)
            if not self._init:
                self._prev_f = fv
                self._prev_s = sv
                self._init = True
                return
            if candle.OpenTime - self._last_signal >= self._cooldown:
                if self._prev_f <= self._prev_s and fv > sv and self.Position <= 0:
                    self.BuyMarket()
                    self._last_signal = candle.OpenTime
                elif self._prev_f >= self._prev_s and fv < sv and self.Position >= 0:
                    self.SellMarket()
                    self._last_signal = candle.OpenTime
            self._prev_f = fv
            self._prev_s = sv

        subscription.Bind(fast, slow, on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def CreateClone(self):
        return trend_signals_with_tp_sl_u_algo_strategy()
