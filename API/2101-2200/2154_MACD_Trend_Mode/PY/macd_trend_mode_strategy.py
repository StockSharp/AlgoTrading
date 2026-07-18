import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_trend_mode_strategy(Strategy):
    # TrendModes: 0=Histogram, 1=Cloud, 2=Zero
    def __init__(self):
        super(macd_trend_mode_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast EMA period", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "Slow EMA period", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "Signal line period", "MACD")
        self._trend_mode = self.Param("TrendMode", 1) \
            .SetDisplay("Trend Mode", "0=Histogram, 1=Cloud, 2=Zero", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_hist = 0.0
        self._prev_prev_hist = 0.0
        self._has_prev_hist = False
        self._has_prev_prev_hist = False
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev_lines = False

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
    def trend_mode(self):
        return self._trend_mode.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_trend_mode_strategy, self).OnReseted()
        self._prev_hist = 0.0
        self._prev_prev_hist = 0.0
        self._has_prev_hist = False
        self._has_prev_prev_hist = False
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev_lines = False

    def OnStarted2(self, time):
        super(macd_trend_mode_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_length
        macd.Macd.LongMa.Length = self.slow_length
        macd.SignalMa.Length = self.signal_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFinal or not macd_value.IsFormed:
            return
        macd_val_raw = macd_value.Macd
        signal_val_raw = macd_value.Signal
        if macd_val_raw is None or signal_val_raw is None:
            return
        macd_val = float(macd_val_raw)
        signal_val = float(signal_val_raw)
        hist = macd_val - signal_val
        buy_signal = False
        sell_signal = False
        mode = int(self.trend_mode)
        if mode == 0:  # Histogram
            if self._has_prev_hist and self._has_prev_prev_hist:
                if self._prev_hist < self._prev_prev_hist and hist > self._prev_hist:
                    buy_signal = True
                if self._prev_hist > self._prev_prev_hist and hist < self._prev_hist:
                    sell_signal = True
            self._prev_prev_hist = self._prev_hist
            self._has_prev_prev_hist = self._has_prev_hist
            self._prev_hist = hist
            self._has_prev_hist = True
        elif mode == 1:  # Cloud
            if self._has_prev_lines:
                if self._prev_macd <= self._prev_signal and macd_val > signal_val:
                    buy_signal = True
                if self._prev_macd >= self._prev_signal and macd_val < signal_val:
                    sell_signal = True
            self._prev_macd = macd_val
            self._prev_signal = signal_val
            self._has_prev_lines = True
        elif mode == 2:  # Zero
            if self._has_prev_hist:
                if self._prev_hist <= 0 and hist > 0:
                    buy_signal = True
                if self._prev_hist >= 0 and hist < 0:
                    sell_signal = True
            self._prev_hist = hist
            self._has_prev_hist = True
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return macd_trend_mode_strategy()
