import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class zero_lag_macd_strategy(Strategy):
    def __init__(self):
        super(zero_lag_macd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal", "Signal EMA period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def signal_length(self):
        return self._signal_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zero_lag_macd_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(zero_lag_macd_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_length
        macd.Macd.LongMa.Length = self.slow_length
        macd.SignalMa.Length = self.signal_length
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
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
        if not macd_value.IsFinal:
            return
        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        if self._has_prev:
            cross_up = self._prev_macd <= self._prev_signal and macd_line > signal_line
            cross_down = self._prev_macd >= self._prev_signal and macd_line < signal_line
            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()
        self._prev_macd = macd_line
        self._prev_signal = signal_line
        self._has_prev = True

    def CreateClone(self):
        return zero_lag_macd_strategy()
