import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_cross_audusd_d1_strategy(Strategy):
    def __init__(self):
        super(macd_cross_audusd_d1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_is_macd_above_signal = False
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_cross_audusd_d1_strategy, self).OnReseted()
        self._prev_is_macd_above_signal = False
        self._has_prev = False

    def OnStarted2(self, time):
        super(macd_cross_audusd_d1_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def on_process(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        m = macd_value.Macd
        s = macd_value.Signal
        if m is None or s is None:
            return
        macd_line = float(m)
        signal_line = float(s)
        is_macd_above_signal = macd_line > signal_line
        if not self._has_prev:
            self._prev_is_macd_above_signal = is_macd_above_signal
            self._has_prev = True
            return
        crossed_up = is_macd_above_signal and not self._prev_is_macd_above_signal
        crossed_down = not is_macd_above_signal and self._prev_is_macd_above_signal
        if crossed_up and self.Position <= 0:
            self.BuyMarket()
        elif crossed_down and self.Position >= 0:
            self.SellMarket()
        self._prev_is_macd_above_signal = is_macd_above_signal

    def CreateClone(self):
        return macd_cross_audusd_d1_strategy()
