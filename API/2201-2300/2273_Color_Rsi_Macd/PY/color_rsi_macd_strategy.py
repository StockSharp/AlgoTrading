import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

MODE_BREAKDOWN = 0
MODE_MACD_TWIST = 1
MODE_SIGNAL_TWIST = 2
MODE_MACD_DISPOSITION = 3


class color_rsi_macd_strategy(Strategy):
    def __init__(self):
        super(color_rsi_macd_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period", "MACD")
        self._mode = self.Param("Mode", MODE_MACD_DISPOSITION) \
            .SetDisplay("Mode", "Algorithm mode", "Logic")
        self._hist_prev = None
        self._macd_prev = None
        self._macd_prev2 = None
        self._signal_prev = None
        self._signal_prev2 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def signal_period(self):
        return self._signal_period.Value

    @property
    def mode(self):
        return self._mode.Value

    def OnReseted(self):
        super(color_rsi_macd_strategy, self).OnReseted()
        self._hist_prev = None
        self._macd_prev = None
        self._macd_prev2 = None
        self._signal_prev = None
        self._signal_prev2 = None

    def OnStarted2(self, time):
        super(color_rsi_macd_strategy, self).OnStarted2(time)
        self._hist_prev = None
        self._macd_prev = None
        self._macd_prev2 = None
        self._signal_prev = None
        self._signal_prev2 = None
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_period
        macd.Macd.LongMa.Length = self.slow_period
        macd.SignalMa.Length = self.signal_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return
        macd_line = float(macd_line)
        signal_line = float(signal_line)
        hist = macd_line - signal_line
        m = int(self.mode)
        if m == MODE_BREAKDOWN:
            if self._hist_prev is not None:
                if self._hist_prev < 0 and hist >= 0 and self.Position <= 0:
                    self.BuyMarket()
                elif self._hist_prev > 0 and hist <= 0 and self.Position >= 0:
                    self.SellMarket()
            self._hist_prev = hist
        elif m == MODE_MACD_TWIST:
            if self._macd_prev2 is not None and self._macd_prev is not None:
                if self._macd_prev < self._macd_prev2 and macd_line > self._macd_prev and self.Position <= 0:
                    self.BuyMarket()
                elif self._macd_prev > self._macd_prev2 and macd_line < self._macd_prev and self.Position >= 0:
                    self.SellMarket()
            self._macd_prev2 = self._macd_prev
            self._macd_prev = macd_line
        elif m == MODE_SIGNAL_TWIST:
            if self._signal_prev2 is not None and self._signal_prev is not None:
                if self._signal_prev < self._signal_prev2 and signal_line > self._signal_prev and self.Position <= 0:
                    self.BuyMarket()
                elif self._signal_prev > self._signal_prev2 and signal_line < self._signal_prev and self.Position >= 0:
                    self.SellMarket()
            self._signal_prev2 = self._signal_prev
            self._signal_prev = signal_line
        elif m == MODE_MACD_DISPOSITION:
            if self._macd_prev is not None and self._signal_prev is not None:
                if self._macd_prev <= self._signal_prev and macd_line > signal_line and self.Position <= 0:
                    self.BuyMarket()
                elif self._macd_prev >= self._signal_prev and macd_line < signal_line and self.Position >= 0:
                    self.SellMarket()
            self._macd_prev = macd_line
            self._signal_prev = signal_line

    def CreateClone(self):
        return color_rsi_macd_strategy()
